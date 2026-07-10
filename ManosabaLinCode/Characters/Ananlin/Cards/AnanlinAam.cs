using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Ananlin;
using ManosabaLin.ManosabaLinCode.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public class AnanlinAam() : ManosabaCardTemplate(2, CardType.Power, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SuspectPower>(3m),
        new PowerVar<AamPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<AamPower>();
            yield return HoverTipFactory.FromPower<SuspectPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var markedEnemy = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(markedEnemy);

        await PowerCmd.Apply<SuspectPower>(
            choiceContext, source.Owner.Creature, source.DynamicVars["SuspectPower"].BaseValue,
            source.Owner.Creature, source, false);

        await PowerCmd.Apply<AamPower>(
            choiceContext, markedEnemy, source.DynamicVars["AamPower"].BaseValue,
            source.Owner.Creature, source, false);

        var redirectPower = markedEnemy.Powers.OfType<AamPower>().FirstOrDefault();
        if (redirectPower is not null)
            _ = TaskHelper.RunSafely(redirectPower.ChooseMoveAndTarget(choiceContext, source.Owner));
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}