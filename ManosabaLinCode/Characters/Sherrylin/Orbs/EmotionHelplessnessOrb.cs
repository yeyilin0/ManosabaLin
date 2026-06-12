using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 无助球体：效果待定。
/// </summary>
[RegisterOrb]
public sealed class EmotionHelplessnessOrb : EmotionOrb<EmotionHelplessness>
{
    protected override Color OrbColor => new(0.5f, 0.5f, 0.6f);

    public override Task Passive(PlayerChoiceContext ctx, Creature? target) => Task.CompletedTask;
}
