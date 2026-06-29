using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ManosabaLin.Characters.Hiro.Monsters;

public static class GuardThreePhaseTransitionOverlay
{
    private const string GuardSheetPath =
        "res://ManosabaLin/scenes/backgrounds/guard_three_encounter/images/guard.png";

    private const int Columns = 4;
    private const int Rows = 2;
    private const int FrameCount = Columns * Rows;
    private const float FrameRate = 16f;
    private const string SubtitleText = "\u6211\u8981\u9A71\u9664\u4E00\u5207\u9519\u8BEF!!";
    private const string Subtitle = "我要驱除一切错误!!";

    public static async Task PlayAsync()
    {
        var room = NCombatRoom.Instance;
        if (room == null)
            return;

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

        var sprite = new AnimatedSprite2D
        {
            Centered = true,
            Position = viewportSize / 2f,
            SpriteFrames = CreateSpriteFrames()
        };

        var frameSize = new Vector2(1024f, 1024f);
        var coverScale = Mathf.Max(viewportSize.X / frameSize.X, viewportSize.Y / frameSize.Y);
        sprite.Scale = Vector2.One * coverScale;
        overlay.AddChild(sprite);

        sprite.Play("default");
        await sprite.ToSignal(sprite, AnimatedSprite2D.SignalName.AnimationFinished);

        await ShowSubtitle(overlay, viewportSize);
        overlay.QueueFree();
    }

    private static SpriteFrames CreateSpriteFrames()
    {
        var sheet = PreloadManager.Cache.GetTexture2D(GuardSheetPath);
        var spriteFrames = new SpriteFrames();
        spriteFrames.AddAnimation("default");
        spriteFrames.SetAnimationLoop("default", false);
        spriteFrames.SetAnimationSpeed("default", FrameRate);

        var frameWidth = sheet.GetWidth() / Columns;
        var frameHeight = sheet.GetHeight() / Rows;

        for (var loop = 0; loop < 2; loop++)
        {
            for (var index = 0; index < FrameCount; index++)
            {
                var x = index % Columns;
                var y = index / Columns;
                var atlas = new AtlasTexture
                {
                    Atlas = sheet,
                    Region = new Rect2(x * frameWidth, y * frameHeight, frameWidth, frameHeight)
                };
                spriteFrames.AddFrame("default", atlas);
            }
        }

        return spriteFrames;
    }

    private static async Task ShowSubtitle(Control overlay, Vector2 viewportSize)
    {
        var chars = SubtitleText.ToCharArray();
        const float charStep = 62f;
        var startX = viewportSize.X / 2f - (chars.Length - 1) * charStep / 2f;
        var y = viewportSize.Y * 0.78f;

        for (var i = 0; i < chars.Length; i++)
        {
            var label = CreateSubtitleLabel(chars[i].ToString());
            label.Position = new Vector2(startX + i * charStep, y);
            overlay.AddChild(label);
            StartJitter(label);
            await overlay.ToSignal(overlay.GetTree().CreateTimer(0.11), Godot.Timer.SignalName.Timeout);
        }

        await overlay.ToSignal(overlay.GetTree().CreateTimer(2.2), Godot.Timer.SignalName.Timeout);
    }

    private static Label CreateSubtitleLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            Modulate = new Color(0.86f, 0.02f, 0.07f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        label.AddThemeFontSizeOverride("font_size", 74);
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.85f));
        label.AddThemeConstantOverride("shadow_outline_size", 5);
        label.AddThemeConstantOverride("shadow_offset_x", 3);
        label.AddThemeConstantOverride("shadow_offset_y", 3);
        return label;
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
