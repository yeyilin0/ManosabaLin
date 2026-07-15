namespace ManosabaLin.Audio.Services;

using static AudioEventPathProvider;
using static FmodHelper;

public static class CardAudioService
{
    public static string? GetCardPlayEvent(CardModel card)
    {
        if (card is not ManosabaCardTemplate) return null;
        if (card.VisualCardPool is LinCardPool) return null;

        var pool = CardPoolName(card);
        var owner = CardOwnerName(card);
        var slug = CardNameSlug(card);

        string path;

        path = BuildCardEventPath(pool, slug);
        if (IsEventExists(path)) return path;

        path = BuildCardEventPath(owner, slug);
        if (IsEventExists(path)) return path;

        path = BuildCardEventPath("common", slug);
        if (IsEventExists(path)) return path;

        path = BuildCardEventPath(null, slug);
        if (IsEventExists(path)) return path;

        if (CardTypeName(card) is { } type)
        {
            path = BuildCardEventPath(pool, $"fallback/{type}");
            if (IsEventExists(path)) return path;

            path = BuildCardEventPath(owner, $"fallback/{type}");
            if (IsEventExists(path)) return path;
        }

        return null;
    }

    public static void PlayCardPlaySfx(CardModel card)
    {
        if (card is ManosabaCardTemplate && card.VisualCardPool is LinCardPool) return;

        if (card is ICustomCardPlaySfx customSfxCard)
        {
            customSfxCard.PlayCustomCardPlaySfx();
            return;
        }
        if (card is not ManosabaCardTemplate) return;
        if (GetCardPlayEvent(card) is { } path)
        {
            SfxCmd.Play(path);
        }
    }

    public interface ICustomCardPlaySfx
    {
        void PlayCustomCardPlaySfx();
    }

}
