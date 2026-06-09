using Godot;
using ManosabaLin.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 快乐球体：每打出攻击卡获得1层临时减力量，每打出攻击/技能/能力获得1点能量。
/// </summary>
[RegisterOrb]
public sealed class EmotionJoyOrb : EmotionOrb<EmotionJoy>
{
    protected override Color OrbColor => new(1f, 0.8f, 0.2f);

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner.Creature) return;

        if (cardPlay.Card.Type == CardType.Attack)
        {
            await PowerCmd.Apply<TempStrengthDown>(
                choiceContext, Owner.Creature, 1, Owner.Creature, null, false);
        }

        if (cardPlay.Card.Type is CardType.Attack or CardType.Skill or CardType.Power)
        {
            await PlayerCmd.GainEnergy(1m, Owner);
        }
    }
}
