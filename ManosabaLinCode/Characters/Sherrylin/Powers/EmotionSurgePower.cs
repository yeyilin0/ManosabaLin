using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
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

[RegisterPower]
public sealed class EmotionSurgePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;

        var emotionPower = Owner.GetPower<EmotionPower>();
        if (emotionPower != null)
        {
            var wasBefore = emotionPower.Amount;
            emotionPower.Amount++;

            // 满13层获得情绪卡时恢复1点能量
            if (wasBefore == 12)
                await PlayerCmd.GainEnergy(1, Owner.Player);

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
