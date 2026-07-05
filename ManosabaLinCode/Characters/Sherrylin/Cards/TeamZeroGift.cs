using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Capabilities;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TeamZeroGift() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    private const string CardsCountKey = "Cards";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var candidates = Owner.Character.CardPool
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.EnergyCost.Canonical == 0 && !c.EnergyCost.CostsX);

        var cardCount = source.DynamicVars[CardsCountKey].IntValue;

        foreach (var ally in CombatState.Players.Where(p => p != Owner && p.Creature.Side == Owner.Creature.Side && p.Creature.IsAlive))
        {
            var newCards = CardFactory.GetForCombat(ally, candidates, cardCount, Owner.RunState.Rng.CombatCardGeneration);
            foreach (var newCard in newCards)
            {
                newCard.GetOrCreateCapability<RemoveOnPlayCapability>();
                await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, ally);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[CardsCountKey].UpgradeValueBy(1m);
    }
}
