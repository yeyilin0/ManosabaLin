using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class HiroWith : ManosabaCardTemplate
{
    private const int RequiredWithAmount = 100;

    public HiroWith() : base(4, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }




    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<RitualCeremonyPower>(1m),
        new PowerVar<IntangiblePower>(2m),
        new PowerVar<MayhemPower>(1m),
        new PowerVar<WithPower>(100m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WithPower>();
            yield return HoverTipFactory.FromPower<RitualCeremonyPower>();
            yield return HoverTipFactory.FromPower<IntangiblePower>();
            yield return HoverTipFactory.FromPower<MayhemPower>();
            yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
        }
    }

    protected override bool IsPlayableC
    {
        get
        {
            if (!base.IsPlayableC)
                return false;

            var withPower = Owner.Creature.GetPower<WithPower>();
            var withAmount = withPower?.Amount ?? 0;

            return withAmount >= RequiredWithAmount;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<RitualCeremonyPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["RitualCeremonyPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        await PowerCmd.Apply<IntangiblePower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["IntangiblePower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        await PowerCmd.Apply<MayhemPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["ShadowStepPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
