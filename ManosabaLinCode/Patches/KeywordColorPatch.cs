using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ManosabaLin.Patches;

/// <summary>
/// 将卡牌描述中的 [gold]关键词[/gold] 替换为对应颜色的 [color=#xxx]关键词[/color]
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

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile",
            [typeof(PileType), AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType"), typeof(Creature)]);
    }

    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (string.IsNullOrEmpty(__result)) return;

        // 匹配 [gold]内容[/gold]
        var matches = Regex.Matches(__result, @"\[gold\](.+?)\[/gold\]")
            .Cast<Match>()
            .ToList();

        if (matches.Count == 0) return;

        // 从后往前替换，避免索引偏移
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var m = matches[i];
            string keyword = m.Groups[1].Value;

            if (!KeywordColors.TryGetValue(keyword, out string color))
                continue;

            string replacement = $"[color={color}]{keyword}[/color]";
            __result = __result.Remove(m.Index, m.Length)
                               .Insert(m.Index, replacement);
        }
    }
}
