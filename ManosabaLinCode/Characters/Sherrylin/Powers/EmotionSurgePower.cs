using ManosabaLin.Characters.Common;
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
/// 情绪涌动能力：本回合每打出1张牌，情绪计数额外+1。
/// </summary>
[RegisterPower]
public sealed class EmotionSurgePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;

        // 找到EmotionPower并额外+1计数
        var emotionPower = Owner.GetPower<EmotionPower>();
        if (emotionPower != null)
        {
            emotionPower.Amount++;
            Flash();
        }
    }

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return Task.CompletedTask;
        RemoveInternal();
        return Task.CompletedTask;
    }
}
