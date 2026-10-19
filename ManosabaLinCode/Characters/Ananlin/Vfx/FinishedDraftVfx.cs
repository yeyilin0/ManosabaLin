using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Saves;

namespace ManosabaLin.Characters.Ananlin.Vfx;

public partial class FinishedDraftVfx : Node, IDisposable
{
    internal const string ScenePath = "res://ManosabaLin/scenes/Ananlin/finished_draft/finished_draft.tscn";
    internal const string CurtainScenePath = "res://ManosabaLin/scenes/Ananlin/finished_draft/curtain.tscn";
    public static IEnumerable<string> AssetPaths => [ScenePath, CurtainScenePath];

    private Creature? _owner;
    private Node2D? _stage;
    private Node2D? _backdrop;
    private TaskCompletionSource? _hit;
    private double _hitTime;
    private readonly TaskCompletionSource _opened = new();
    private readonly TaskCompletionSource _closed = new();
    private double _time;
    private double _prefsAge;
    private bool _dormant = true;
    private bool _disposed;
    private bool _ending;
    public Task Opened => _opened.Task;
    public Task Closed => _closed.Task;

    public static void Prepare(Creature player)
    {
        if (TestMode.IsOn) return;
        var room = NCombatRoom.Instance;
        if (room == null || !GodotObject.IsInstanceValid(room) || !room.IsInsideTree()) return;
        if (FindPrepared(room, player) == null) CreatePrepared(room, player);
    }

    public static FinishedDraftVfx? Play(Creature player, Creature? target)
    {
        if (TestMode.IsOn || player.CombatState?.IsLiveCombat() != true) return null;
        var room = NCombatRoom.Instance;
        if (room == null || !GodotObject.IsInstanceValid(room) || !room.IsInsideTree()) return null;
        var fx = FindPrepared(room, player) ?? CreatePrepared(room, player);
        if (fx == null) return null;
        try
        {
            fx.Activate(player, target, room);
            return fx;
        }
        catch (Exception ex)
        {
            Log.Warn($"Finished draft theater unavailable: {ex.Message}");
            fx.Dispose();
            return null;
        }
    }

    private static FinishedDraftVfx? FindPrepared(NCombatRoom room, Creature player)
    {
        foreach (var child in room.GetChildren())
            if (child is FinishedDraftVfx fx && fx._dormant && !fx._disposed &&
                !fx.IsQueuedForDeletion() && ReferenceEquals(fx._owner, player)) return fx;
        return null;
    }

    private static FinishedDraftVfx? CreatePrepared(NCombatRoom room, Creature player)
    {
        // Never turn a missing preload into blocking disk/resource work on card play.
        if (!PreloadManager.Cache.ContainsKey(ScenePath) ||
            !PreloadManager.Cache.ContainsKey(CurtainScenePath)) return null;
        var fx = new FinishedDraftVfx { Name = "FinishedDraftPlayback", _owner = player };
        try
        {
            var scene = PreloadManager.Cache.GetScene(ScenePath);
            var layer = scene.Instantiate<CanvasLayer>();
            fx.AddChild(layer);
            fx._stage = layer.GetNode<Node2D>("Stage");
            if (!fx._stage.HasMethod("prepare_idle") || !fx._stage.HasMethod("begin_show") ||
                !fx._stage.HasMethod("strike") || !fx._stage.HasMethod("end_show") ||
                !fx._stage.HasSignal("opened") || !fx._stage.HasSignal("closed") ||
                !fx._stage.HasSignal("impact"))
                throw new InvalidOperationException("Finished draft scene contract unavailable.");
            fx._stage.Set("sfx_gain", SfxGain());
            var curtainScene = PreloadManager.Cache.GetScene(CurtainScenePath);
            fx._backdrop = curtainScene.Instantiate<Node2D>();
            fx._backdrop.Hide();
            // Match EmaFormVfx: official creature back-VFX container, no manual ZIndex.
            var backContainer = player.GetBackVfxContainer()
                ?? throw new InvalidOperationException("Creature back-VFX container unavailable.");
            backContainer.AddChildSafely(fx._backdrop);
            // Foreground effects must be above the curtain's actual canvas, including
            // rooms that wrap combat visuals in their own CanvasLayer.
            for (Node? ancestor = backContainer; ancestor != null; ancestor = ancestor.GetParent())
            {
                if (ancestor is not CanvasLayer backCanvas) continue;
                layer.Layer = Math.Max(layer.Layer, backCanvas.Layer + 1);
                break;
            }
            fx._stage.Set("backdrop", fx._backdrop);
            // Build audio players, particles and draw caches before the actual card press.
            fx._stage.Call("prepare_idle");
            fx._stage.Connect("opened", Callable.From(() => fx._opened.TrySetResult()));
            fx._stage.Connect("closed", Callable.From(fx.Dispose));
            fx._stage.Connect("impact", Callable.From(() => fx._hit?.TrySetResult()));
            fx._stage.TreeExiting += fx.ReleaseWaitersAndBackdrop;
            fx.SetProcess(false);
            room.AddChild(fx);
            return fx;
        }
        catch (Exception ex) { Log.Warn($"Finished draft theater unavailable: {ex.Message}"); fx.Dispose(); return null; }
    }

    private void Activate(Creature player, Creature? target, NCombatRoom room)
    {
        var size = room.GetViewport().GetVisibleRect().Size;
        Vector2 Anchor(Creature? creature, Vector2 fallback)
        {
            var node = creature?.GetCreatureNode()?.Visuals?.VfxSpawnPosition;
            return node == null || size.X <= 0 || size.Y <= 0
                ? fallback : node.GetGlobalTransformWithCanvas().Origin / size;
        }
        _stage!.Set("player_anchor", Anchor(player, new Vector2(.22f, .55f)));
        _stage.Set("target_anchor", Anchor(target, new Vector2(.75f, .5f)));
        _stage.Set("sfx_gain", SfxGain());
        _dormant = false;
        _stage.Call("begin_show");
        SetProcess(true);
    }

    public Task Hit()
    {
        if (_disposed || _dormant || _ending || _stage == null || !GodotObject.IsInstanceValid(_stage))
            return Task.CompletedTask;
        _hit?.TrySetResult();
        _hit = new TaskCompletionSource();
        _hitTime = 0;
        _stage.Call("strike");
        return _hit.Task;
    }
    public void End()
    {
        if (_disposed || _ending) return;
        _ending = true; _time = 0;
        if (_stage != null && GodotObject.IsInstanceValid(_stage)) _stage.Call("end_show");
        else _closed.TrySetResult();
    }
    public override void _Process(double delta)
    {
        if (_disposed || _dormant) return;
        if (_stage == null || !GodotObject.IsInstanceValid(_stage) || !_stage.IsInsideTree())
        { Dispose(); return; }
        _prefsAge += delta;
        if (_prefsAge >= .15)
        {
            _prefsAge = 0;
            _stage.Set("sfx_gain", SfxGain());
        }
        _time += delta;
        _hitTime += delta;
        if (_hitTime > 1.6) _hit?.TrySetResult();
        if (_time > 3) _opened.TrySetResult();
        if ((_ending && _time > 3) || _time > 90) Dispose();
    }
    private static float SfxGain()
    {
        var saves = SaveManager.Instance;
        var muted = saves.PrefsSave.MuteInBackground && !DisplayServer.WindowIsFocused();
        return muted ? 0f : Mathf.Pow(Mathf.Clamp(saves.SettingsSave.VolumeSfx, 0f, 1f), 2f);
    }
    public override void _ExitTree()
    {
        _disposed = true;
        ReleaseWaitersAndBackdrop();
    }
    private void ReleaseWaitersAndBackdrop()
    {
        _opened.TrySetResult(); _closed.TrySetResult(); _hit?.TrySetResult();
        if (_backdrop == null || !GodotObject.IsInstanceValid(_backdrop) || _backdrop.IsQueuedForDeletion()) return;
        if (_backdrop.IsInsideTree()) _backdrop.QueueFree(); else _backdrop.Free();
        _backdrop = null;
    }
    public new void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ReleaseWaitersAndBackdrop();
        if (GodotObject.IsInstanceValid(this) && !IsQueuedForDeletion())
        { if (IsInsideTree()) QueueFree(); else Free(); }
        GC.SuppressFinalize(this);
    }
}
