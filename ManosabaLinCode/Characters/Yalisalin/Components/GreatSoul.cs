using ManosabaLin.Characters.Common.Components.Abstracts;
using ManosabaLin.Characters.Yalisalin.Cards;

namespace ManosabaLin.Characters.Yalisalin.Components;

/// <summary>
///     伟大灵魂：被【传火】点亮的牌（可理解为「火种」）。
///     打出带此组件的牌时，使【传火】本场战斗内成长 1 次（王魂数 +1）——「火种养王魂」的引擎。
///     <para>
///     不摸牌、不燃尽：组件挂上后一直留着，同一张火种可以反复当引擎用。
///     同时存在的【伟大灵魂】至多 4 个（【传火】补火时见 <see cref="LinkTheFire.FillUpFireSeeds" />）。
///     王魂数的增长另有效果库里的「灵魂满载」「先古的火种」推动。
///     </para>
///     与【余火】是两个互不相干的体系（余火的行为挂在 RitsuLib 能力上，这里是一个独立的 MinionLib 组件）。
///     组件编号由源生成器按「根命名空间 + 类名」生成，即 <c>ManosabaLin.GreatSoul</c>，
///     卡面前缀 <c>.prefix</c> 与悬浮提示 <c>.hovertip.title</c> / <c>.hovertip.description</c>
///     的本地化键也以此为前缀。
/// </summary>
public sealed partial class GreatSoul : KeywordLikeComponent
{
    /// <summary>这张牌是否已被点亮。</summary>
    public static bool Has(CardModel? card)
    {
        return card.HasComponent<GreatSoul>();
    }

    /// <summary>点亮一张牌；已经点亮的牌不重复挂载。</summary>
    public static bool TryLightUp(CardModel? card)
    {
        if (card == null || Has(card))
            return false;

        return card.TryAddComponent(new GreatSoul()) != null;
    }

    /// <summary>
    ///     打出被点亮的牌：使【传火】本场战斗内成长 1 次（王魂数 +1）。
    ///     不摸牌、不燃尽（本地化口径「打出此牌时，使【传火】本场战斗内成长1次」）。
    /// </summary>
    public override Task AfterCardPlayedPostfix(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(cardPlay.Card, Card))
            return Task.CompletedTask;

        if (Card is not { } soulCard || soulCard.Owner is not { } owner)
            return Task.CompletedTask;

        LinkTheFire.GrowAll(owner, 1);
        return Task.CompletedTask;
    }
}
