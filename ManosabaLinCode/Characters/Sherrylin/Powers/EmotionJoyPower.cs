using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 快乐能力：每打出攻击卡获得1层临时减力量，每打出攻击/技能/能力获得1点能量。
/// </summary>
[RegisterPower]
public sealed class EmotionJoyPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;

        if (cardPlay.Card.Type == CardType.Attack)
        {
            await PowerCmd.Apply<TempStrengthDown>(
                choiceContext, Owner, 1, Owner, null, false);
        }

        if (cardPlay.Card.Type is CardType.Attack or CardType.Skill or CardType.Power)
        {
            await PlayerCmd.GainEnergy(1m, Owner.Player);
        }
    }
}
