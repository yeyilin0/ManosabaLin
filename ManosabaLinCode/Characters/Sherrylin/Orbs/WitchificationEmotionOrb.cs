using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 魔女化球体：本回合造成三倍伤害。
/// </summary>
[RegisterOrb]
public sealed class WitchificationEmotionOrb : EmotionOrb<WitchificationEmotion>
{
    protected override Color OrbColor => new(0.8f, 0f, 0.8f);

    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner.Creature) return 1m;
        if (target == null || target.Side == Owner.Creature.Side) return 1m;
        if (amount <= 0) return 1m;
        return 3m;
    }
}
