using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Timer = Godot.Timer;

namespace ManosabaLin.Characters.Hiro.Monsters;

public static class GuardThreePhaseTransitionOverlay
{
    private const string GuardImagePath =
        "res://ManosabaLin/scenes/backgrounds/guard_three_encounter/images/guard.png";

    private const string GuardBgPath =
        "res://ManosabaLin/scenes/backgrounds/guard_three_encounter/images/guard_three_bg_00_a.png";

    private const string WrongText = "你是错误的!!";
    private const string SubtitleText = "我 要 驱 除 一 切 错 误 ! !";
    private const string FinalText = "我 要 纠 正 你，重 新 掌 控 正 确 的 世 界 ！！！";

    public static async Task PlayAsync()
    {
        var room = NCombatRoom.Instance;
        if (room == null) return;

        var overlay = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 2000
        };
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        room.AddChild(overlay);

        var viewportSize = overlay.GetViewportRect().Size;

        var dim = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.96f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.AddChild(dim);

        // 1. "你是错误的!!"
        var wrongLabels = await ShowCenteredText(overlay, viewportSize, WrongText, 60, 0.11f);
        await overlay.ToSignal(overlay.GetTree().CreateTimer(2.0), Timer.SignalName.Timeout);
        foreach (var label in wrongLabels)
            label.QueueFree();

        // 2. oneone.png
        await ShowFullImage(overlay, viewportSize, "res://ManosabaLin/scenes/backgrounds/guard_three_encounter/images/oneone.png", 1f);

        // 3. twotwo.png
        await ShowFullImage(overlay, viewportSize, "res://ManosabaLin/scenes/backgrounds/guard_three_encounter/images/twotwo.png", 1f);

        // 4. "我 要 驱 除 一 切 错 误 ! !"
        var subtitleLabels = await ShowCenteredText(overlay, viewportSize, SubtitleText, 100, 0.11f);
        await overlay.ToSignal(overlay.GetTree().CreateTimer(1.0), Timer.SignalName.Timeout);
        foreach (var label in subtitleLabels)
            label.QueueFree();

        // 5. threethree.png
        var rect = await ShowFullImageReturn(overlay, viewportSize, "res://ManosabaLin/scenes/backgrounds/guard_three_encounter/images/threethree.png", 1f);

        // 6. 在 threethree.png 上面叠加最终文字
        var finalLabels = await ShowCenteredText(overlay, viewportSize, FinalText, 50, 0.1f);
        await overlay.ToSignal(overlay.GetTree().CreateTimer(2.0), Timer.SignalName.Timeout);

        overlay.QueueFree();
    }

    private static async Task<List<Label>> ShowCenteredText(Control overlay, Vector2 viewportSize, string text, int fontSize, float charDelay)
    {
        var labels = new List<Label>();
        var chars = text.ToCharArray();
        const float charStep = 62f;
        var startX = viewportSize.X / 2f - (chars.Length - 1) * charStep / 2f;
        var y = viewportSize.Y / 2f;

        for (var i = 0; i < chars.Length; i++)
        {
            var label = new Label
            {
                Text = chars[i].ToString(),
                Modulate = new Color(0.86f, 0.02f, 0.07f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Position = new Vector2(startX + i * charStep, y)
            };
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.85f));
            label.AddThemeConstantOverride("shadow_outline_size", 5);
            label.AddThemeConstantOverride("shadow_offset_x", 3);
            label.AddThemeConstantOverride("shadow_offset_y", 3);

            overlay.AddChild(label);
            labels.Add(label);
            StartJitter(label);
            await overlay.ToSignal(overlay.GetTree().CreateTimer(charDelay), Timer.SignalName.Timeout);
        }

        return labels;
    }

    private static async Task ShowFullImage(Control overlay, Vector2 viewportSize, string path, float duration)
    {
        var tex = PreloadManager.Cache.GetTexture2D(path);
        if (tex == null) return;

        var rect = new TextureRect
        {
            Texture = tex,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale
        };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.AddChild(rect);

        await overlay.ToSignal(overlay.GetTree().CreateTimer(duration), Timer.SignalName.Timeout);
        rect.QueueFree();
    }

    private static async Task<TextureRect?> ShowFullImageReturn(Control overlay, Vector2 viewportSize, string path, float duration)
    {
        var tex = PreloadManager.Cache.GetTexture2D(path);
        if (tex == null) return null;

        var rect = new TextureRect
        {
            Texture = tex,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale
        };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.AddChild(rect);

        await overlay.ToSignal(overlay.GetTree().CreateTimer(duration), Timer.SignalName.Timeout);

        return rect;
    }

    private static void StartJitter(Control label)
    {
        var basePosition = label.Position;
        var tween = label.CreateTween().SetLoops();
        tween.TweenCallback(Callable.From(() =>
        {
            label.Position = basePosition + new Vector2(
                GD.Randf() * 8f - 4f,
                GD.Randf() * 8f - 4f);
        }));
        tween.TweenInterval(0.045);
    }
}
