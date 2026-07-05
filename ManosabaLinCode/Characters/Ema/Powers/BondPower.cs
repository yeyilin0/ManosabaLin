using Godot;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Helpers;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class BondPower : ManosabaPowerTemplate, IPowerExtraIconAmountLabelsProvider
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    [SavedProperty]
    public int Affinity
    {
        get;
        set
        {
            AssertMutable();
            var max = Estrangement >= 13 ? 12 : 13;
            var clamped = Mathf.Clamp(value, 0, max);
            var delta = clamped - field;
            field = clamped;
            InvokeDisplayAmountChanged();

            if (delta > 0)
            {
                ReduceBondVerdicts(delta);

                var yalisabond = Owner?.GetPower<Yalisabond>();
                if (yalisabond != null)
                {
                    TaskHelper.RunSafely(yalisabond.ApplyBondDeltaAsync(delta, 0));
                }

              
            }
        }
    }

    [SavedProperty]
    public int Estrangement
    {
        get;
        set
        {
            AssertMutable();
            var max = Affinity >= 13 ? 12 : 13;
            var clamped = Mathf.Clamp(value, 0, max);
            var delta = clamped - field;
            field = clamped;
            InvokeDisplayAmountChanged();

            if (delta > 0)
            {
                ReduceBondVerdicts(delta);

                var yalisabond = Owner?.GetPower<Yalisabond>();
                if (yalisabond != null)
                {
                    TaskHelper.RunSafely(yalisabond.ApplyBondDeltaAsync(0, delta));
                }
            }
        }
    }

    public override int DisplayAmount => Amount;

    private void ReduceBondVerdicts(int amount)
    {
        var player = Owner?.Player;
        if (player == null || amount <= 0) return;

        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
        {
            foreach (var card in pileType.GetPile(player).Cards.OfType<BondVerdict>())
                card.ReduceForBondChange(amount);
        }
    }

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
