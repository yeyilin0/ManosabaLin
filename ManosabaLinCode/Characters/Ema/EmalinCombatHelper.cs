using ManosabaLin.Characters.Emalin.Enchantments;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using System.Linq;

namespace ManosabaLin.Characters.Emalin;

/// <summary>
/// 艾玛角色战斗工具类，用于查询附魔打出历史
/// </summary>
internal static class EmalinCombatHelper
{
    /// <summary>
    /// 本回合打出的附魔牌总数（赞同+反驳+疑问）
    /// </summary>
    public static int GetTotalEnchantmentPlaysThisTurn(Creature owner, ICombatState combatState)
    {
        return CombatManager.Instance.History.Entries
            .OfType<CardPlayStartedEntry>()
            .Count(e => e.HappenedThisTurn(combatState)
                && e.CardPlay.Card.Owner.Creature == owner
                && e.CardPlay.Card.Enchantment is Rebuttal or Agreement or Doubt);
    }

    /// <summary>
    /// 本回合打出的赞同牌数量
    /// </summary>
    public static int GetAgreementPlaysThisTurn(Creature owner, ICombatState combatState)
    {
        return CombatManager.Instance.History.Entries
            .OfType<CardPlayStartedEntry>()
            .Count(e => e.HappenedThisTurn(combatState)
                && e.CardPlay.Card.Owner.Creature == owner
                && e.CardPlay.Card.Enchantment is Agreement);
    }

    /// <summary>
    /// 本回合打出的反驳牌数量
    /// </summary>
    public static int GetRebuttalPlaysThisTurn(Creature owner, ICombatState combatState)
    {
        return CombatManager.Instance.History.Entries
            .OfType<CardPlayStartedEntry>()
            .Count(e => e.HappenedThisTurn(combatState)
                && e.CardPlay.Card.Owner.Creature == owner
                && e.CardPlay.Card.Enchantment is Rebuttal);
    }

    /// <summary>
    /// 本回合打出的疑问牌数量
    /// </summary>
    public static int GetDoubtPlaysThisTurn(Creature owner, ICombatState combatState)
    {
        return CombatManager.Instance.History.Entries
            .OfType<CardPlayStartedEntry>()
            .Count(e => e.HappenedThisTurn(combatState)
                && e.CardPlay.Card.Owner.Creature == owner
                && e.CardPlay.Card.Enchantment is Doubt);
    }

    /// <summary>
    /// 本回合打过几种不同的附魔（赞同/反驳/疑问）
    /// </summary>
    public static int GetDistinctEnchantmentTypesThisTurn(Creature owner, ICombatState combatState)
    {
        return CombatManager.Instance.History.Entries
            .OfType<CardPlayStartedEntry>()
            .Where(e => e.HappenedThisTurn(combatState)
                && e.CardPlay.Card.Owner.Creature == owner
                && e.CardPlay.Card.Enchantment is Rebuttal or Agreement or Doubt)
            .Select(e => e.CardPlay.Card.Enchantment!.GetType())
            .Distinct()
            .Count();
    }

    /// <summary>
    /// 检查本回合是否打过赞同和反驳
    /// </summary>
    public static bool HasPlayedBothAgreementAndRebuttal(Creature owner, ICombatState combatState)
    {
        var plays = CombatManager.Instance.History.Entries
            .OfType<CardPlayStartedEntry>()
            .Where(e => e.HappenedThisTurn(combatState)
                && e.CardPlay.Card.Owner.Creature == owner
                && e.CardPlay.Card.Enchantment is Rebuttal or Agreement)
            .Select(e => e.CardPlay.Card.Enchantment!.GetType())
            .Distinct()
            .Count();
        return plays >= 2;
    }

    /// <summary>
    ///     「免费 / 降费」类效果的候选判定：<b>只按「非资源类」原因判可打性</b>（不看当前能量够不够）。
    /// </summary>
    /// <remarks>
    ///     ⚠️⚠️ <b>多人同步铁律：这类效果的候选集绝不能用 <c>CanPlay()</c> 过滤。</b>
    ///     <para>
    ///         <c>CanPlay()</c> 把 <c>EnergyCostTooHigh / StarCostTooHigh</c> 一起算进来，而能量是随行动推进
    ///         <b>瞬时变化</b>的 —— 两个对端在「同一个 OnPlay 内、但结算进度略有先后」的时刻求值，
    ///         完全可能得到不同的候选集；再叠上 <c>Rng...NextItem()</c> 抽一张，就会
    ///         <b>一端把 A 设为 0 费、另一端把 B 设为 0 费（或谁都没设）</b>。
    ///         卡牌费用（<c>CardState.energyCost</c>）是校验和的一部分 ⇒ 直接触发 StateDivergence，
    ///         主机把队友踢下线（2026-10-15 实测：一张「赞同」附魔的安安防御牌 主机 0 费 / 队友 原费）。
    ///     </para>
    ///     <para>
    ///         口径与 <c>YalisalinFireComponentResolver</c> 一致：屏蔽掉资源类原因，
    ///         只保留「不可打出（Unplayable 关键词）/ 目标非法 / 被 Hook 拦截」这类两端必然一致的否决。
    ///         这同时更贴合卡面文案（本地化=规范）：「随机 1 张手牌免费」并不要求「你现在付得起」。
    ///     </para>
    /// </remarks>
    public static bool IsEligibleForCostEffect(CardModel? card)
    {
        if (card == null) return false;
        if (card.Keywords.Contains(CardKeyword.Unplayable)) return false;
        if (card.Owner?.PlayerCombatState == null) return false;

        if (card.CanPlay(out var reason, out _))
            return true;

        var nonResourceReasons = reason
                                 & ~UnplayableReason.EnergyCostTooHigh
                                 & ~UnplayableReason.StarCostTooHigh;
        return nonResourceReasons == UnplayableReason.None;
    }
}
