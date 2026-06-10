using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 雀跃球体（快乐+惊讶）：获得等于能量上限的能量但减少下回合能量。
/// </summary>
[RegisterOrb]
public sealed class EmotionElationOrb : EmotionOrb<EmotionElation>
{
    protected override Color OrbColor => new(1f, 0.9f, 0.3f);

    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext ctx)
    {
        var maxEnergy = Owner.Creature.Player?.MaxEnergy ?? 0;
        if (maxEnergy > 0)
        {
            await PlayerCmd.GainEnergy(maxEnergy, Owner);
            await PowerCmd.Apply<LoseEnergyPower>(
                ctx, Owner.Creature, (int)maxEnergy,
                Owner.Creature, null, false);
        }

        await OrbCmd.EvokeNext(ctx, Owner);
    }
}
