using ManosabaLin.Characters.Sherrylin.Capabilities;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;
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

        // 自己的卡池中的0费牌
        var ownPool = source.Owner.Character.CardPool
            .GetUnlockedCards(source.Owner.UnlockState, source.Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.EnergyCost.Canonical == 0 && !c.EnergyCost.CostsX);

        // 所有其他角色卡池中的0费牌
        var otherPools = source.Owner.UnlockState.CharacterCardPools
            .Where(p => p != source.Owner.Character.CardPool)
            .SelectMany(p => p.GetUnlockedCards(source.Owner.UnlockState, source.Owner.RunState.CardMultiplayerConstraint))
            .Where(c => c.EnergyCost.Canonical == 0 && !c.EnergyCost.CostsX);

        var candidates = ownPool.Concat(otherPools).Distinct().ToList();

        foreach (var newCard in CardFactory.GetForCombat(
                     source.Owner,
                     candidates,
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
