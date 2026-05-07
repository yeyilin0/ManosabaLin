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
public class HnmPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override Decimal ModifyDamageMultiplicative(
        Creature? target,
        Decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return 1m;
        if (!props.IsPoweredAttack()) return 1m;

        return 0.5m;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;
        await PowerCmd.Remove(this);
    }
}