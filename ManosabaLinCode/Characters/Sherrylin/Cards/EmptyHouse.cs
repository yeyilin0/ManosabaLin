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
        [new RetainCounterComponent(), new EmptyHouseComponent()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6, DamageProps.cardUnpowered)
    ];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var emptyHouseComp = source.GetComponent<EmptyHouseComponent>();
        var markedCards = emptyHouseComp?.MarkedCards;

        int hitCount = 1 + (markedCards?.Count ?? 0);

        for (int i = 0; i < hitCount; i++)
        {
            await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
                .FromCard(source)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);
        }

        if (markedCards != null)
        {
            foreach (var card in markedCards)
            {
                await CardPileCmd.RemoveFromCombat(card);
                await CardPileCmd.Add(card, PileType.Hand);

                var magnifyingGlass = Owner.Relics.OfType<MagnifyingGlass>().FirstOrDefault();
                if (magnifyingGlass != null && magnifyingGlass.HasTriggeredThisCombat)
                    card.SetToFreeThisTurn();
            }

            emptyHouseComp.ClearMarkedCards();
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}