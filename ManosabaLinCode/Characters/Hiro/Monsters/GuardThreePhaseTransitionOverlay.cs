using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Timer = Godot.Timer;

namespace ManosabaLin.Characters.Hiro.Monsters;

public static class GuardThreePhaseTransitionOverlay
{
    private const string GuardImagePath =
        "res://ManosabaLin/scenes/backgrounds/guard_three_encounter/images/guard.png";

    public const string PhaseTwoBgPath =
        "res://ManosabaLin/scenes/backgrounds/guard_three_encounter/images/guard_three_bg_phase2.png";

    private const string WrongText = "你是错误的!!";
    private const string SubtitleText = "我 要 驱 除 一 切 错 误 ! ! !";

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
            Color = new Color(0f, 0f, 0f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.AddChild(dim);

        await FadeDim(overlay, dim, 0.96f, 0.45f);

        var wrongLabels = await ShowCenteredText(overlay, viewportSize, WrongText, 60);
        await overlay.ToSignal(overlay.GetTree().CreateTimer(2.0), Timer.SignalName.Timeout);
        foreach (var label in wrongLabels)
            label.QueueFree();

        await ShowFullImage(overlay, viewportSize, GuardImagePath, 1f);

        await ShowFullImage(overlay, viewportSize, PhaseTwoBgPath, 1.5f);

        var subtitleLabels = await ShowCenteredText(overlay, viewportSize, SubtitleText, 100);

        await overlay.ToSignal(overlay.GetTree().CreateTimer(2.2), Timer.SignalName.Timeout);

        ApplyPhaseTwoBackground(room);
        foreach (var label in subtitleLabels)
            label.QueueFree();

        await FadeDim(overlay, dim, 0f, 0.65f);

        overlay.QueueFree();
    }

    private static void ApplyPhaseTwoBackground(NCombatRoom room)
    {
        var layer = room.Background.GetNodeOrNull<Control>("Layer_00");
        var rect = layer?.FindChild("A", true, false) as TextureRect;
        if (rect == null) return;

        var tex = PreloadManager.Cache.GetTexture2D(PhaseTwoBgPath);
        if (tex != null)
            rect.Texture = tex;
    }

    private static async Task FadeDim(Control overlay, ColorRect dim, float alpha, double duration)
    {
        var tween = overlay.CreateTween();
        tween.TweenProperty(dim, (NodePath)"color", new Color(0f, 0f, 0f, alpha), duration);
        await overlay.ToSignal(tween, Tween.SignalName.Finished);
    }

    private static async Task<List<Label>> ShowCenteredText(Control overlay, Vector2 viewportSize, string text, int fontSize)
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
            await overlay.ToSignal(overlay.GetTree().CreateTimer(0.11), Timer.SignalName.Timeout);
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
