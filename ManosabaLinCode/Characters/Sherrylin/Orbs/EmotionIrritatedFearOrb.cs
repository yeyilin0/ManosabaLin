using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 恼惧球体（愤怒+恐惧）：回合结束给予自身13伤害并获得等量护盾。
/// </summary>
[RegisterOrb]
public sealed class EmotionIrritatedFearOrb : EmotionOrb<EmotionIrritatedFear>
{
    protected override Color OrbColor => new(0.9f, 0.2f, 0.5f);

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext ctx)
    {
        await CreatureCmd.Damage(ctx, Owner.Creature, 13m, ValueProp.Unpowered, Owner.Creature, null);
        await CreatureCmd.GainBlock(Owner.Creature, 13m, ValueProp.Unpowered, null);
    }
}
