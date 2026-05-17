using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Relics;
using ManosabaLin.Characters.Emalin.Enchantments;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class WitchTrialPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != Owner.Side) return;

        Flash();

        var badge = Owner.Player?.Relics.OfType<EmaTrialBadge>().FirstOrDefault();
        if (badge == null) return;

        foreach (var card in PileType.Hand.GetPile(Owner.Player).Cards)
        {
            if (card.Enchantment is Agreement)
                badge.IncrementCount(card.Enchantment);
            else if (card.Enchantment is Rebuttal)
                badge.IncrementCount(card.Enchantment);
            else if (card.Enchantment is Doubt)
                badge.IncrementCount(card.Enchantment);
        }
    }
}