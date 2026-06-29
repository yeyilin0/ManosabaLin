using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class PeriodicFusionPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private int _turnCounter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        _turnCounter++;

        if (_turnCounter >= Amount)
        {
            _turnCounter = 0;

            await PowerCmd.Apply<EmotionFusionPower>(
                choiceContext, Owner, 1, Owner, null, false);

            var rng = player.RunState.Rng.CombatCardSelection;
            var emotionTypes = new[]
            {
                typeof(EmotionAnger), typeof(EmotionDisgust), typeof(EmotionSadness),
                typeof(EmotionFear), typeof(EmotionJoy), typeof(EmotionSurprise)
            };
            var roll = rng.NextInt(emotionTypes.Length);
            var emotionCard = Owner.CombatState.CreateCard(
                ModelDb.GetById<CardModel>(ModelDb.GetId(emotionTypes[roll])), player);
            if (emotionCard != null)
                await CardPileCmd.Add(emotionCard, MainFile.CaseFilePile, CardPilePosition.Top);
        }
    }
}
