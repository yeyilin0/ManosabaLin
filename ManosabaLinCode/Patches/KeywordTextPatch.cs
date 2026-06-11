using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ManosabaLin.Patches;

[HarmonyPatch]
public static class KeywordTextPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile",
            [typeof(PileType), typeof(Creature)]);
        yield return AccessTools.Method(typeof(CardModel), "GetDescriptionForUpgradePreview",
            Type.EmptyTypes);
    }

    [HarmonyPriority(Priority.Low)]
    static void Postfix(CardModel __instance, ref string __result)
    {
        var keywordColors = new Dictionary<string, string>
        {
            { "轮回。",     "#CC6666" },
            { "赞同",     "#6699cc" },
            { "质疑",     "#339966" },
            { "反驳",     "#CC6666" },
           
        };

        HashSet<string> whitelist = new(keywordColors.Keys);

        var matches = Regex.Matches(__result, @"\[gold\](.+?)\[/gold\]")
            .Cast<Match>()
            .Where(m => whitelist.Contains(m.Groups[1].Value))
            .ToList();

        if (matches.Count == 0) return;

        int offset = 0;
        for (int i = 0; i < matches.Count; i++)
        {
            Match m = matches[i];
            string name = m.Groups[1].Value;

            if (!keywordColors.TryGetValue(name, out string? color))
                continue;

            string replacement = $"[color={color}]{name}[/color]";

            __result = __result.Remove(m.Index + offset, m.Length)
                             .Insert(m.Index + offset, replacement);
            offset += replacement.Length - m.Length;
        }
    }
}
