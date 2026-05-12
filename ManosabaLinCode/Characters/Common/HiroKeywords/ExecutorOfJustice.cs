using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Common.HiroKeywords;

internal static class ExecutorOfJusticeKeywordRegistration
{
    [RegisterOwnedCardKeyword("executor_of_justice",
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
    private sealed class ExecutorOfJusticeKeyword;
}
