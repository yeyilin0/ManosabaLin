using ManosabaLin.Characters.Emalin.Enchantments;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
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
}
