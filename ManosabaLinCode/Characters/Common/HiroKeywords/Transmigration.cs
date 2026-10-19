using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Characters.Hiro.Capabilities;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Common.HiroKeywords;

internal static class TransmigrationKeywordRegistration
{
    [RegisterOwnedCardKeyword("transmigration",
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
    private sealed class TransmigrationKeyword;
}

public static class TransmigrationRules
{
    private const int MaxCopiesToPlay = 2;

    public static string TransmigrationKeywordId =>
        ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, "transmigration");

    public static CardKeyword TransmigrationCardKeyword => TransmigrationKeywordId.GetModCardKeyword();

    public static bool HasTransmigration(CardModel? card)
    {
        return card != null && card.HasModKeyword(TransmigrationCardKeyword);
    }

    /// <summary>
    ///     【轮回】的「同名」判据：
    ///     ① 同 Id（原版基础规则，两张「打击」互为同名）；
    ///     ② 两张卡由同一次「宿命的轮替」升级后的**隐藏能力**结成一对
    ///        （<c>TransmigrationPairCapability</c>，PairNames 含「第 2 次选择」那张卡的卡名）
    ///        ⇒ 无论打出结成一对的哪一张，都按**同一个卡名**认出彼此。
    ///     <para>
    ///         ⚠️ 刻意**不看**「显示卡名」：卡名覆盖（<c>CardRename</c>）只是视觉，
    ///         若拿卡名当判据，牌组里恰好同名的无关卡牌会被误判成一对。
    ///     </para>
    /// </summary>
    private static bool IsSameNameForTransmigration(CardModel a, CardModel b)
    {
        return a.Id == b.Id || TransmigrationPairCapability.AreLinked(a, b);
    }

    private static List<CardModel> GetMatchingCardsFromDrawPile(CardModel source)
    {
        if (source?.Owner == null) return new List<CardModel>();

        var owner = source.Owner;
        var drawPile = PileType.Draw.GetPile(owner);
        var discardPile = PileType.Discard.GetPile(owner);

        // 抽牌堆中的同名轮回卡（原有逻辑）
        // 同一处痕迹：本次打出时刚加入抽牌堆的副本不能立刻被轮回触发，只触发原本就在抽牌堆的卡
        // ⭐ 2026-10-08：「宿命的轮替」升级后的配对改由**隐藏能力**承担
        //     （TransmigrationPairCapability，只挂在「获得轮回」的卡上、PairNames 含「第 2 次选择」那张的卡名）——
        //     不再靠「卡名相等」判定，卡名覆盖只负责视觉。判据见 IsSameNameForTransmigration。
        var matching = drawPile.Cards
            .Where(c => c != source && HasTransmigration(c) && IsSameNameForTransmigration(c, source)
                        && !(c is SamePlaceTrace { JustAddedToDrawPile: true }))
            .Take(MaxCopiesToPlay)
            .ToList();

        // 带真相组件的卡在弃牌堆也能被轮回自动打出（真相组件扩展）
        if (matching.Count < MaxCopiesToPlay)
        {
            var fromDiscard = discardPile.Cards
                .Where(c => c != source && HasTransmigration(c) && IsSameNameForTransmigration(c, source)
                            && c.TryGetCapability<TruthComponentCapability>(out _))
                .Take(MaxCopiesToPlay - matching.Count);

            matching.AddRange(fromDiscard);
        }

        return matching;
    }

    /// <summary>
    ///     自动打出前复核：这张卡是否**仍然在战斗牌堆里**。
    ///     <para>
    ///         ⚠️ 必须复核——候选卡是「本次触发开始时」一次性快照的，但后面那张卡可能在
    ///         前一张结算途中就被移出战斗。典型：同一处痕迹凑满线索后
    ///         <c>SamePlaceTrace.RemoveAllSamePlaceTrace</c> 会把战斗内的同一处痕迹**全部**移出，
    ///         连快照里还没轮到的那张也被移出。
    ///     </para>
    ///     <para>
    ///         此时若照旧交给 <c>CardCmd.AutoPlay</c>：其内部的
    ///         <c>CardPileCmd.Add(card, PileType.Play)</c> 会因 <c>HasBeenRemovedFromState</c>
    ///         静默失败（返回 success=false），于是 <c>CardModel.CombatState</c> 保持为 null；
    ///         而 <c>CardModel.OnPlayWrapper</c> 在 <c>PushModel(this)</c>（第 1860 行）之后、
    ///         <c>PopModel(this)</c>（第 2018 行）之前有一处
    ///         <c>if (combatState == null) return;</c> —— 提前 return 就跳过 PopModel，
    ///         把 model **永久残留**在 choiceContext 的栈上。引擎的 <c>PopModel</c> 在不匹配时
    ///         **只报错、不弹栈**，所以之后该 context 的每一次 pop 都会继续报错
    ///         （日志：<c>Tried to pop model ... but ... SAME_PLACE_TRACE was on the top</c>）。
    ///     </para>
    /// </summary>
    private static bool CanStillAutoPlayCore(CardModel card)
    {
        if (card.HasBeenRemovedFromState) return false;
        if (card.Pile is not { IsCombatPile: true }) return false;
        var owner = card.Owner;
        return owner != null && !owner.Creature.IsDead;
    }

    /// <summary>
    ///     供【轮回】管线的其它入口（<c>Transmigration3Rules</c>、<c>HextechTransmigrationEcho</c>）复用，
    ///     统一「交给 <c>CardCmd.AutoPlay</c> 前必须复核卡牌仍在战斗牌堆内」这条约束。
    /// </summary>
    internal static bool CanStillAutoPlay(CardModel card)
    {
        return !CombatManager.Instance.IsOverOrEnding && CanStillAutoPlayCore(card);
    }

    public static async Task TriggerTransmigrationEffect(CardModel card, PlayerChoiceContext choiceContext,
        Creature? originalTarget)
    {
        if (!HasTransmigration(card)) return;

        var matchingCards = GetMatchingCardsFromDrawPile(card);

        if (matchingCards.Count == 0) return;

        var isFirst = true;
        foreach (var matchingCard in matchingCards)
        {
            // 战斗已结束 / 这张卡已被移出战斗 ⇒ 不能再交给 AutoPlay（详见 CanStillAutoPlay 注释）
            if (!CanStillAutoPlay(matchingCard))
            {
                continue;
            }

            matchingCard.SetToFreeThisTurn();

            await CardCmd.AutoPlay(
                choiceContext,
                matchingCard,
                originalTarget,
                skipCardPileVisuals: !isFirst
            );

            isFirst = false;
            await Cmd.Wait(0.1f);
        }
    }
}

[RegisterSingleton]
public sealed class TransmigrationSingleton : SingletonModel
{
    public TransmigrationSingleton()
    {
        ModHelper.SubscribeForCombatStateHooks(Id.Entry, CombatSubModels);
    }

    public override bool ShouldReceiveCombatHooks => true;

    private IEnumerable<AbstractModel> CombatSubModels(CombatState _)
    {
        return [this];
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.IsAutoPlay)
            return SamePlaceTruth.AdvanceLockedTruthsForAutoPlay(context, cardPlay);

        return TransmigrationRules.TriggerTransmigrationEffect(cardPlay.Card, context, cardPlay.Target);
    }
}
