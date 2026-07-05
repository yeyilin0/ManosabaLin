using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class SacrificeTankPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private decimal _damageAccumulated;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        foreach (var target in CombatState.GetTeammatesOf(Owner))
        {
            if (target.IsAlive && target.IsPlayer && target != Owner)
            {
                await PowerCmd.Apply<GuardedPower>(
                    new ThrowingPlayerChoiceContext(), target, Amount, Owner, null);
            }
        }
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target == Owner && props.IsPoweredAttack())
            return 2m;
        return 1m;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || !props.IsPoweredAttack()) return;

        _damageAccumulated += result.TotalDamage;

        while (_damageAccumulated >= 20)
        {
            _damageAccumulated -= 20;

            await PowerCmd.Apply<EnergyNextTurnPower>(
                choiceContext, Owner, 1, Owner, null, false);

            foreach (var ally in CombatState.GetTeammatesOf(Owner)
                .Where(c => c.IsAlive && c.IsPlayer && c != Owner))
            {
                await PowerCmd.Apply<WithPower>(
                    choiceContext, ally, 10, Owner, null, false);
                await PowerCmd.Apply<DrawCardsNextTurnPower>(
                    choiceContext, ally, 1, Owner, null, false);
            }
        }
    }
}
