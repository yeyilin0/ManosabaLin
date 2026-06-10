using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 凄惶球体（悲伤+恐惧）：回合结束回复护盾量四分之一的血量。
/// </summary>
[RegisterOrb]
public sealed class EmotionDesolateOrb : EmotionOrb<EmotionDesolate>
{
    protected override Color OrbColor => new(0.3f, 0.3f, 0.8f);

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext ctx)
    {
        var healAmount = Math.Floor(Owner.Creature.Block / 4m);
        if (healAmount > 0)
            await CreatureCmd.Heal(Owner.Creature, healAmount);
    }
}
