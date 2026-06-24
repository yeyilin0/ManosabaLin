using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class AuroraChainPower : ManosabaPowerTemplate
{
    public const decimal DamagePerStack = 1m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner)
            return 0m;
        if (!props.IsPoweredAttack())
            return 0m;
        if (cardSource is not ISwordGraveCard)
            return 0m;

        return Amount * DamagePerStack;
    }
}
