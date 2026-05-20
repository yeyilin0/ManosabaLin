using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.ManosabaLinCode.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

using ManosabaLin.Characters.Emalin;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class EmaAam : ManosabaCardTemplate
{
    public EmaAam() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.AnyEnemy) { }

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
            choiceContext, source.Owner.Creature, 3,
            source.Owner.Creature, source, false);

        await PowerCmd.Apply<AamPower>(
            choiceContext, markedEnemy, 1,
            source.Owner.Creature, source, false);

        var redirectPower = markedEnemy.Powers.OfType<AamPower>().FirstOrDefault();
        if (redirectPower is not null)
            await redirectPower.ChooseMoveAndTarget(choiceContext, source.Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
