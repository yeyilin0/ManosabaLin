using MegaCrit.Sts2.Core.Commands.Builders;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinDeletedStressPower : ManosabaPowerTemplate
{
    private sealed class Data
    {
        internal AttackCommand? CommandToModify { get; set; }
    }

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.None;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker != Owner || !command.DamageProps.IsPoweredAttack())
            return Task.CompletedTask;

        var data = GetInternalData<Data>();
        data.CommandToModify ??= command;
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer != Owner || !props.IsPoweredAttack())
            return 1m;

        return 0.5m;
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        var data = GetInternalData<Data>();
        if (command != data.CommandToModify) return;

        data.CommandToModify = null;
        await PowerCmd.ModifyAmount(choiceContext, this, -1, null, null);
    }
}
