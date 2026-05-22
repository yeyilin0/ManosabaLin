using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class ShieldInterceptPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private decimal _totalDamageTaken;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target == Owner && result.TotalDamage > 0)
            _totalDamageTaken += result.TotalDamage;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        if (_totalDamageTaken <= 0) return;

        Flash();

        if (Amount > 0)
        {
            var allies = Owner.CombatState.Allies;
            if (allies != null)
            {
                foreach (var ally in allies)
                {
                    if (ally is { IsAlive: true } && ally != Owner)
                    {
                        await CreatureCmd.GainBlock(ally, _totalDamageTaken, ValueProp.Move, null);
                        break;
                    }
                }
            }
        }

        var bond = Owner.GetPower<BondPower>();
        if (bond != null && bond.Affinity > bond.Estrangement)
        {
            await CreatureCmd.GainBlock(Owner, _totalDamageTaken, ValueProp.Move, null);
        }

        _totalDamageTaken = 0;
        await PowerCmd.Remove(this);
    }
}