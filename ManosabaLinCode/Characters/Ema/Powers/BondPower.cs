using Godot;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class BondPower : ManosabaPowerTemplate, IPowerExtraIconAmountLabelsProvider
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private int _affinity;
    private int _estrangement;

    [SavedProperty]
    public int Affinity
    {
        get => _affinity;
        set
        {
            AssertMutable();
            var max = Estrangement >= 13 ? 12 : 13;
            _affinity = Mathf.Clamp(value, 0, max);
            InvokeDisplayAmountChanged();
        }
    }

    [SavedProperty]
    public int Estrangement
    {
        get => _estrangement;
        set
        {
            AssertMutable();
            var max = Affinity >= 13 ? 12 : 13;
            _estrangement = Mathf.Clamp(value, 0, max);
            InvokeDisplayAmountChanged();
        }
    }

    public override int DisplayAmount => Amount;

    public IReadOnlyList<ExtraIconAmountLabelSlot> GetPowerExtraIconAmountLabelSlots()
    {
        return new ExtraIconAmountLabelSlot[]
        {
            new()
            {
                Text = Affinity.ToString(),
                Corner = ExtraIconAmountLabelCorner.TopLeft,
                FontColor = new Color(1f, 0.6f, 0.8f),
                FontOutlineColor = new Color(0f, 0f, 0f),
            },
            new()
            {
                Text = Estrangement.ToString(),
                Corner = ExtraIconAmountLabelCorner.TopRight,
                FontColor = new Color(0.8f, 0.4f, 0.4f),
                FontOutlineColor = new Color(0f, 0f, 0f),
            }
        };
    }
}