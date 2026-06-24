using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class SwordlightPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner)
            return 1m;
        if (!props.IsPoweredAttack())
            return 1m;
        if (cardSource is not IAuroraSwordCard)
            return 1m;
        if (amount <= 0m)
            return 1m;

        var player = Owner.Player;
        if (player == null)
            return 1m;

        var multiplier = 1m;
        foreach (var _ in PileType.Discard.GetPile(player).Cards.OfType<Swordlight>())
            multiplier *= 2m;

        return multiplier;
    }
}
