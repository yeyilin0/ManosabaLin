namespace ManosabaLin.Audio.Services;

public static class AudioEventPathProvider
{
    public static string CardPoolName(CardModel card)
    {
        return card.VisualCardPool.Title.ToLowerInvariant();
    }

    public static string CardOwnerName(CardModel card)
    {
        return card.Owner.Character.GetType().Name.ToLowerInvariant();
    }

    public static string? CardHomeName(CardModel card)
    {
        var ns = card.GetType().Namespace ?? "";
        if (ns.Contains(".Characters.Ananlin.")) return "ananlin";
        if (ns.Contains(".Characters.Ema.")) return "emalin";
        if (ns.Contains(".Characters.Hiro.")) return "hiro";
        if (ns.Contains(".Characters.Sherrylin.")) return "sherrylin";
        if (ns.Contains(".Characters.Yalisalin.")) return "yalisalin";
        return null;
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
