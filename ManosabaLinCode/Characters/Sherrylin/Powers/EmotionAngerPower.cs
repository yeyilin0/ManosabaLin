using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 愤怒能力：本回合造成双倍伤害。
/// </summary>
[RegisterPower]
public sealed class EmotionAngerPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner) return 1m;
        if (target == null || target.Side == Owner.Side) return 1m;
        if (amount <= 0) return 1m;
        return 2m;
    }
}
