using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin.Enchantments;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

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

    foreach (var card in PileType.Hand.GetPile(Owner.Player).Cards)
    {
        var enchantment = card.Enchantment;
        if (enchantment is Rebuttal or Agreement or Doubt)
            enchantment.Amount += (int)Amount;
    }
}
}
