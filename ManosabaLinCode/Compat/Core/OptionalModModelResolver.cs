using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Compat.Core;

internal static class OptionalModModelResolver
{
    public static bool TryFindCardByEntry(string entryId, out CardModel card)
    {
        card = null!;
        if (string.IsNullOrWhiteSpace(entryId))
            return false;

        var normalizedTarget = entryId.Trim();

        try
        {
            card = ModelDb.AllCards.FirstOrDefault(candidate =>
            {
                var entry = candidate.Id.Entry;
                return string.Equals(entry, normalizedTarget, StringComparison.OrdinalIgnoreCase)
                       || entry.EndsWith($"-{normalizedTarget}", StringComparison.OrdinalIgnoreCase)
                       || entry.EndsWith($"_{normalizedTarget}", StringComparison.OrdinalIgnoreCase);
            })!;

            return card != null;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"[{MainFile.ModId}] Optional mod compatibility card resolve failed for '{entryId}': {ex.Message}");
            return false;
        }
    }
}
