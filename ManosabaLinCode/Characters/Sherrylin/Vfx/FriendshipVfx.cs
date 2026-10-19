using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace ManosabaLin.Characters.Sherrylin.Vfx;

/// <summary>
/// Scene-owned friendship opening and short, nonblocking feedback for the orb.
/// UI destinations are resolved from the local owner's actual cards/energy display.
/// </summary>
public partial class FriendshipVfx : Node, IDisposable
{
    internal const string ScenePath = "res://ManosabaLin/scenes/Sherrylin/friendship/friendship.tscn";
    public static IEnumerable<string> AssetPaths => [ScenePath, FriendshipFieldVfx.ScenePath];
    private readonly TaskCompletionSource _finished = new();
    private Node2D? _stage;
    private Creature? _player;
    private double _elapsed;
    private double _watchdog;
    private bool _disposed;
    private bool _dormant;
    private double _prefsAge;
    private static bool _reportedUnavailable;

    public Task Finished => _finished.Task;

    public static FriendshipVfx? Open(Creature player) => Play(player, new Vector2(.5f, .47f), 0);

    public static void Prepare(Creature player)
    {
        Play(player, new Vector2(.5f, .47f), 0, dormant: true);
        FriendshipFieldVfx.Prepare(player);
    }

    public static void CardsDrawn(Player owner, IEnumerable<CardModel> cards)
    {
        if (TestMode.IsOn || !LocalContext.IsMe(owner)) return;
        foreach (var card in cards)
        {
            // Prevented draws and cards moved out of hand by draw hooks have no target.
            if (card.Owner != owner || card.Pile?.Type != PileType.Hand) continue;
            if (CardAnchor(card) is { } anchor) Play(owner.Creature, anchor, 1);
        }
    }

    public static void EnergyGained(Player owner)
    {
        if (TestMode.IsOn || !LocalContext.IsMe(owner)) return;
        try
        {
            var container = NCombatRoom.Instance?.Ui?.EnergyCounterContainer;
            var counter = container?.GetChildren().OfType<NEnergyCounter>().FirstOrDefault();
            if (ControlAnchor(counter ?? container) is { } anchor) Play(owner.Creature, anchor, 2);
        }
        catch (Exception ex) { WarnUnavailable(ex); }
    }

    public static void StrengthGained(Player owner) => Play(owner.Creature, PlayerAnchor(owner.Creature), 3);

    public static void CardExhausting(Player owner, CardModel card)
    {
        if (TestMode.IsOn || !LocalContext.IsMe(owner) || card.Owner != owner) return;
        // Capture before Exhaust removes the holder from the hand.
        if (CardAnchor(card) is { } anchor) Play(owner.Creature, anchor, 4);
    }

    private static FriendshipVfx? Play(Creature player, Vector2 target, int mode, bool dormant = false)
    {
        if (TestMode.IsOn || (!dormant && player.CombatState?.IsLiveCombat() != true)) return null;
        var room = NCombatRoom.Instance;
        if (room == null || !GodotObject.IsInstanceValid(room) || !room.IsInsideTree()) return null;
        if (mode == 0)
        {
            foreach (var child in room.GetChildren())
            {
                if (child is not FriendshipVfx prepared || prepared._disposed || !prepared._dormant ||
                    !ReferenceEquals(prepared._player, player)) continue;
                if (!dormant)
                {
                    prepared._dormant = false;
                    prepared._stage!.Call("setup", PlayerAnchor(player), target, mode);
                    prepared._stage.Set("sfx_gain", SfxGain());
                    prepared._stage.Set("audio_enabled", true);
                    prepared._stage.Set("autoplay", true);
                }
                return prepared;
            }
        }
        // The run cache owns the scene and its dependencies. A missing preload must
        // not move synchronous disk decoding onto the card-play frame.
        if (!PreloadManager.Cache.ContainsKey(ScenePath)) return null;
        var playback = new FriendshipVfx
        {
            Name = "FriendshipPlayback",
            _player = player,
            _watchdog = mode == 0 ? 5.2 : 1.8,
            _dormant = dormant
        };
        try
        {
            var packed = PreloadManager.Cache.GetScene(ScenePath);
            var stage = packed.Instantiate<Node2D>();
            playback._stage = stage;
            var layer = new CanvasLayer { Name = "FriendshipCanvas", Layer = 7 };
            // Keep canvas effects above combat visuals without moving or hiding them.
            for (Node? ancestor = player.GetBackVfxContainer(); ancestor != null; ancestor = ancestor.GetParent())
            {
                if (ancestor is not CanvasLayer combatCanvas) continue;
                layer.Layer = Math.Max(layer.Layer, combatCanvas.Layer + 1);
                break;
            }
            playback.AddChild(layer);
            layer.AddChild(stage);
            if (!stage.HasMethod("setup") || !stage.HasSignal("finished"))
                throw new InvalidOperationException("Friendship script failed to load.");
            stage.Set("sfx_gain", SfxGain());
            stage.Set("autoplay", !dormant);
            stage.Set("audio_enabled", !dormant);
            stage.Call("setup", PlayerAnchor(player), target, mode);
            stage.Connect("finished", Callable.From(playback.Dispose));
            stage.TreeExiting += playback.Dispose;
            room.AddChild(playback);
            return playback;
        }
        catch (Exception ex)
        {
            WarnUnavailable(ex);
            playback.Dispose();
            return null;
        }
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
        try { return ControlAnchor(NPlayerHand.Instance?.GetCardHolder(card)?.CardNode); }
        catch (Exception ex) { WarnUnavailable(ex); return null; }
    }

    private static Vector2? ControlAnchor(Control? control)
    {
        if (control == null || !GodotObject.IsInstanceValid(control) || !control.IsInsideTree() || !control.IsVisibleInTree())
            return null;
        var size = control.GetViewportRect().Size;
        if (size.X <= 0 || size.Y <= 0) return null;
        return (control.GetGlobalTransformWithCanvas() * (control.Size * .5f)) / size;
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
        Log.Warn($"Friendship VFX unavailable; card effects continue normally: {ex.Message}");
    }

    public override void _Process(double delta)
    {
        if (_disposed || _dormant) return;
        _elapsed += delta;
        if (_elapsed > _watchdog || CombatManager.Instance.IsOverOrEnding || _player?.CombatState?.IsLiveCombat() != true)
        {
            Dispose();
            return;
        }
        if (_stage != null && GodotObject.IsInstanceValid(_stage))
        {
            _prefsAge += delta;
            if (_prefsAge >= .25) { _prefsAge = 0; _stage.Set("sfx_gain", SfxGain()); }
        }
        else Dispose();
    }

    public override void _ExitTree() => _finished.TrySetResult();

    public new void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _finished.TrySetResult();
        if (GodotObject.IsInstanceValid(this) && !IsQueuedForDeletion())
        {
            if (IsInsideTree()) QueueFree();
            else Free();
        }
        GC.SuppressFinalize(this);
    }
}
