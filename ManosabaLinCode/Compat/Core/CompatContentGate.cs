using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Compat.Core;

internal static class CompatContentGate
{
    public static bool IsExternalCompatRelic(RelicModel? relic)
    {
        if (relic == null)
            return false;

        var canonicalRelic = relic.CanonicalInstance ?? relic;
        return canonicalRelic switch
        {
            // VariantPersonWindchaserThePlaneswalker => true,
            _ => false
        };
    }

    public static bool IsGameplayRelicAvailable(RelicModel? relic)
    {
        if (relic == null)
            return false;

        var canonicalRelic = relic.CanonicalInstance ?? relic;
        return canonicalRelic switch
        {
            // VariantPersonWindchaserThePlaneswalker => WindchaserCompat.IsLoaded(),
            _ => true
        };
    }

    public static bool IsCompendiumCardVisible(CardModel? card)
    {
        if (card == null)
            return false;

        return card switch
        {
            // SkillGrantSpark => WindchaserCompat.IsLoaded(),
            _ => true
        };
    }
}
