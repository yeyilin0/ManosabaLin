using MegaCrit.Sts2.Core.Commands.Builders;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class BattlemarkBondPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (Amount <= 0)
            return;
        if (!IsTriggeringAttack(command))
            return;

        Flash();
        await PowerCmd.Apply<MarkPower>(
            choiceContext,
            Owner,
            Amount,
            Owner,
            null,
            false);
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
}
