using Godot;
using ManosabaLin.Characters.Ananlin.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace ManosabaLin.Characters.Ananlin.Nodes;

public partial class AnanlinPeaceProgressCounter : Control
{
    private const int SlotCount = AnanlinPeaceOfMindPower.MaxStacks;

    private static readonly Color EmptyColor = new("262636");
    private static readonly Color PeaceColor = new("6666cc");
    private static readonly Color FadingPeaceColor = new("9999cc");
    private static readonly Color IsolatedColor = new("000000");

    private Player? _player;
    private ColorRect[] _slots = [];
    private int _lastAmount = int.MinValue;
    private int _lastTurnsAtMax = int.MinValue;
    private bool _lastIsolated;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        _slots = GetChildren().OfType<ColorRect>().ToArray();
        foreach (var slot in _slots)
            slot.MouseFilter = MouseFilterEnum.Ignore;

        Connect(SignalName.MouseEntered, Callable.From(OnHovered));
        Connect(SignalName.MouseExited, Callable.From(OnUnhovered));
        Refresh(force: true);
    }

    public override void _ExitTree()
    {
        NHoverTipSet.Remove(this);
    }

    public void SetContext(Player player)
    {
        _player = player;
        Refresh(force: true);
    }

    public override void _Process(double delta)
    {
        Refresh();
    }

    private void Refresh(bool force = false)
    {
        var creature = _player?.Creature;
        var peace = creature?.GetPower<AnanlinPeaceOfMindPower>();
        var isolated = creature?.GetPower<AnanlinIsolatedPower>() != null;
        var amount = Math.Clamp(peace?.Amount ?? 0, 0, SlotCount);
        var turnsAtMax = peace?.TurnsEndedWithPeace ?? 0;

        if (!force
            && amount == _lastAmount
            && turnsAtMax == _lastTurnsAtMax
            && isolated == _lastIsolated)
            return;

        _lastAmount = amount;
        _lastTurnsAtMax = turnsAtMax;
        _lastIsolated = isolated;

        for (var i = 0; i < _slots.Length; i++)
            _slots[i].Color = ResolveColor(i, amount, turnsAtMax, isolated);
    }

    private static Color ResolveColor(int slotIndex, int amount, int turnsAtMax, bool isolated)
    {
        if (isolated)
            return IsolatedColor;

        if (slotIndex >= amount)
            return EmptyColor;

        return amount >= SlotCount && turnsAtMax > 0
            ? FadingPeaceColor
            : PeaceColor;
    }

    private void OnHovered()
    {
        NHoverTipSet.CreateAndShow(
            this,
            HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>(_lastAmount),
            HoverTipAlignment.Right);
    }

    private void OnUnhovered()
    {
        NHoverTipSet.Remove(this);
    }
}
