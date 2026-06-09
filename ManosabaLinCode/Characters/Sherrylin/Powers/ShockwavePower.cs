using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 冲击：攻击时额外造成等量伤害。
/// </summary>
[RegisterPower]
public sealed class ShockwavePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(
        Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner) return amount;
        if (target == null || target == Owner) return amount;

        return amount + Amount;
    }
}
