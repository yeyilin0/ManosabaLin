using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Emalin;

internal static class AgreeKeywordRegistration
{
    [RegisterOwnedCardKeyword("agree",
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
    private sealed class AgreeKeyword;
}

internal static class DoubtKeywordRegistration
{
    [RegisterOwnedCardKeyword("doubt",
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
    private sealed class DoubtKeyword;
}

internal static class RebuttalKeywordRegistration
{
    [RegisterOwnedCardKeyword("rebuttal",
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
    private sealed class RebuttalKeyword;
}

public static class EmalinKeywordRules
{
    public static string AgreeKeywordId =>
        ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, "agree");

    public static string DoubtKeywordId =>
        ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, "doubt");

    public static string RebuttalKeywordId =>
        ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, "rebuttal");

    public static bool HasAgreeKeyword(CardModel? card) =>
        card != null && card.HasModKeyword(AgreeKeywordId);

    public static bool HasDoubtKeyword(CardModel? card) =>
        card != null && card.HasModKeyword(DoubtKeywordId);

    public static bool HasRebuttalKeyword(CardModel? card) =>
        card != null && card.HasModKeyword(RebuttalKeywordId);
}