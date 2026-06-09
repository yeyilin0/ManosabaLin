using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Threading.Tasks;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 恐惧球体：回合结束获得当前一半格挡。
/// </summary>
[RegisterOrb]
public sealed class EmotionFearOrb : EmotionOrb<EmotionFear>
{
    protected override Color OrbColor => new(0.5f, 0.2f, 0.7f);

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext ctx)
    {
        var halfBlock = Math.Floor(Owner.Creature.Block / 2m);
        if (halfBlock > 0)
            await CreatureCmd.GainBlock(Owner.Creature, halfBlock, ValueProp.Unpowered, null);
    }
}
