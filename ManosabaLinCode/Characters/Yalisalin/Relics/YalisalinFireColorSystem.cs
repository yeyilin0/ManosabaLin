namespace ManosabaLin.Characters.Yalisalin.Relics;

public static class YalisalinFireColorSystem
{
    public static bool TryGetHairpin(Player player, out YalisalinsHairpin hairpin)
    {
        hairpin = player.Relics.OfType<YalisalinsHairpin>().FirstOrDefault()!;
        return hairpin != null;
    }

    public static Task<int> GiveFireColor(
        PlayerChoiceContext choiceContext,
        Player player,
        Creature target,
        int amount,
        CardModel? source = null,
        bool overflowTriggersConsume = false)
    {
        return TryGetHairpin(player, out var hairpin)
            ? hairpin.GiveFireColor(choiceContext, target, amount, source, overflowTriggersConsume)
            : Task.FromResult(0);
    }

    public static Task<IReadOnlyList<YalisalinFireColor>> ConsumeFireColor(
        PlayerChoiceContext choiceContext,
        Player player,
        Creature target,
        int amount,
        CardModel? source = null)
    {
        return TryGetHairpin(player, out var hairpin)
            ? hairpin.ConsumeFireColor(choiceContext, target, amount, source)
            : Task.FromResult<IReadOnlyList<YalisalinFireColor>>([]);
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
