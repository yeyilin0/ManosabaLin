using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ananlin;
using ManosabaLin.Characters.Ananlin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public class AnanlinAam() : ManosabaCardTemplate(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AnanlinBrainwashBacklashPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<AnanlinBrainwashBacklashPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner.Creature;

        if (owner.GetPower<AnanlinAamBacklashReductionUsedPower>() is not null) return;
        if (owner.GetPower<AnanlinBrainwashBacklashPower>() is not { Amount: > 0 } backlash) return;

        await PowerCmd.ModifyAmount(
            choiceContext,
            backlash,
            -source.DynamicVars["AnanlinBrainwashBacklashPower"].BaseValue,
            owner,
            source,
            false);

        await PowerCmd.Apply<AnanlinAamBacklashReductionUsedPower>(
            choiceContext,
            owner,
            1,
            owner,
            source,
            false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
