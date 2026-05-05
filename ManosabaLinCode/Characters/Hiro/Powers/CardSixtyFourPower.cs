using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class CardSixtyFourPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.Static(StaticHoverTip.Block); }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var source = this;

        // 只处理持有者打出的牌
        if (cardPlay.Card.Owner.Creature != source.Owner)
            return;

        // 直接使用 HasModKeyword 检查轮回关键词
        if (!cardPlay.Card.HasModKeyword(TransmigrationRules.TransmigrationKeywordId))
            return;

        source.Flash();

        // 获得两层伪证
        await PowerCmd.Apply<PerjuryPower>(
            context, // ★ 第一个参数
            source.Owner,
            2,
            source.Owner,
            cardPlay.Card,
            false
        );
    }
}