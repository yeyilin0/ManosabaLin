using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class SharedWitchRitual() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    private const int RequiredWitchification = 100;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WithPower>(RequiredWitchification),
        new PowerVar<RitualCeremonyPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
            yield return HoverTipFactory.FromPower<WithPower>();
            yield return HoverTipFactory.FromPower<RitualCeremonyPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target ?? Owner.Creature;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        if (!HasEnoughWitchification(Owner.Creature) || !HasEnoughWitchification(target)) return;

        await PowerCmd.Apply<RitualCeremonyPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);

        if (target != Owner.Creature)
            await PowerCmd.Apply<RitualCeremonyPower>(choiceContext, target, 1m, Owner.Creature, this, false);
    }

    private static bool HasEnoughWitchification(Creature creature)
    {
        return (creature.GetPower<WithPower>()?.Amount ?? 0) >= RequiredWitchification;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
