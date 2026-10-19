using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace ManosabaLin.Characters.Yalisalin.Vfx;

/// <summary>
/// Owns the fullscreen scene and its two gameplay synchronization points.
/// The room owns this node, so abandoning combat also stops audio and resolves waits.
/// </summary>
public partial class ExplosionMagicVfx : Node, IDisposable
{
    public const string ScenePath = "res://ManosabaLin/scenes/Yalisalin/explosion_magic/explosion_magic.tscn";
    private readonly TaskCompletionSource<bool> _impact = new();
    private readonly TaskCompletionSource _finished = new();
    private CanvasLayer? _effect;
    private bool _disposed;
    private double _elapsed;

    public Task<bool> Impact => _impact.Task;
    public Task Finished => _finished.Task;

    public static ExplosionMagicVfx? Play(Creature caster, IReadOnlyList<Creature> enemies)
    {
        // Simulation/replay-without-visuals must never wait on a Godot signal.
        if (TestMode.IsOn || caster.CombatState?.IsLiveCombat() != true)
            return null;
        var room = NCombatRoom.Instance;
        if (room == null || !GodotObject.IsInstanceValid(room) || !room.IsInsideTree())
            return null;

        var playback = new ExplosionMagicVfx { Name = "ExplosionMagicPlayback" };
        try
        {
            var scene = ResourceLoader.Load<PackedScene>(ScenePath);
            if (scene == null)
            {
                playback.Dispose();
                return null;
            }
            var effect = scene.Instantiate<CanvasLayer>();
            playback._effect = effect;
            if (!effect.HasSignal("impact") || !effect.HasSignal("finished") || !effect.HasMethod("seek_preview"))
                throw new InvalidOperationException("Explosion scene script did not load correctly.");
            var viewportSize = room.GetViewportRect().Size;
            var source = ScreenPosition(caster, viewportSize) ?? new Vector2(0.21f, 0.54f);
            var positions = enemies.Select(e => ScreenPosition(e, viewportSize))
                .Where(p => p.HasValue).Select(p => p!.Value).ToArray();
            var target = positions.Length > 0
                ? positions.Aggregate(Vector2.Zero, (sum, p) => sum + p) / positions.Length
                : new Vector2(0.77f, source.Y);
            source.X = Mathf.Clamp(source.X, 0.08f, 0.40f);
            target.X = Mathf.Clamp(target.X, source.X + 0.34f, 0.90f);
            effect.Set("source_uv", source);
            effect.Set("target_uv", target);
            effect.Set("sfx_gain", SfxGain());
            effect.Connect("impact", Callable.From(() => playback._impact.TrySetResult(true)));
            effect.Connect("finished", Callable.From(() =>
            {
                playback._finished.TrySetResult();
                if (GodotObject.IsInstanceValid(playback) && !playback.IsQueuedForDeletion())
                    playback.QueueFree();
            }));
            // Catch early disposal even when the scene never reached its impact cue.
            effect.TreeExiting += () =>
            {
                playback._impact.TrySetResult(false);
                playback._finished.TrySetResult();
            };
            playback.AddChild(effect);
            room.AddChild(playback);
            return playback;
        }
        catch (Exception ex)
        {
            // A presentation failure must not swallow a paid card's gameplay effect.
            Log.Warn($"ExplosionMagic VFX unavailable: {ex.Message}");
            playback.Dispose();
            return null;
        }
    }

    private static Vector2? ScreenPosition(Creature creature, Vector2 viewportSize)
    {
        var anchor = creature.GetCreatureNode()?.Visuals?.VfxSpawnPosition;
        if (anchor == null || viewportSize.X <= 0 || viewportSize.Y <= 0)
            return null;
        return anchor.GetGlobalTransformWithCanvas().Origin / viewportSize;
    }

    private static float SfxGain()
    {
        var prefs = SaveManager.Instance.SettingsSave;
        // Master bus already carries Godot's master slider. Only add the SFX curve.
        return Mathf.Pow(Mathf.Clamp(prefs.VolumeSfx, 0f, 1f), 2f);
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_elapsed > 9 && !_finished.Task.IsCompleted)
        {
            // A broken scene must not leave the paid card waiting forever.
            Log.Warn("ExplosionMagic VFX exceeded its 7-second timeline; resolving gameplay and releasing overlay.");
            _impact.TrySetResult(true);
            Dispose();
            return;
        }
        if (_effect == null || !GodotObject.IsInstanceValid(_effect))
            return;
        var muted = SaveManager.Instance.PrefsSave.MuteInBackground && !DisplayServer.WindowIsFocused();
        _effect.Set("sfx_gain", muted ? 0f : SfxGain());
    }

    public override void _ExitTree()
    {
        _impact.TrySetResult(false);
        _finished.TrySetResult();
    }

    public new void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _impact.TrySetResult(false);
        _finished.TrySetResult();
        if (GodotObject.IsInstanceValid(this) && !IsQueuedForDeletion())
        {
            if (IsInsideTree()) QueueFree();
            else Free();
        }
        GC.SuppressFinalize(this);
    }
}
