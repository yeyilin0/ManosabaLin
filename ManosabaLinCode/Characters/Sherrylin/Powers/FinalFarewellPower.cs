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
    public override PowerStackType StackType => PowerStackType.None;

    private decimal _storm;

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
        _storm += amount;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var triggers = (int)(_storm / 40);
        if (triggers <= 0) return;

        _storm -= triggers * 40;

        await PlayerCmd.GainEnergy(triggers, Owner.Player);

        for (int i = 0; i < triggers; i++)
            await CardPileCmd.Draw(choiceContext, 1, Owner.Player);

        var rng = Owner.Player.RunState.Rng.CombatCardSelection;
        var emotionTypes = new[]
        {
            typeof(EmotionAnger), typeof(EmotionDisgust), typeof(EmotionSadness),
            typeof(EmotionFear), typeof(EmotionJoy), typeof(EmotionSurprise)
        };

        for (int i = 0; i < triggers; i++)
        {
            var roll = rng.NextInt(emotionTypes.Length);
            var emotionCard = Owner.CombatState.CreateCard(
                ModelDb.GetById<CardModel>(ModelDb.GetId(emotionTypes[roll])), Owner.Player);
            if (emotionCard != null)
                await CaseFilePileHelper.AddToCaseFilePile(emotionCard, player, CardPilePosition.Top);
        }
    }
}
