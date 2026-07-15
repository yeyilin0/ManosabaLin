using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Xlmk() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<XlmPower>();
            yield return HoverTipFactory.FromPower<ShockwavePower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        var magicAmount = (int)source.Owner.Creature.GetPowerAmount<XlmPower>();
        var shockwaveAmount = IsUpgraded ? magicAmount : magicAmount / 2;
        if (shockwaveAmount <= 0) return;

        await PowerCmd.Apply<ShockwavePower>(
            choiceContext,
            target,
            shockwaveAmount,
            source.Owner.Creature,
            source,
            false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
