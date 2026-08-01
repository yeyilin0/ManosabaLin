using MegaCrit.Sts2.Core.Nodes.Audio;

namespace ManosabaLin.MainMenu;

public sealed record MainMenuBackgroundOption(string ImagePath, string? MusicEventPath);

public static class MainMenuBackgroundOptions
{
    public static readonly IReadOnlyList<MainMenuBackgroundOption> All =
    [
        new($"{MainFile.ResPath}/images/ui/xiluo.png", "event:/ManosabaLin/music/biaoti"),
        new($"{MainFile.ResPath}/images/ui/aima.png", "event:/ManosabaLin/music/biaoti"),
        new($"{MainFile.ResPath}/images/ui/amxiluo.png", "event:/ManosabaLin/music/bloom"),
        new($"{MainFile.ResPath}/images/ui/oneyear.png", "event:/ManosabaLin/music/oneyear"),
    ];

    private static MainMenuBackgroundOption? activeOption;

    public static MainMenuBackgroundOption PickRandom()
    {
        return All.Count == 0
            ? new($"{MainFile.ResPath}/mod_image.png", null)
            : All[Random.Shared.Next(All.Count)];
    }

    public static MainMenuBackgroundOption PickNewActive()
    {
        activeOption = PickRandom();
        return activeOption;
    }

    public static MainMenuBackgroundOption GetActiveOrPick()
    {
        return activeOption ??= PickRandom();
    }

    public static void TryPlayMusic(MainMenuBackgroundOption option)
    {
        if (string.IsNullOrWhiteSpace(option.MusicEventPath))
        {
            return;
        }

        try
        {
            NAudioManager.Instance?.StopMusic();
            NAudioManager.Instance?.PlayMusic(option.MusicEventPath);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[MainMenuBackground] Failed to play '{option.MusicEventPath}': {ex.Message}");
        }
    }
}
