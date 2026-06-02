using ManosabaLin.Characters.Hiro;

namespace ManosabaLin.Audio.Services;

public static class AudioEventPathProvider
{
    public static string CardPoolName(CardModel card)
    {
        return card.VisualCardPool.Title.ToLowerInvariant();
    }

    public static string CardOwnerName(CardModel card)
    {
        return card.Owner.Character.Id.Entry.ToLowerInvariant();
    }

    public static string CardNameSlug(CardModel card)
    {
        return card.GetType().Name.ToLowerInvariant();
    }

    private static string? CardTypeName(CardType type)
    {
        return type switch
        {
            CardType.None => "none",
            CardType.Attack => "attack",
            CardType.Skill => "skill",
            CardType.Power => "power",
            CardType.Status => "status",
            CardType.Curse => "curse",
            CardType.Quest => "quest",
            _ => null
        };
    }

    public static string? CardTypeName(CardModel card)
    {
        return CardTypeName(card.Type);
    }

    public static string BuildCardEventPath(string? pool, string card)
    {
        if (string.IsNullOrEmpty(pool))
            return $"event:/{ModId}/sfx/cards/{card}";
        return $"event:/{ModId}/sfx/cards/{pool}/{card}";
    }

}
