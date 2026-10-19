using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using ManosabaLin.Characters.Common.Vfx;

namespace ManosabaLin.Characters.Ema.Vfx;

/// <summary>
/// One scene per owner: a finite, awaited opening followed by a power-owned edge glow.
/// Scene callbacks, room exit and watchdogs all release the opening wait idempotently.
/// </summary>
public partial class TrueEndingVfx : Node, IDisposable
{
    internal const string ScenePath = "res://ManosabaLin/scenes/Emalin/true_ending/true_ending.tscn";
    public static IEnumerable<string> AssetPaths => [ScenePath];
    private TaskCompletionSource _openingFinished = new();
    private Node2D? _stage;
    private Creature? _player;
    private double _openingElapsed;
    private double _finishElapsed;
    private bool _finishing;
    private bool _disposed;
    private bool _dormant;
    private static bool _reportedUnavailable;

    public Task OpeningFinished => _openingFinished.Task;

    public static TrueEndingVfx? Open(EmaTrueEndingPower power) => Ensure(power, true);

    public static void Restore(EmaTrueEndingPower power) => Ensure(power, false);

    public static void Prepare(Creature player) => Ensure(null, false, player);

    public static void Notify(EmaTrueEndingPower power, int kind, CardModel card)
    {
        TrueEndingVfx? playback = null;
        try
        {
            // Global card hooks may also report a teammate's card.
            if (TestMode.IsOn || !power.IsMutable || power.Owner is not { } owner ||
                card.Owner?.Creature != owner) return;
            playback = Ensure(power, false);
            if (playback == null) return;
            // Only a local owner's cards have meaningful hand/play-pile screen positions.
            var origin = PlayerAnchor(power.Owner);
            if (card.Owner == power.Owner.Player && LocalContext.IsMe(card.Owner))
                origin = CardAnchor(card) ?? origin;
            playback._stage!.Call("notify_memory", kind, origin, power.AchievedCards.Count);
        }
        catch (Exception ex)
        {
            WarnUnavailable(ex);
            playback?.Dispose();
        }
    }

    public static void Finish(Creature owner)
    {
        if (TestMode.IsOn) return;
        try
        {
            foreach (var playback in Find(owner)) playback.BeginFinish();
        }
        catch (Exception ex) { WarnUnavailable(ex); }
    }

    private static IEnumerable<TrueEndingVfx> Find(Creature player)
    {
        var room = NCombatRoom.Instance;
        return room != null && GodotObject.IsInstanceValid(room)
            ? room.GetChildren().OfType<TrueEndingVfx>().Where(v => !v._disposed && ReferenceEquals(v._player, player)).ToArray()
            : [];
    }

    private static TrueEndingVfx? Ensure(EmaTrueEndingPower? power, bool replayOpening, Creature? preparingPlayer = null)
    {
        if (TestMode.IsOn || (preparingPlayer == null && power?.IsMutable != true)) return null;
        TrueEndingVfx? playback = null;
        try
        {
            // Apply can return an unattached mutable power if its amount was reduced to zero.
            var player = preparingPlayer ?? power?.Owner;
            if (player == null || (preparingPlayer == null &&
                (player.CombatState?.IsLiveCombat() != true || CombatManager.Instance.IsOverOrEnding ||
                 !player.Powers.Contains(power!)))) return null;
            var room = NCombatRoom.Instance;
            if (room == null || !GodotObject.IsInstanceValid(room) || !room.IsInsideTree()) return null;
            // Keeping ownership in the scene tree avoids static references across runs.
            playback = Find(player).FirstOrDefault();
            if (playback != null && !playback._finishing)
            {
                if (preparingPlayer == null && (replayOpening || playback._dormant))
                    playback.Activate(replayOpening, power!.AchievedCards.Count);
                return playback;
            }
            playback?.Dispose();
            playback = new TrueEndingVfx { Name = "TrueEndingPlayback", _player = player, _dormant = preparingPlayer != null };
            var packed = PreloadManager.Cache.GetScene(ScenePath)
                ?? throw new InvalidOperationException("True ending scene missing.");
            var stage = packed.Instantiate<Node2D>();
            playback._stage = stage;
            var layer = new CanvasLayer { Name = "TrueEndingCanvas", Layer = 7 };
            for (Node? ancestor = player.GetBackVfxContainer(); ancestor != null; ancestor = ancestor.GetParent())
            {
                if (ancestor is not CanvasLayer combatCanvas) continue;
                layer.Layer = Math.Max(layer.Layer, combatCanvas.Layer + 1);
                break;
            }
            playback.AddChild(layer);
            layer.AddChild(stage);
            if (!stage.HasMethod("setup") || !stage.HasMethod("notify_memory") || !stage.HasMethod("set_field_active") ||
                !stage.HasMethod("prepare_idle") || !stage.HasMethod("finish_effect") ||
                !stage.HasSignal("opening_finished") || !stage.HasSignal("finished"))
                throw new InvalidOperationException("True ending script contract unavailable.");
            stage.Set("sfx_gain", SfxGain());
            stage.Connect("opening_finished", Callable.From(playback.CompleteOpening));
            stage.Connect("finished", Callable.From(playback.Dispose));
            stage.TreeExiting += playback.OnStageExiting;
            if (playback._dormant) stage.Call("prepare_idle");
            else stage.Call("setup", PlayerAnchor(player), replayOpening, power!.AchievedCards.Count);
            if (!replayOpening) playback.CompleteOpening();
            room.AddChild(playback);
            if (!playback._dormant) playback.AcquireField(replayOpening);
            return playback;
        }
        catch (Exception ex)
        {
            WarnUnavailable(ex);
            playback?.Dispose();
            return null;
        }
    }

    private void Activate(bool replayOpening, int progress)
    {
        if (_stage == null || !GodotObject.IsInstanceValid(_stage))
            throw new InvalidOperationException("True ending stage was removed.");
        _openingFinished.TrySetResult();
        _openingFinished = new TaskCompletionSource();
        _openingElapsed = 0;
        _dormant = false;
        _stage.Call("setup", PlayerAnchor(_player!), replayOpening, progress);
        AcquireField(replayOpening);
        if (!replayOpening) CompleteOpening();
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

    private void CompleteOpening() => _openingFinished.TrySetResult();

    private void OnStageExiting()
    {
        _stage = null;
        Dispose();
    }

    private void BeginFinish()
    {
        if (_finishing || _disposed) return;
        _finishing = true;
        CombatFieldVfxSlot.Release(this);
        _finishElapsed = 0;
        CompleteOpening();
        try
        {
            if (_stage != null && GodotObject.IsInstanceValid(_stage) && _stage.HasMethod("finish_effect"))
                _stage.Call("finish_effect");
            else Dispose();
        }
        catch (Exception ex) { WarnUnavailable(ex); Dispose(); }
    }

    public override void _Process(double delta)
    {
        if (_disposed) return;
        try
        {
            if (_stage == null || !GodotObject.IsInstanceValid(_stage)) { Dispose(); return; }
            // The prewarmed scene waits silently in its current room until the power is played.
            if (_dormant) return;
            if (_finishing)
            {
                _finishElapsed += delta;
                if (_finishElapsed >= .9) Dispose();
                return;
            }
            if (CombatManager.Instance.IsOverOrEnding || _player?.CombatState?.IsLiveCombat() != true ||
                _player.GetPower<EmaTrueEndingPower>() == null)
            {
                BeginFinish();
                return;
            }
            _stage.Set("sfx_gain", SfxGain());
            if (!_openingFinished.Task.IsCompleted)
            {
                _openingElapsed += delta;
                // A broken/missing GDScript signal must never hold the card action queue.
                if (_openingElapsed >= 6.5) Dispose();
            }
        }
        catch (Exception ex) { WarnUnavailable(ex); Dispose(); }
    }

    private static Vector2 PlayerAnchor(Creature player)
    {
        if (TestMode.IsOn) return new Vector2(.22f, .55f);
        try
        {
            var marker = player.GetCreatureNode()?.Visuals?.VfxSpawnPosition;
            if (marker != null && GodotObject.IsInstanceValid(marker) && marker.IsInsideTree())
            {
                var size = marker.GetViewportRect().Size;
                if (size.X > 0 && size.Y > 0) return marker.GetGlobalTransformWithCanvas().Origin / size;
            }
        }
        catch (Exception ex) { WarnUnavailable(ex); }
        return new Vector2(.22f, .55f);
    }

    private static Vector2? CardAnchor(CardModel card)
    {
        var hand = ControlAnchor(NPlayerHand.Instance?.GetCardHolder(card)?.CardNode);
        return hand ?? ControlAnchor(NCombatRoom.Instance?.Ui?.GetCardFromPlayContainer(card));
    }

    private static Vector2? ControlAnchor(Control? control)
    {
        if (control == null || !GodotObject.IsInstanceValid(control) || !control.IsInsideTree() || !control.IsVisibleInTree())
            return null;
        var size = control.GetViewportRect().Size;
        return size.X > 0 && size.Y > 0
            ? (control.GetGlobalTransformWithCanvas() * (control.Size * .5f)) / size
            : null;
    }

    private static float SfxGain()
    {
        var saves = SaveManager.Instance;
        if (saves.PrefsSave.MuteInBackground && !DisplayServer.WindowIsFocused()) return 0f;
        return Mathf.Pow(Mathf.Clamp(saves.SettingsSave.VolumeSfx, 0f, 1f), 2f);
    }

    private static void WarnUnavailable(Exception ex)
    {
        if (_reportedUnavailable) return;
        _reportedUnavailable = true;
        Log.Warn($"True ending VFX unavailable; card effects continue normally: {ex.Message}");
    }

    public override void _ExitTree()
    {
        CombatFieldVfxSlot.Release(this);
        _disposed = true;
        CompleteOpening();
    }

    public new void Dispose()
    {
        if (_disposed) return;
        CombatFieldVfxSlot.Release(this);
        _disposed = true;
        CompleteOpening();
        if (GodotObject.IsInstanceValid(this) && !IsQueuedForDeletion())
        {
            if (IsInsideTree()) QueueFree();
            else Free();
        }
        GC.SuppressFinalize(this);
    }
}
