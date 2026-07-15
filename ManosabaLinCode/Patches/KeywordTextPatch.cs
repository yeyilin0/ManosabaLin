using HarmonyLib;
using ManosabaLin.Characters.Common.HiroKeywords;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using System.Reflection;
using STS2RitsuLib;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Patches;

[HarmonyPatch]
[HarmonyAfter(RitsuLibContentAssetsHarmonyId)]
public static class KeywordTextPatch
{
    private const string RitsuLibContentAssetsHarmonyId = Const.ModId + ".framework-content-assets";
    private const string DescriptionPreviewTypeName = "DescriptionPreviewType";

    private static readonly IReadOnlyDictionary<string, string> KeywordColors =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [TransmigrationRules.TransmigrationKeywordId] = "#CC6666",
            [Transmigration3Rules.Transmigration3KeywordId] = "#CC6666",
            [EmalinKeywordRules.AgreeKeywordId] = "#6699cc",
            [EmalinKeywordRules.DoubtKeywordId] = "#cc9966",
            [EmalinKeywordRules.RebuttalKeywordId] = "#339966",
        };

    static IEnumerable<MethodBase> TargetMethods()
    {
        var privateDescriptionBuilder = AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.GetDescriptionForPile),
            [typeof(PileType), GetDescriptionPreviewType(), typeof(Creature)]);
        if (privateDescriptionBuilder != null)
            yield return privateDescriptionBuilder;

        // Keep the public wrappers as a final safety net for call sites that bypass the private builder.
        var descriptionForPile = AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.GetDescriptionForPile),
            [typeof(PileType), typeof(Creature)]);
        if (descriptionForPile != null)
            yield return descriptionForPile;

        var upgradePreview = AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.GetDescriptionForUpgradePreview),
            Type.EmptyTypes);
        if (upgradePreview != null)
            yield return upgradePreview;
    }

    [HarmonyPriority(Priority.Last)]
    static void Postfix(CardModel __instance, ref string __result)
    {
        if (string.IsNullOrEmpty(__result))
            return;

        foreach (var keywordId in __instance.GetModKeywordIds())
        {
            if (!KeywordColors.TryGetValue(keywordId, out var color))
                continue;

            var title = ModKeywordRegistry.GetTitle(keywordId).GetFormattedText();
            if (string.IsNullOrEmpty(title))
                continue;

            __result = ColorizeGoldKeyword(__result, title, color);
        }
    }

    private static string ColorizeGoldKeyword(string text, string title, string color)
    {
        return text.Replace(
            $"[gold]{title}[/gold]",
            $"[color={color}]{title}[/color]",
            StringComparison.Ordinal);
    }

    private static Type GetDescriptionPreviewType()
    {
        return typeof(CardModel).GetNestedType(DescriptionPreviewTypeName, BindingFlags.NonPublic)
               ?? throw new MissingMemberException(typeof(CardModel).FullName, DescriptionPreviewTypeName);
    }
}
