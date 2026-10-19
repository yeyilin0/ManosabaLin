using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace ManosabaLin.Characters.Common.Vfx;

/// <summary>
/// One retained battlefield presentation per combat room, across all owners and
/// effect types. Claim only on a new field cast; notification/preload paths must
/// not claim. This manages visuals only, never powers, card state or action waits.
/// </summary>
public partial class CombatFieldVfxSlot : Node
{
    private const string SlotName = "ManosabaRetainedFieldSlot";
    private Node? _presenter;
    private Action? _onReplaced;
    private Action? _onPresenterExit;
    private bool _hasClaimed;

    /// <param name="restoreOnly">
    /// Restore into a fresh room only. Previously replaced fields never regain
    /// ownership just because a notification fires or the newer field ends.
    /// </param>
    public static bool Claim(Node presenter, Action onReplaced, bool restoreOnly = false)
    {
        if (TestMode.IsOn || !GodotObject.IsInstanceValid(presenter) ||
            !presenter.IsInsideTree() || presenter.IsQueuedForDeletion() ||
            FindRoom(presenter) is not { } room || !room.IsInsideTree()) return false;
        var slot = room.GetNodeOrNull<CombatFieldVfxSlot>(SlotName);
        if (slot == null)
        {
            slot = new CombatFieldVfxSlot { Name = SlotName };
            room.AddChild(slot);
        }
        if (ReferenceEquals(slot._presenter, presenter)) return true;
        if (restoreOnly && slot._hasClaimed) return false;

        var retirePrevious = slot._onReplaced;
        slot.DropCurrent();
        slot._hasClaimed = true;
        slot._presenter = presenter;
        slot._onReplaced = onReplaced;
        slot._onPresenterExit = () => slot.ReleaseCurrent(presenter);
        presenter.TreeExiting += slot._onPresenterExit;
        // Install the new owner first: a callback disposing the old one must not
        // clear the replacement, and re-entrant newer claims must still win.
        try { retirePrevious?.Invoke(); }
        catch (Exception ex) { Log.Warn($"Retained field retirement failed: {ex.Message}"); }
        return ReferenceEquals(slot._presenter, presenter);
    }

    public static void Release(Node presenter)
    {
        var room = FindRoom(presenter);
        room?.GetNodeOrNull<CombatFieldVfxSlot>(SlotName)?.ReleaseCurrent(presenter);
    }

    private static NCombatRoom? FindRoom(Node presenter)
    {
        if (!GodotObject.IsInstanceValid(presenter)) return null;
        for (Node? node = presenter.GetParent(); node != null; node = node.GetParent())
            if (node is NCombatRoom room) return room;
        return null;
    }

    private void ReleaseCurrent(Node presenter)
    {
        if (ReferenceEquals(_presenter, presenter)) DropCurrent();
    }

    private void DropCurrent()
    {
        if (_presenter != null && GodotObject.IsInstanceValid(_presenter) && _onPresenterExit != null)
            _presenter.TreeExiting -= _onPresenterExit;
        _presenter = null;
        _onReplaced = null;
        _onPresenterExit = null;
    }

    public override void _ExitTree() => DropCurrent();
}
