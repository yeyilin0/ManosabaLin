using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using ManosabaLin.Characters.Sherrylin.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class EmptyHouse() : ManosabaCardTemplate(4, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new RetainCounterComponent()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3, DamageProps.cardUnpowered)
    ];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var markedCards = new[] { PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust }
            .SelectMany(pileType => pileType.GetPile(Owner).Cards)
            .Where(card => card.HasComponent<EmptyHouseComponebt>())
            .ToList();

        int hitCount = 1 + markedCards.Count;

        for (int i = 0; i < hitCount; i++)
        {
            await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
                .FromCard(source, cardPlay)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);
        }

        var magnifyingGlass = Owner.Relics.OfType<MagnifyingGlass>().FirstOrDefault();
        var hasTriggeredCaseReversalThisTurn = magnifyingGlass?.HasTriggeredThisCombat == true;

        foreach (var card in markedCards)
        {
            if (hasTriggeredCaseReversalThisTurn)
                card.SetToFreeThisTurn();

            await CardPileCmd.Add(card, PileType.Hand);
            card.TryRemoveComponent<EmptyHouseComponebt>();
        }
    }

    protected override Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player,
        ComponentContext componentContext)
    {
        if (player != Owner) return Task.CompletedTask;

        var exhaustCards = PileType.Exhaust.GetPile(Owner).Cards
            .Where(card => card is IComponentsCardModel && !card.HasComponent<EmptyHouseComponebt>())
            .ToList();
        if (exhaustCards.Count == 0) return Task.CompletedTask;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        rng.NextItem(exhaustCards)?.TryAddComponent(new EmptyHouseComponebt());
        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
