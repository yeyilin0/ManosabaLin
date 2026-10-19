using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Saves;
using ManosabaLin.Characters.Common.Vfx;

namespace ManosabaLin.Characters.Hiro.Vfx;

/// <summary>Error cuts/cracks; qualifying cards retain glass until TheEnd consumes it.</summary>
public partial class ErrorGlassVfx : Node, IDisposable
{
    public const string ScenePath = "res://ManosabaLin/scenes/Hiro/error_glass/error_glass.tscn";
    private readonly TaskCompletionSource<bool> _impact = new();
    private readonly TaskCompletionSource _finished = new();
    private readonly TaskCompletionSource<bool> _prepared = new();
    private bool _disposed;
    private bool _fieldReplaced;
    private double _elapsed;
    private CanvasLayer? _effect;
    public Task<bool> Impact => _impact.Task;
    public Task Finished => _finished.Task;
    public Task<bool> Prepared => _prepared.Task;
    public bool IsRetained { get; private set; }

    public static ErrorGlassVfx? Play(Creature caster, bool holdFullscreen)
        => Create(caster, holdFullscreen, false);

    public static ErrorGlassVfx? Burst(Creature caster)
    {
        if (TestMode.IsOn || caster.CombatState?.IsLiveCombat() != true)
            return null;
        var existing = NCombatRoom.Instance?.GetNodeOrNull<ErrorGlassVfx>("ErrorGlassPersistent");
        if (existing != null && GodotObject.IsInstanceValid(existing) && !existing.IsQueuedForDeletion()
            && !existing._fieldReplaced
            && existing._effect != null && GodotObject.IsInstanceValid(existing._effect))
        {
            existing.IsRetained = false;
            CombatFieldVfxSlot.Release(existing);
            // A burst is now a transient action, not the room's retained glass.
            // A later Error must not mistake it for a replaceable field and
            // finish TheEnd's awaited burst early.
            existing.Name = "ErrorGlassBurstPlayback";
            existing._elapsed = 0;
            existing._effect.Call("begin_burst");
            return existing;
        }
        // Generated/recovered TheEnd cards can be played without a surviving Error overlay.
        return Create(caster, false, true);
    }

    public void RetainUntilTheEnd()
    {
        if (!_fieldReplaced && !_disposed && GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion())
            IsRetained = true;
    }

    private void ReplaceRetainedField()
    {
        if (_fieldReplaced || _disposed)
            return;
        _fieldReplaced = true;
        IsRetained = false;
        // Keep the opening's original impact/prepared timings. Error awaits these
        // signals to apply damage and finish generating TheEnd, even if another
        // field has already taken over the room's persistent presentation.
        if (_effect != null && GodotObject.IsInstanceValid(_effect))
            _effect.Call("suppress_retained");
        if (_prepared.Task.IsCompleted)
            Dispose();
    }

    private void OnPrepared()
    {
        _prepared.TrySetResult(true);
        if (_fieldReplaced)
            Dispose();
    }

    private static ErrorGlassVfx? Create(Creature caster, bool holdFullscreen, bool startInBurst)
    {
        if (TestMode.IsOn || caster.CombatState?.IsLiveCombat() != true)
            return null;
        var room = NCombatRoom.Instance;
        if (room == null || !GodotObject.IsInstanceValid(room) || !room.IsInsideTree())
            return null;

        var playback = new ErrorGlassVfx { Name = holdFullscreen ? "ErrorGlassPersistent" : "ErrorGlassPlayback" };
        try
        {
            var scene = ResourceLoader.Load<PackedScene>(ScenePath);
            if (scene == null) throw new InvalidOperationException("Glass effect scene missing.");
            var effect = scene.Instantiate<CanvasLayer>();
            playback._effect = effect;
            effect.Set("hold_fullscreen", holdFullscreen);
            effect.Set("start_in_burst", startInBurst);
            effect.Set("sfx_gain", Mathf.Pow(SaveManager.Instance.SettingsSave.VolumeSfx, 2f));
            playback.AddChild(effect);
            if (!effect.HasSignal("impact") || !effect.HasSignal("prepared") || !effect.HasSignal("finished")
                || !effect.HasMethod("begin_burst") || !effect.HasMethod("suppress_retained"))
                throw new InvalidOperationException("Glass effect script did not load.");
            effect.Connect("impact", Callable.From(() => playback._impact.TrySetResult(true)));
            effect.Connect("prepared", Callable.From(playback.OnPrepared));
            effect.Connect("finished", Callable.From(() =>
            {
                CombatFieldVfxSlot.Release(playback);
                playback._finished.TrySetResult();
                if (GodotObject.IsInstanceValid(playback) && !playback.IsQueuedForDeletion())
                    playback.QueueFree();
            }));
            effect.TreeExiting += () =>
            {
                CombatFieldVfxSlot.Release(playback);
                playback._impact.TrySetResult(false);
                playback._prepared.TrySetResult(false);
                playback._finished.TrySetResult();
            };
            if (holdFullscreen)
            {
                var previous = room.GetNodeOrNull<ErrorGlassVfx>("ErrorGlassPersistent");
                if (previous != null && GodotObject.IsInstanceValid(previous))
                {
                    // Free the lookup name without exiting the tree: a pending
                    // Error must still receive its original timeline signals.
                    previous.Name = "ErrorGlassSuperseded";
                    previous.ReplaceRetainedField();
                }
            }
            room.AddChild(playback);
            if (holdFullscreen && !CombatFieldVfxSlot.Claim(playback, playback.ReplaceRetainedField))
                playback.ReplaceRetainedField();
            return playback;
        }
        catch (Exception ex)
        {
            Log.Warn($"Error glass VFX unavailable: {ex.Message}");
            playback.Dispose();
            return null;
        }
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_effect != null && GodotObject.IsInstanceValid(_effect))
        {
            var muted = SaveManager.Instance.PrefsSave.MuteInBackground && !DisplayServer.WindowIsFocused();
            _effect.Set("sfx_gain", muted ? 0f : Mathf.Pow(SaveManager.Instance.SettingsSave.VolumeSfx, 2f));
        }
        if (IsRetained || _elapsed <= 6.15 || _finished.Task.IsCompleted)
            return;
        // Fail open for presentation errors; never leave a paid card hanging.
        Log.Warn("Error glass VFX exceeded its timeline; releasing overlay and resolving card.");
        _impact.TrySetResult(true);
        _prepared.TrySetResult(false);
        Dispose();
    }

    public override void _ExitTree()
    {
        CombatFieldVfxSlot.Release(this);
        IsRetained = false;
        _impact.TrySetResult(false);
        _prepared.TrySetResult(false);
        _finished.TrySetResult();
    }

    public new void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CombatFieldVfxSlot.Release(this);
        IsRetained = false;
        _impact.TrySetResult(false);
        _prepared.TrySetResult(false);
        _finished.TrySetResult();
        if (GodotObject.IsInstanceValid(this) && !IsQueuedForDeletion())
        {
            if (IsInsideTree()) QueueFree();
            else Free();
        }
        GC.SuppressFinalize(this);
    }
}
