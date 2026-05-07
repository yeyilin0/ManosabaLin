using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class XlmPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Decimal ModifyDamageAdditive(
        Creature? target,
        Decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner) return 0m;
        if (target == null || target.Side == Owner.Side) return 0m;
        if (amount <= 0) return 0m;

        return Amount;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;
        await PowerCmd.Remove(this);
    }
}