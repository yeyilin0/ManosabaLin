using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 惊讶能力：回合结束记录手卡数量，下回合开始抽取等量卡牌。
/// </summary>
[RegisterPower]
public sealed class EmotionSurprisePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int _recordedHandSize;

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return Task.CompletedTask;

        var hand = PileType.Hand.GetPile(Owner.Player);
        _recordedHandSize = hand.Cards.Count;

        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side) return;

        if (_recordedHandSize > 0)
        {
            await CardPileCmd.Draw(
                new ThrowingPlayerChoiceContext(),
                _recordedHandSize,
                Owner.Player);
        }
    }
}
