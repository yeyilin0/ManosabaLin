using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace ManosabaLin.Characters.Yalisalin.Relics;

public partial class YalisalinFireColorCounter : Control
{
    private const float SlotSize = 22f;
    private const float SlotGap = 4f;
    private const float LeftPadding = 10f;
    private const int Columns = 1;
    private const int Rows = YalisalinsHairpin.MaxSegments;

    private static readonly Vector2 CounterSize = new(
        SlotSize * Columns + SlotGap * (Columns - 1),
        SlotSize * Rows + SlotGap * (Rows - 1));

    private static readonly Color EmptyColor = new("2b2021");

    private Player? _viewer;
    private Creature? _target;
    private ColorRect[] _slots = [];
    private YalisalinFireColor?[] _slotColors = [];
    private string _lastSignature = string.Empty;
    private int _hoveredSlotIndex = -1;

    public override void _Ready()
    {
        Size = CounterSize;
        CustomMinimumSize = CounterSize;
        MouseFilter = MouseFilterEnum.Pass;
        ZIndex = 20;

        BuildSlots();
        Refresh(force: true);
    }

    public override void _ExitTree()
    {
        NHoverTipSet.Remove(this);
        foreach (var slot in _slots)
            NHoverTipSet.Remove(slot);
    }

    public void SetContext(Player viewer, Creature target)
    {
        _viewer = viewer;
        _target = target;
        Refresh(force: true);
    }

    public override void _Process(double delta)
    {
        if (!CanShowInCurrentContext())
        {
            HideCounter();
            return;
        }

        RefreshPosition();
        Refresh();
    }

    private void BuildSlots()
    {
        if (_slots.Length > 0)
            return;

        _slots = Enumerable.Range(0, YalisalinsHairpin.MaxSegments)
            .Select(CreateSlot)
            .ToArray();

        _slotColors = new YalisalinFireColor?[YalisalinsHairpin.MaxSegments];

        for (var i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i];
            var index = i;
            slot.Connect(SignalName.MouseEntered, Callable.From(() => OnSlotHovered(index)));
            slot.Connect(SignalName.MouseExited, Callable.From(() => OnSlotUnhovered(index)));
            AddChild(slot);
        }
    }

    private static ColorRect CreateSlot(int index)
    {
        return new ColorRect
        {
            Name = $"FireColorSlot{index + 1}",
            Color = EmptyColor,
            Position = new Vector2(
                (index % Columns) * (SlotSize + SlotGap),
                (index / Columns) * (SlotSize + SlotGap)),
            Size = new Vector2(SlotSize, SlotSize),
            MouseFilter = MouseFilterEnum.Stop
        };
    }

    private void RefreshPosition()
    {
        if (_target?.GetCreatureNode() is not { Hitbox: { } hitbox })
            return;

        GlobalPosition = new Vector2(
            hitbox.GlobalPosition.X - CounterSize.X - LeftPadding,
            hitbox.GlobalPosition.Y + Math.Max(0f, (hitbox.Size.Y - CounterSize.Y) * 0.5f));
    }

    private bool CanShowInCurrentContext()
    {
        if (_viewer == null || _target == null || !_target.IsAlive)
            return false;

        return IsCombatScreenActive()
               && _target.GetCreatureNode() is { } creatureNode
               && creatureNode.IsVisibleInTree();
    }

    private void HideCounter()
    {
        Visible = false;
        _lastSignature = string.Empty;

        if (_hoveredSlotIndex >= 0 && _hoveredSlotIndex < _slots.Length)
            NHoverTipSet.Remove(_slots[_hoveredSlotIndex]);
    }

    private static bool IsCombatScreenActive()
    {
        try
        {
            return ActiveScreenContext.Instance.GetCurrentScreen() is NCombatRoom;
        }
        catch
        {
            return false;
        }
    }

    private void Refresh(bool force = false)
    {
        var segments = GetVisibleSegments();
        var signature = string.Join(';', segments.Select(segment => $"{(int)segment.Color}:{segment.Order}"));

        if (!force && signature == _lastSignature)
            return;

        _lastSignature = signature;
        Visible = segments.Count > 0;

        for (var i = 0; i < _slots.Length; i++)
        {
            _slotColors[i] = i < segments.Count ? segments[i].Color : null;
            _slots[i].Color = i < segments.Count ? segments[i].DisplayColor : EmptyColor;
        }

        if (_hoveredSlotIndex >= 0)
            ShowSlotHoverTip(_hoveredSlotIndex);
    }

    private IReadOnlyList<YalisalinFireColorSegment> GetVisibleSegments()
    {
        if (_viewer == null || _target == null || !_target.IsAlive)
            return [];

        return YalisalinFireColorSystem
            .GetFireColorSegments(_viewer, _target)
            .OrderBy(segment => segment.Order)
            .Take(YalisalinsHairpin.MaxSegments)
            .ToArray();
    }

    private void OnSlotHovered(int index)
    {
        _hoveredSlotIndex = index;
        ShowSlotHoverTip(index);
    }

    private void OnSlotUnhovered(int index)
    {
        NHoverTipSet.Remove(_slots[index]);
        if (_hoveredSlotIndex == index)
            _hoveredSlotIndex = -1;
    }

    private void ShowSlotHoverTip(int index)
    {
        if (index < 0 || index >= _slots.Length)
            return;

        var slot = _slots[index];
        NHoverTipSet.Remove(slot);

        var color = _slotColors.ElementAtOrDefault(index);
        if (color == null)
            return;

        NHoverTipSet.CreateAndShow(
            slot,
            CreateColorHoverTip(color.Value),
            HoverTipAlignment.Right);
    }

    private HoverTip CreateColorHoverTip(YalisalinFireColor color)
    {
        var suffix = color switch
        {
            YalisalinFireColor.LightOrange => "lightOrange",
            YalisalinFireColor.BrightYellow => "brightYellow",
            YalisalinFireColor.Red => "red",
            YalisalinFireColor.BlackRed => "blackRed",
            _ => "unknown"
        };

        var description = new LocString("relics", $"{YalisalinsHairpin.LocalizationEntry}.fireColor.{suffix}.description");
        description.Add("Damage", GetCurrentRedConsumeDamage());

        return new HoverTip(
            new LocString("relics", $"{YalisalinsHairpin.LocalizationEntry}.fireColor.{suffix}.title"),
            description);
    }

    private int GetCurrentRedConsumeDamage()
    {
        if (_viewer != null && YalisalinFireColorSystem.TryGetHairpin(_viewer, out var hairpin))
            return hairpin.RedConsumeDamage;

        return 3;
    }
}
