using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class FinalFarewellPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private decimal _storm;

    internal void RecordStorm(decimal amount)
    {
        if (amount > 0)
            _storm += amount;
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is not WithPower || power.Owner != Owner) return;
        if (amount <= 0) return;

        power.Amount -= (int)amount;
        RecordStorm(amount);
    }

    /// <summary>
    ///     本地化=规范（2026-10-15 用户改版描述）：<b>回合结束获得 40 点风暴</b>。
    /// </summary>
    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
            RecordStorm(40m);
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var triggers = (int)(_storm / 40);
        if (triggers <= 0) return;

        var cleared = triggers * 40;
        _storm -= cleared;

        await PlayerCmd.GainEnergy(triggers, Owner.Player);

        for (int i = 0; i < triggers; i++)
            await CardPileCmd.Draw(choiceContext, 1, Owner.Player);

        // 本地化=规范（2026-10-15 用户改版描述）：基础情绪不再是「每清除 40 点各给 1 张」，
        // 而是「本次清除大于 40 点」才给 1 张。
        if (cleared <= 40) return;

        var rng = Owner.Player.RunState.Rng.CombatCardSelection;
        var emotionTypes = new[]
        {
            typeof(EmotionAnger), typeof(EmotionDisgust), typeof(EmotionSadness),
            typeof(EmotionFear), typeof(EmotionJoy), typeof(EmotionSurprise)
        };

        var roll = rng.NextInt(emotionTypes.Length);

        // Owner.CombatState 在引擎里是 ICombatState?（可空标注）；此处必在战斗结算中，判空只是消除可空性告警。
        var combatState = Owner.CombatState;
        if (combatState == null) return;

        var emotionCard = combatState.CreateCard(
            ModelDb.GetById<CardModel>(ModelDb.GetId(emotionTypes[roll])), Owner.Player);
        if (emotionCard != null)
            await CaseFilePileHelper.AddToCaseFilePile(
                emotionCard, player, CardPilePosition.Top, choiceContext);
    }
}
