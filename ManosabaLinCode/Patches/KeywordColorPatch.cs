using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ManosabaLin.Patches;

/// <summary>
/// 将卡牌描述中的关键词文本替换为对应颜色
/// Priority=999 确保在 RitsuLib 的 Postfix 之后运行
/// </summary>
[HarmonyPatch]
internal static class KeywordColorPatch
{
    private static readonly Dictionary<string, string> KeywordColors = new()
    {
        // 轮回系 — 暗红
        { "轮回", "#CC6666" },
        { "Cycle", "#CC6666" },
        { "轮轮轮回回回", "#CC6666" },
        { "Cycle Cycle Cycle", "#CC6666" },

        // 审判系 — 各自颜色
        { "赞同", "#6699cc" },
        { "Agreement", "#6699cc" },
        { "疑问", "#cc9966" },
        { "Doubt", "#cc9966" },
        { "反驳", "#339966" },
        { "Rebuttal", "#339966" },
    };

    // 按长度降序排列，优先匹配更长的关键词
    private static readonly KeyValuePair<string, string>[] SortedKeywords =
        KeywordColors.OrderByDescending(kv => kv.Key.Length).ToArray();

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile",
            [typeof(PileType), AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType"), typeof(Creature)]);
    }

    [HarmonyPriority(999)]
    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (string.IsNullOrEmpty(__result)) return;

        foreach (var (keyword, color) in SortedKeywords)
        {
            // 简单直接替换，不依赖 [gold] 标签
            // 如果关键词已在 [color] 标签内，替换后结果不变（同色）
            __result = __result.Replace(keyword, $"[color={color}]{keyword}[/color]");
        }
    }
}
