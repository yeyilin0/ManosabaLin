using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 愤怒球体：本回合造成双倍伤害。
/// </summary>
[RegisterOrb]
public sealed class EmotionAngerOrb : EmotionOrb<EmotionAnger>
{
    protected override Color OrbColor => new(1f, 0.2f, 0.2f);

    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner.Creature) return 1m;
        if (target == null || target.Side == Owner.Creature.Side) return 1m;
        if (amount <= 0) return 1m;
        return 2m;
    }
}
