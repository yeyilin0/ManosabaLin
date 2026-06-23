using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

[RegisterOrb]
public sealed class EmotionElationOrb : EmotionOrb<EmotionElation>
{
    protected override Color OrbColor => new(1f, 0.9f, 0.3f);
}
