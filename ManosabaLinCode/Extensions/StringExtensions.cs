namespace ManosabaLin.Extensions;

public static class StringExtensions
{
    public static string ImagePath(this string path)
    {
        return BuildResPath("images", path);
    }

    public static string CharacterImgPath(this string path, string character)
    {
        return BuildResPath("images", "characters", character, path);
    }

    public static string CharacterAudioPath(this string path)
    {
        return BuildRelativePath("characters", path);
    }

    public static string CharacterScenePath(this string path, string character)
    {
        return BuildResPath("scenes", character, path);
    }

    public static string PotionImagePath(this string path)
    {
        return BuildResPath("images", "potions", path);
    }

    public static string CardsImagePath(this string path)
    {
        return BuildResPath("images", "cards", path);
    }

    public static string CardsAudioPath(this string path)
    {
        return BuildRelativePath("cards", path);
    }

    public static string BgmAudioPath(this string path)
    {
        return BuildRelativePath("bgm", path);
    }

    public static string BigCardsImagePath(this string path)
    {
        return BuildResPath("images", "cards", "big", path);
    }

    public static string PowerImagePath(this string path)
    {
        return BuildResPath("images", "powers", path);
    }

    public static string BigPowerImagePath(this string path)
    {
        return BuildResPath("images", "powers", "big", path);
    }

    public static string RelicImagePath(this string path)
    {
        return BuildResPath("images", "relics", path);
    }

    public static string BigRelicImagePath(this string path)
    {
        return BuildResPath("images", "relics", "big", path);
    }

    public static string CharacterUiPath(this string path)
    {
        return BuildResPath("images", "charui", path);
    }

    private static string BuildResPath(params string[] segments)
    {
        return string.Join(
            "/",
            new[] { MainFile.ResPath.TrimEnd('/') }
                .Concat(segments.Select(NormalizeSegment).Where(static s => !string.IsNullOrEmpty(s))));
    }

    private static string BuildRelativePath(params string[] segments)
    {
        return string.Join(
            "/",
            segments.Select(NormalizeSegment).Where(static s => !string.IsNullOrEmpty(s)));
    }

    private static string NormalizeSegment(string segment)
    {
        return segment.Replace('\\', '/').Trim('/');
    }
}