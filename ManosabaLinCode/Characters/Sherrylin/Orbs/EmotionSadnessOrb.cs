using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 悲伤球体：每打出一张卡获得1点护盾，回复1点生命。
/// </summary>
[RegisterOrb]
public sealed class EmotionSadnessOrb : EmotionOrb<EmotionSadness>
{
    protected override Color OrbColor => new(0.4f, 0.4f, 0.9f);

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner.Creature) return;

        await CreatureCmd.GainBlock(Owner.Creature, 1m, ValueProp.Unpowered, null);

        if (Owner.Creature.CurrentHp < Owner.Creature.MaxHp)
            await CreatureCmd.Heal(Owner.Creature, 1m);
    }
}
