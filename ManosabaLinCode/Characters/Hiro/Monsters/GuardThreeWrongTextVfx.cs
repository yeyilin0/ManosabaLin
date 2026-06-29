using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ManosabaLin.Characters.Hiro.Monsters;

public static class GuardThreeWrongTextVfx
{
    private const string Text = "[color=#DC143C][b]你是错误[/b][/color]";

    public static void Spawn(Creature creature, int count)
    {
        var room = NCombatRoom.Instance;
        var creatureNode = room?.GetCreatureNode(creature);
        if (room == null || creatureNode == null)
            return;

        for (var i = 0; i < count; i++)
        {
            var vfx = CreateLabel();
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

    private static Control CreateLabel()
    {
        var container = new Control
        {
            CustomMinimumSize = new Vector2(360f, 90f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            PivotOffset = new Vector2(180f, 45f),
            Scale = Vector2.One * Rng.Chaotic.NextFloat(1.1f, 1.35f),
            RotationDegrees = Rng.Chaotic.NextFloat(-8f, 8f),
            Modulate = Colors.White
        };

        var label = new RichTextLabel
        {
            BbcodeEnabled = true,
            Text = Text,
            FitContent = true,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.Off,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new Vector2(360f, 90f)
        };

        label.AddThemeFontSizeOverride("normal_font_size", 54);
        label.AddThemeFontSizeOverride("bold_font_size", 54);
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
        label.AddThemeConstantOverride("shadow_outline_size", 3);
        label.AddThemeConstantOverride("shadow_offset_x", 3);
        label.AddThemeConstantOverride("shadow_offset_y", 3);

        container.AddChild(label);
        return container;
    }
}
