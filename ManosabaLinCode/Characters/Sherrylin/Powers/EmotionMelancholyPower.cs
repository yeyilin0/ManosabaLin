using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 怅然能力（快乐+悲伤）：攻击牌+1临时减力量+回复队友2血，技能+2盾，3张技能回1能量，能力牌全体+1能量。
/// </summary>
[RegisterPower]
public sealed class EmotionMelancholyPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int _skillCount;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;

        if (cardPlay.Card.Type == CardType.Attack)
        {
            // +1临时减力量
            await PowerCmd.Apply<TempStrengthDown>(
                choiceContext, Owner, 1, Owner, null, false);
            // 随机队友回复2血
            await CreatureCmd.Heal(Owner, 2m);
        }
        else if (cardPlay.Card.Type == CardType.Skill)
        {
            // +2盾
            await CreatureCmd.GainBlock(Owner, 2m, ValueProp.Unpowered, null);
            _skillCount++;
            // 3张技能回1能量
            if (_skillCount % 3 == 0)
                await PlayerCmd.GainEnergy(1m, Owner.Player);
        }
        else if (cardPlay.Card.Type == CardType.Power)
        {
            // 全体获得1能量
            await PlayerCmd.GainEnergy(1m, Owner.Player);
        }
    }
}
