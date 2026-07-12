using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using ManosabaLin.Audio.Services;

namespace ManosabaLin.MainMenu.Patches;

[HarmonyPatch(typeof(NMainMenuBg))]
public static class NMainMenuBackgroundPatch
{
    private const string BackgroundNodeName = "ManosabaLin_MainMenuBackground";

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void Ready(NMainMenuBg __instance)
    {
        if (!MainMenuBackgroundSettingsService.IsEnabled)
        {
            return;
        }

        Build(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NMainMenu), "_Ready")]
    public static void MainMenuReady()
    {
        if (!MainMenuBackgroundSettingsService.IsEnabled)
        {
            return;
        }

        var option = MainMenuBackgroundOptions.GetActiveOrPick();
        TryApplyMusic(option);
    }

    [HarmonyPostfix]
    [HarmonyPatch("ShowLogo")]
    public static void ShowLogo(NMainMenuBg __instance)
    {
        if (!MainMenuBackgroundSettingsService.IsEnabled)
        {
            return;
        }

        Build(__instance);
    }

    private static void Build(NMainMenuBg host)
    {
        Clean(host);

        var option = MainMenuBackgroundOptions.PickNewActive();
        var texture = ResourceLoader.Load<Texture2D>(option.ImagePath);
        if (texture == null)
        {
            MainFile.Logger.Warn($"[MainMenuBackground] Missing texture: {option.ImagePath}");
            return;
        }

        var originalBg = host.GetNodeOrNull<Control>("BgContainer");
        if (originalBg != null)
        {
            originalBg.Visible = false;
        }

        var originalLogo = host.GetNodeOrNull<Node2D>("%Logo");
        if (originalLogo != null)
        {
            originalLogo.Visible = false;
        }

        var background = new TextureRect
        {
            Name = BackgroundNodeName,
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = -100,
            ZAsRelative = false,
        };
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        host.AddChild(background);
        host.MoveChild(background, 0);

        TryApplyMusic(option);
    }

    private static void TryApplyMusic(MainMenuBackgroundOption option)
    {
        if (string.IsNullOrWhiteSpace(option.MusicEventPath))
        {
            return;
        }

        if (!FmodHelper.IsEventExists(option.MusicEventPath))
        {
            MainFile.Logger.Warn($"[MainMenuBackground] Missing music event: {option.MusicEventPath}");
            return;
        }

        MainMenuBackgroundOptions.TryPlayMusic(option);
    }

    private static void Clean(NMainMenuBg host)
    {
        var existing = host.GetNodeOrNull<Node>(BackgroundNodeName);
        if (existing != null)
        {
            host.RemoveChild(existing);
            existing.QueueFree();
        }
    }
}
