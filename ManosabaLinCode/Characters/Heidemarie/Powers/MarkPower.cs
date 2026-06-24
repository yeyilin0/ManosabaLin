using MegaCrit.Sts2.Core.Commands.Builders;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class MarkPower : ManosabaPowerTemplate
{
    public const decimal Damage = 1m;
    public const decimal LayerCost = 1m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (Amount < LayerCost)
            return;
        if (!IsTriggeringAttack(command))
            return;

        var target = GetMarkDamageTarget(command);
        if (target == null)
            return;

        Flash();
        await PowerCmd.ModifyAmount(choiceContext, this, -LayerCost, Owner, null);
        await CreatureCmd.Damage(
            choiceContext,
            target,
            Damage,
            ValueProp.Move | ValueProp.Unpowered,
            Owner,
            null);
    }

    private bool IsTriggeringAttack(AttackCommand command)
    {
        if (!command.DamageProps.IsPoweredAttack())
            return false;

        var attacker = command.Attacker;
        if (attacker == null || !attacker.IsPlayer)
            return false;
        if (command.TargetSide == attacker.Side)
            return false;

        var combatState = Owner.CombatState;
        if (combatState == null)
            return false;

        if (combatState.Players.Count <= 1)
            return attacker == Owner;

        return attacker != Owner && attacker.Side == Owner.Side;
    }

    private Creature? GetMarkDamageTarget(AttackCommand command)
    {
        return command.Results
            .SelectMany(static r => r)
            .Select(static r => r.Receiver)
            .FirstOrDefault(receiver => receiver.IsAlive && receiver.Side != Owner.Side);
    }
}
