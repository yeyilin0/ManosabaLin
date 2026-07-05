using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class YlsmPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (Owner.IsDead) return;

        await CreatureCmd.Damage(choiceContext, Owner, 1m, ValueProp.Unblockable | ValueProp.Unpowered, null, null);

        await PowerCmd.Remove(this);
    }
}