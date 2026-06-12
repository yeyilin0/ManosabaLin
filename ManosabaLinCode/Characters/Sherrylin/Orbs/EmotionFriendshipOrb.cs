using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 友谊球体：效果待定。
/// </summary>
[RegisterOrb]
public sealed class EmotionFriendshipOrb : EmotionOrb<EmotionFriendship>
{
    protected override Color OrbColor => new(1f, 0.84f, 0f);

    public override Task Passive(PlayerChoiceContext ctx, Creature? target) => Task.CompletedTask;
}
