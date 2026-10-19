using ManosabaLin.Characters.Common.Vfx;
using ManosabaLin.Characters.Sherrylin.Orbs;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace ManosabaLin.Characters.Sherrylin.Vfx;

/// <summary>
/// The retained border belongs to one actual friendship orb, not to the opening
/// movie or a turn timer. Removing/replacing that orb retires only its own field.
/// </summary>
public partial class FriendshipFieldVfx : Node, IDisposable
{
    internal const string ScenePath = "res://ManosabaLin/scenes/Sherrylin/friendship/friendship_field.tscn";
    private Creature? _owner;
    private EmotionFriendshipOrb? _orb;
    private Node2D? _stage;
    private bool _dormant = true;
    private bool _awaitingChannel;
    private bool _retiring;
    private bool _disposed;
    private double _retireAge;
    private static bool _reportedUnavailable;

    public static void Prepare(Creature owner) => Ensure(owner);

    public static FriendshipFieldVfx? Open(Creature owner)
    {
        if (TestMode.IsOn || owner.CombatState?.IsLiveCombat() != true) return null;
        var field = Ensure(owner);
        if (field == null) return null;
        try
        {
            // Another cast is a new field claim even if the previous friendship
            // orb still exists. A prepared invisible node never claims the slot.
            field._dormant = false;
            field._awaitingChannel = true;
            field._orb = null;
            field._stage!.Call("activate_field");
            if (!CombatFieldVfxSlot.Claim(field, field.BeginRetire)) field.BeginRetire();
            return field;
        }
        catch (Exception ex) { Warn(ex); field.Dispose(); return null; }
    }

    public static void BindChanneledOrb(EmotionFriendshipOrb orb)
    {
        if (TestMode.IsOn || !orb.IsMutable) return;
        try
        {
            var field = Find(orb.Owner.Creature);
            if (field is { _awaitingChannel: true, _orb: null }) field._orb = orb;
        }
        catch (Exception ex) { Warn(ex); }
    }

    public void CompleteChanneling()
    {
        _awaitingChannel = false;
        if (!_disposed && !_retiring && !OrbPresent()) BeginRetire();
    }

    private bool OrbPresent() => _orb is { HasBeenRemovedFromState: false } &&
        _owner?.Player?.PlayerCombatState?.OrbQueue.Orbs.Contains(_orb) == true;

    private static FriendshipFieldVfx? Find(Creature owner)
    {
        var room = NCombatRoom.Instance;
        if (room == null || !GodotObject.IsInstanceValid(room)) return null;
        foreach (var child in room.GetChildren())
            if (child is FriendshipFieldVfx field && !field._disposed && !field._retiring &&
                ReferenceEquals(field._owner, owner)) return field;
        return null;
    }

    private static FriendshipFieldVfx? Ensure(Creature owner)
    {
        if (TestMode.IsOn) return null;
        FriendshipFieldVfx? field = null;
        try
        {
            var room = NCombatRoom.Instance;
            if (room == null || !GodotObject.IsInstanceValid(room) || !room.IsInsideTree()) return null;
            field = Find(owner);
            if (field != null) return field;
            if (!PreloadManager.Cache.ContainsKey(ScenePath)) return null;
            var layer = PreloadManager.Cache.GetScene(ScenePath).Instantiate<CanvasLayer>();
            field = new FriendshipFieldVfx { Name = "FriendshipFieldPlayback", _owner = owner };
            field.AddChild(layer);
            var stage = layer.GetNode<Node2D>("Stage");
            field._stage = stage;
            if (!stage.HasMethod("prepare_idle") || !stage.HasMethod("activate_field") ||
                !stage.HasMethod("retire_field") || !stage.HasSignal("finished"))
                throw new InvalidOperationException("Friendship field script contract unavailable.");
            for (Node? ancestor = owner.GetBackVfxContainer(); ancestor != null; ancestor = ancestor.GetParent())
            {
                if (ancestor is not CanvasLayer combatCanvas) continue;
                layer.Layer = Math.Max(layer.Layer, combatCanvas.Layer + 1);
                break;
            }
            stage.Call("prepare_idle");
            stage.Connect("finished", Callable.From(field.Dispose));
            stage.TreeExiting += field.Dispose;
            room.AddChild(field);
            return field;
        }
        catch (Exception ex) { Warn(ex); field?.Dispose(); return null; }
    }

    private void BeginRetire()
    {
        if (_retiring || _disposed) return;
        _retiring = true;
        _dormant = false;
        // Release is identity-checked by the room slot: an expired old orb cannot
        // erase a newer true/bad-ending field, and retiring fields never reclaim.
        CombatFieldVfxSlot.Release(this);
        if (_stage != null && GodotObject.IsInstanceValid(_stage)) _stage.Call("retire_field");
        else Dispose();
    }

    public override void _Process(double delta)
    {
        if (_disposed || _dormant) return;
        if (_retiring)
        {
            _retireAge += delta;
            if (_retireAge >= .8) Dispose();
            return;
        }
        if (_stage == null || !GodotObject.IsInstanceValid(_stage)) { Dispose(); return; }
        // Queue membership, rather than only Evoke(), also catches slot removal,
        // queue clearing and early evocation. It changes when the visible orb exits.
        if (CombatManager.Instance.IsOverOrEnding || _owner?.CombatState?.IsLiveCombat() != true ||
            (!_awaitingChannel && !OrbPresent())) BeginRetire();
    }

    private static void Warn(Exception ex)
    {
        if (_reportedUnavailable) return;
        _reportedUnavailable = true;
        Log.Warn($"Friendship field unavailable; orb effects continue normally: {ex.Message}");
    }

    public override void _ExitTree() { CombatFieldVfxSlot.Release(this); _disposed = true; }

    public new void Dispose()
    {
        if (_disposed) return;
        CombatFieldVfxSlot.Release(this);
        _disposed = true;
        if (GodotObject.IsInstanceValid(this) && !IsQueuedForDeletion())
        {
            if (IsInsideTree()) QueueFree(); else Free();
        }
        GC.SuppressFinalize(this);
    }
}
