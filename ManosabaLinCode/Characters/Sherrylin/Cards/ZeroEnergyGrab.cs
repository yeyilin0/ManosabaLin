using ManosabaLin.Characters.Sherrylin.Capabilities;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class ZeroEnergyGrab() : ManosabaCardTemplate(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        foreach (var newCard in CardFactory.GetForCombat(
                     source.Owner,
                     source.Owner.Character.CardPool.GetUnlockedCards(
                             source.Owner.UnlockState,
                             source.Owner.RunState.CardMultiplayerConstraint)
                         .Where(c => c.EnergyCost.Canonical == 0 && !c.EnergyCost.CostsX),
                     source.DynamicVars.Cards.IntValue,
                     source.Owner.RunState.Rng.CombatCardGeneration))
        {
            newCard.GetOrCreateCapability<RemoveOnPlayCapability>();
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, source.Owner);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Cards"].UpgradeValueBy(1m);
    }
}
