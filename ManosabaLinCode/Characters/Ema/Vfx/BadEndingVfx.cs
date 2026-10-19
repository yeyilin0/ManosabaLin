using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using ManosabaLin.Characters.Common.Vfx;

namespace ManosabaLin.Characters.Ema.Vfx;

/// <summary>Room-owned presentation. Visual failures always release the card action.</summary>
public partial class BadEndingVfx : Node, IDisposable
{
    internal const string ScenePath = "res://ManosabaLin/scenes/Emalin/bad_ending/bad_ending.tscn";
    public static IEnumerable<string> AssetPaths => [ScenePath];
    private TaskCompletionSource _impact = new();
    private TaskCompletionSource _opening = new();
    private Creature? _owner;
    private Node2D? _stage;
    private bool _dormant;
    private bool _disposed;
    private bool _finishing;
    private double _elapsed;
    private double _finishAge;
    private double _endGrace = 3;
    private double _rewardTail;
    private double _prefsAge;
    private static bool _reportedUnavailable;

    public Task ImpactReady => _impact.Task;
    public Task OpeningFinished => _opening.Task;
    public static void Prepare(Creature owner) => Ensure(owner, false, true);
    public static BadEndingVfx? Open(Creature owner) => Ensure(owner, true, false);

    public static void Restore(EmaBadEndingRewardPower power)
    {
        if (!power.IsMutable || power.Owner is not { } owner || !owner.Powers.Contains(power)) return;
        Ensure(owner, false, false);
    }

    private static BadEndingVfx? Find(Creature owner)
    {
        var room = NCombatRoom.Instance;
        if (room == null || !GodotObject.IsInstanceValid(room)) return null;
        foreach (var child in room.GetChildren())
            if (child is BadEndingVfx playback && !playback._disposed && ReferenceEquals(playback._owner, owner))
                return playback;
        return null;
    }

    private static BadEndingVfx? Ensure(Creature owner, bool opening, bool dormant)
    {
        if (TestMode.IsOn) return null;
        BadEndingVfx? playback = null;
        try
        {
            var room = NCombatRoom.Instance;
            if (room == null || !GodotObject.IsInstanceValid(room) || !room.IsInsideTree()) return null;
            playback = Find(owner);
            if (playback != null && !playback._finishing)
            {
                if (!dormant && (opening || playback._dormant)) playback.Activate(opening);
                return playback;
            }
            if (!dormant && !opening && (CombatManager.Instance.IsOverOrEnding ||
                owner.CombatState?.IsLiveCombat() != true || owner.GetPower<EmaBadEndingRewardPower>() == null)) return null;
            playback?.Dispose();
            // A missed preload must not turn the card press into a synchronous scene load.
            if (!PreloadManager.Cache.ContainsKey(ScenePath)) return null;
            var packed = PreloadManager.Cache.GetScene(ScenePath);
            playback = new BadEndingVfx { Name = "BadEndingPlayback", _owner = owner, _dormant = dormant };
            var stage = packed.Instantiate<Node2D>();
            playback._stage = stage;
            var layer = new CanvasLayer { Name = "BadEndingCanvas", Layer = 7 };
            for (Node? ancestor = owner.GetBackVfxContainer(); ancestor != null; ancestor = ancestor.GetParent())
            {
                if (ancestor is not CanvasLayer combatCanvas) continue;
                layer.Layer = Math.Max(layer.Layer, combatCanvas.Layer + 1);
                break;
            }
            playback.AddChild(layer);
            layer.AddChild(stage);
            if (!stage.HasMethod("setup") || !stage.HasMethod("prepare_idle") || !stage.HasMethod("set_field_active") ||
                !stage.HasMethod("notify_support") || !stage.HasMethod("finish_effect") ||
                !stage.HasSignal("impact_ready") || !stage.HasSignal("opening_finished") || !stage.HasSignal("finished"))
                throw new InvalidOperationException("Bad ending scene contract unavailable.");
            stage.Set("sfx_gain", SfxGain());
            stage.Connect("impact_ready", Callable.From(() => { playback._impact.TrySetResult(); }));
            stage.Connect("opening_finished", Callable.From(() => { playback.CompleteOpening(); }));
            stage.Connect("finished", Callable.From(playback.Dispose));
            stage.TreeExiting += playback.StageExiting;
            if (dormant) stage.Call("prepare_idle");
            else
            {
                stage.Call("setup", Anchor(owner), opening);
                if (!opening) playback.CompleteOpening();
            }
            room.AddChild(playback);
            if (!dormant) playback.AcquireField(opening);
            return playback;
        }
        catch (Exception ex) { Warn(ex); playback?.Dispose(); return null; }
    }

    private void Activate(bool opening, bool acquireField = true)
    {
        CompleteOpening();
        _impact = new TaskCompletionSource();
        _opening = new TaskCompletionSource();
        _dormant = false;
        _elapsed = 0;
        _endGrace = 3;
        _rewardTail = 0;
        _stage!.Call("setup", Anchor(_owner!), opening);
        if (acquireField) AcquireField(opening);
        else _stage.Call("set_field_active", false);
        if (!opening) CompleteOpening();
    }

    private void AcquireField(bool replace)
    {
        var active = CombatFieldVfxSlot.Claim(this, RetireField, restoreOnly: !replace);
        _stage!.Call("set_field_active", active);
    }

    private void RetireField()
    {
        if (_stage != null && GodotObject.IsInstanceValid(_stage)) _stage.Call("set_field_active", false);
    }

    public static void NotifyEnemy(Creature enemy, int amount)
    {
        if (TestMode.IsOn) return;
        try
        {
            var room = NCombatRoom.Instance;
            if (room == null || !GodotObject.IsInstanceValid(room)) return;
            // One notification per actual counter change; never called by damage previews.
            foreach (var child in room.GetChildren())
                if (child is BadEndingVfx playback && !playback._disposed && !playback._dormant)
                    playback.Notify(1, Anchor(enemy), amount);
        }
        catch (Exception ex) { Warn(ex); }
    }

    public static void NotifyDecision(Creature owner)
    {
        if (TestMode.IsOn) return;
        try { Ensure(owner, false, false)?.Notify(2, Anchor(owner), 1); }
        catch (Exception ex) { Warn(ex); }
    }

    public static void NotifyReward(Creature owner, CardModel card, int index)
    {
        if (TestMode.IsOn) return;
        try
        {
            // EndCombat has already changed IsInProgress here. Keep the current room's
            // stage alive briefly; never create a new persistent power after combat.
            var playback = Find(owner);
            if (playback == null || playback._finishing || playback._dormant) return;
            playback._rewardTail = 2.3;
            var origin = new Vector2(.48f + index * .12f, .48f);
            playback.Notify(3, origin, index + 1);
            playback.NotifyCachedCard(3, origin, card);
        }
        catch (Exception ex) { Warn(ex); }
    }

    public static void NotifyAutoplay(CardModel card)
    {
        if (TestMode.IsOn || card.Owner?.Creature is not { } owner) return;
        try
        {
            // Attached rewards can outlive the original power/combat. Show the short
            // seal in a dormant prepared stage, without opening or granting the aura.
            var playback = Find(owner) ?? Ensure(owner, false, true);
            if (playback == null || playback._finishing) return;
            if (playback._dormant) playback.Activate(false, acquireField: false);
            // A reward's short feedback never reclaims a field replaced by another card.
            if (owner.GetPower<EmaBadEndingRewardPower>() == null) playback.RetireField();
            playback._rewardTail = 2.3;
            playback.Notify(4, Anchor(owner) + new Vector2(.08f, -.08f), 1);
        }
        catch (Exception ex) { Warn(ex); }
    }

    private void Notify(int kind, Vector2 origin, int count)
    {
        if (_finishing || _disposed || _stage == null || !GodotObject.IsInstanceValid(_stage)) return;
        _stage.Call("notify_support", kind, origin, count);
    }

    private void NotifyCachedCard(int kind, Vector2 origin, CardModel card)
    {
        // Only request an existing cache entry. Never use card.Portrait here:
        // that property can synchronously ResourceLoader.Load a never-seen portrait.
        if (_stage == null || !_stage.HasMethod("notify_card")) return;
        var path = card.PortraitPath;
        if (!PreloadManager.Cache.ContainsKey(path)) return;
        var art = PreloadManager.Cache.GetTexture2D(path);
        _stage.Call("notify_card", kind, origin, art);
    }

    public static void Finish(Creature owner)
    {
        if (TestMode.IsOn) return;
        try
        {
            // Removal during the combat cleanup must not erase the reward response.
            if (!CombatManager.Instance.IsOverOrEnding) Find(owner)?.BeginFinish();
        }
        catch (Exception ex) { Warn(ex); }
    }

    public override void _Process(double delta)
    {
        if (_disposed || _dormant) return;
        try
        {
            if (_stage == null || !GodotObject.IsInstanceValid(_stage)) { Dispose(); return; }
            if (_finishing)
            {
                _finishAge += delta;
                if (_finishAge >= .95) Dispose();
                return;
            }
            _elapsed += delta;
            _rewardTail = Math.Max(0, _rewardTail - delta);
            _prefsAge += delta;
            if (_prefsAge >= .25) { _prefsAge = 0; _stage.Set("sfx_gain", SfxGain()); }
            if (!_opening.Task.IsCompleted)
            {
                // Both waits are released on room exit and this watchdog as well.
                if (_elapsed > 7.0) Dispose();
                return;
            }
            if (CombatManager.Instance.IsOverOrEnding || _owner?.CombatState?.IsLiveCombat() != true)
            {
                _endGrace -= delta;
                if (_endGrace <= 0 && _rewardTail <= 0) BeginFinish();
            }
            else if (_owner.GetPower<EmaBadEndingRewardPower>() == null && _rewardTail <= 0) BeginFinish();
        }
        catch (Exception ex) { Warn(ex); Dispose(); }
    }

    private void CompleteOpening() { _impact.TrySetResult(); _opening.TrySetResult(); }
    private void StageExiting() { _stage = null; Dispose(); }
    private void BeginFinish()
    {
        if (_finishing || _disposed) return;
        _finishing = true;
        CombatFieldVfxSlot.Release(this);
        CompleteOpening();
        if (_stage != null && GodotObject.IsInstanceValid(_stage)) _stage.Call("finish_effect");
        else Dispose();
    }

    private static Vector2 Anchor(Creature creature)
    {
        var marker = creature.GetCreatureNode()?.Visuals?.VfxSpawnPosition;
        if (marker != null && GodotObject.IsInstanceValid(marker) && marker.IsInsideTree())
        {
            var size = marker.GetViewportRect().Size;
            if (size.X > 0 && size.Y > 0) return marker.GetGlobalTransformWithCanvas().Origin / size;
        }
        return creature.IsPlayer ? new Vector2(.22f,.55f) : new Vector2(.75f,.55f);
    }

    private static float SfxGain()
    {
        var saves = SaveManager.Instance;
        if (saves.PrefsSave.MuteInBackground && !DisplayServer.WindowIsFocused()) return 0;
        return Mathf.Pow(Mathf.Clamp(saves.SettingsSave.VolumeSfx,0,1),2);
    }

    private static void Warn(Exception ex)
    {
        if (_reportedUnavailable) return;
        _reportedUnavailable = true;
        Log.Warn($"Bad ending VFX unavailable; card effects continue normally: {ex.Message}");
    }
    public override void _ExitTree() { CombatFieldVfxSlot.Release(this); _disposed = true; CompleteOpening(); }
    public new void Dispose()
    {
        if (_disposed) return;
        CombatFieldVfxSlot.Release(this);
        _disposed = true;
        CompleteOpening();
        if (GodotObject.IsInstanceValid(this) && !IsQueuedForDeletion())
        {
            if (IsInsideTree()) QueueFree(); else Free();
        }
        GC.SuppressFinalize(this);
    }
}
