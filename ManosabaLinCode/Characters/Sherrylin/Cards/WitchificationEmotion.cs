using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 魔女化（情绪卡）：造成伤害时变为三倍伤害并且将目标转为随机对全场任意一个目标，此伤害不致死
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class WitchificationEmotion() : ManosabaCardTemplate(0, CardType.Power, CardRarity.Token, TargetType.Self, false)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<WitchificationEmotionPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await PowerCmd.Apply<WitchificationEmotionPower>(
            choiceContext, source.Owner.Creature, 1,
            source.Owner.Creature, source, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
