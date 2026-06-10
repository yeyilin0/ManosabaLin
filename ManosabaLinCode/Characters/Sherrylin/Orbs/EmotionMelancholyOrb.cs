using Godot;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 怅然球体（快乐+悲伤）：攻击牌+1临时减力量+回复队友2血，技能+2盾，3张技能回1能量，能力牌全体+1能量。
/// </summary>
[RegisterOrb]
public sealed class EmotionMelancholyOrb : EmotionOrb<EmotionMelancholy>
{
    private int _skillCount;

    protected override Color OrbColor => new(0.8f, 0.6f, 0.2f);

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner.Creature) return;

        if (cardPlay.Card.Type == CardType.Attack)
        {
            await PowerCmd.Apply<TempStrengthDown>(
                choiceContext, Owner.Creature, 1, Owner.Creature, null, false);
            await CreatureCmd.Heal(Owner.Creature, 2m);
        }
        else if (cardPlay.Card.Type == CardType.Skill)
        {
            await CreatureCmd.GainBlock(Owner.Creature, 2m, ValueProp.Unpowered, null);
            _skillCount++;
            if (_skillCount % 3 == 0)
                await PlayerCmd.GainEnergy(1m, Owner);
        }
        else if (cardPlay.Card.Type == CardType.Power)
        {
            await PlayerCmd.GainEnergy(1m, Owner);
        }
    }
}
