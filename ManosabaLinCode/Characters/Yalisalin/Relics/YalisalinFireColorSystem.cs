namespace ManosabaLin.Characters.Yalisalin.Relics;

public static class YalisalinFireColorSystem
{
    public static bool TryGetHairpin(Player player, out YalisalinsHairpin hairpin)
    {
        hairpin = player.Relics.OfType<YalisalinsHairpin>().FirstOrDefault()!;
        return hairpin != null;
    }

    public static bool TryAddFireColor(Player player, Creature target, int amount = 1, CardModel? source = null)
    {
        return TryGetHairpin(player, out var hairpin)
               && hairpin.TryAddFireColor(target, amount, source);
    }

    public static bool TryConvertFireColor(Player player, Creature target, CardModel? source = null)
    {
        return TryGetHairpin(player, out var hairpin)
               && hairpin.TryConvertFireColor(target, source);
    }

    public static bool TryStrongConvertFireColor(Player player, Creature target, CardModel? source = null)
    {
        return TryGetHairpin(player, out var hairpin)
               && hairpin.TryStrongConvertFireColor(target, source);
    }

    public static bool TryDowngradeFireColor(
        Player player,
        Creature target,
        out YalisalinFireColor originalColor,
        CardModel? source = null)
    {
        originalColor = default;
        return TryGetHairpin(player, out var hairpin)
               && hairpin.TryDowngradeFireColor(target, out originalColor, source);
    }

    public static bool TryMoveLastFireColorToFront(
        Player player,
        Creature target,
        out YalisalinFireColor movedColor,
        CardModel? source = null)
    {
        movedColor = default;
        return TryGetHairpin(player, out var hairpin)
               && hairpin.TryMoveLastFireColorToFront(target, out movedColor, source);
    }

    public static Task<IReadOnlyList<YalisalinFireColorSegment>> ConsumeFireColor(
        PlayerChoiceContext choiceContext,
        Player player,
        Creature target,
        int amount,
        CardModel? source = null)
    {
        return TryGetHairpin(player, out var hairpin)
            ? hairpin.ConsumeFireColor(choiceContext, target, amount, source)
            : Task.FromResult<IReadOnlyList<YalisalinFireColorSegment>>([]);
    }

    public static Task<YalisalinFireColorConsumeResult> ConsumeFireColorDetailed(
        PlayerChoiceContext choiceContext,
        Player player,
        Creature target,
        int amount,
        CardModel? source = null)
    {
        return TryGetHairpin(player, out var hairpin)
            ? hairpin.ConsumeFireColorDetailed(choiceContext, target, amount, source)
            : Task.FromResult(YalisalinFireColorConsumeResult.Empty);
    }

    public static Task ResolveExtraFireColorReward(
        PlayerChoiceContext choiceContext,
        Player player,
        YalisalinFireColor color,
        CardModel? source = null)
    {
        return TryGetHairpin(player, out var hairpin)
            ? hairpin.ResolveExtraFireColorReward(choiceContext, color, source)
            : Task.CompletedTask;
    }

    public static IReadOnlyList<YalisalinFireColorSegment> GetFireColorSegments(Player player, Creature target)
    {
        return TryGetHairpin(player, out var hairpin)
            ? hairpin.GetFireColorSegments(target)
            : [];
    }
}
