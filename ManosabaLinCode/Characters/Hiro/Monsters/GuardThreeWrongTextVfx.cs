using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ManosabaLin.Characters.Hiro.Monsters;

public static class GuardThreeWrongTextVfx
{
    private const string WrongText = "[color=#DC143C][b]你 是 错 误 的 ![/b][/color]";

    private static readonly string[] PhaseOneLines =
    [
        "■ 是 ■ ■ 的",
        "你 ■ ■ 误 ■ ■",
        "你 ■ 错 误 ■"
    ];

    public static void SpawnFloatingWrong(Creature creature, int count)
    {
        SpawnFloating(creature, WrongText, count, new Color(0.86f, 0.02f, 0.07f), 70);
    }

    public static void SpawnPhaseOneLine(Creature creature)
    {
        var room = NCombatRoom.Instance;
        if (room == null) return;

        var viewportSize = room.GetViewportRect().Size;
        var index = Rng.Chaotic.NextInt(PhaseOneLines.Length);
        var vfx = CreateLabel(
            $"[color=#6E0B16]{PhaseOneLines[index]}[/color]",
            new Color(0.43f, 0.04f, 0.09f),
            90);
        room.CombatVfxContainer.AddChild(vfx);

        vfx.GlobalPosition = new Vector2(
            Rng.Chaotic.NextFloat(100f, viewportSize.X - 100f),
            Rng.Chaotic.NextFloat(100f, viewportSize.Y - 200f));

        var floatDistance = Rng.Chaotic.NextFloat(100f, 200f);
        var duration = Rng.Chaotic.NextFloat(2.5f, 3.5f);

        var tween = vfx.CreateTween().SetParallel();
        tween.TweenProperty(vfx, "position:y", vfx.Position.Y - floatDistance, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(vfx, "modulate:a", 0f, duration)
            .SetDelay(0.8f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(vfx, "scale", Vector2.One * 0.5f, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenCallback(Callable.From(vfx.QueueFree)).SetDelay(duration);
    }

    public static void SpawnPersistentWrong(Creature creature, int count)
    {
        var room = NCombatRoom.Instance;
        if (room == null) return;

        var viewportSize = room.GetViewportRect().Size;

        var colors = new[]
        {
            new Color(0.86f, 0.02f, 0.07f),  // 鲜红
            new Color(0.47f, 0.04f, 0.09f),  // 暗红
        };

        for (var i = 0; i < count; i++)
        {
            var color = colors[Rng.Chaotic.NextInt(colors.Length)];
            var vfx = CreateLabel(WrongText, color, 20);
            room.CombatVfxContainer.AddChild(vfx);

            vfx.GlobalPosition = new Vector2(
                Rng.Chaotic.NextFloat(80f, viewportSize.X - 80f),
                Rng.Chaotic.NextFloat(80f, viewportSize.Y - 200f));
            StartPersistentJitter(vfx);
        }
    }

    private static void SpawnFloating(Creature creature, string text, int count, Color color, int fontSize)
    {
        var room = NCombatRoom.Instance;
        var creatureNode = room?.GetCreatureNode(creature);
        if (room == null || creatureNode == null)
            return;

        for (var i = 0; i < count; i++)
        {
            var vfx = CreateLabel(text, color, fontSize);
            room.CombatVfxContainer.AddChild(vfx);

            vfx.GlobalPosition = creatureNode.VfxSpawnPosition
                                 + new Vector2(
                                     Rng.Chaotic.NextFloat(-170f, 170f),
                                     Rng.Chaotic.NextFloat(-150f, -40f));

            var floatDistance = Rng.Chaotic.NextFloat(70f, 130f);
            var duration = Rng.Chaotic.NextFloat(1.6f, 2.2f);
            var delay = i * 0.08f;

            var tween = vfx.CreateTween().SetParallel();
            tween.TweenProperty(vfx, "position:y", vfx.Position.Y - floatDistance, duration)
                .SetDelay(delay)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
            tween.TweenProperty(vfx, "modulate:a", 0f, duration)
                .SetDelay(delay + 0.45f)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Quad);
            tween.TweenProperty(vfx, "scale", Vector2.One * 0.72f, duration)
                .SetDelay(delay)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quad);
            tween.TweenCallback(Callable.From(vfx.QueueFree)).SetDelay(delay + duration);
        }
    }

    private static Control CreateLabel(string text, Color color, int fontSize)
    {
        var container = new Control
        {
            CustomMinimumSize = new Vector2(420f, 90f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            PivotOffset = new Vector2(210f, 45f),
            Scale = Vector2.One * Rng.Chaotic.NextFloat(1.1f, 1.35f),
            RotationDegrees = Rng.Chaotic.NextFloat(-8f, 8f),
            Modulate = color
        };

        var label = new RichTextLabel
        {
            BbcodeEnabled = true,
            Text = text,
            FitContent = true,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.Off,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new Vector2(420f, 90f)
        };

        label.AddThemeFontSizeOverride("normal_font_size", fontSize);
        label.AddThemeFontSizeOverride("bold_font_size", fontSize);
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
        label.AddThemeConstantOverride("shadow_outline_size", 3);
        label.AddThemeConstantOverride("shadow_offset_x", 3);
        label.AddThemeConstantOverride("shadow_offset_y", 3);

        container.AddChild(label);
        return container;
    }

    private static void StartPersistentJitter(Control vfx)
    {
        var basePosition = vfx.Position;
        var tween = vfx.CreateTween().SetLoops();
        tween.TweenCallback(Callable.From(() =>
        {
            vfx.Position = basePosition + new Vector2(
                Rng.Chaotic.NextFloat(-3f, 3f),
                Rng.Chaotic.NextFloat(-3f, 3f));
        }));
        tween.TweenInterval(0.08);
    }
}
