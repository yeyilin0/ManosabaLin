using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class EmotionPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card.Type == CardType.Power && cardPlay.Card.Rarity == CardRarity.Token) return;

        Amount++;
        Flash();

        if (Amount >= 13)
        {
            Amount = 0;

            var rng = Owner.Player.RunState.Rng.CombatCardSelection;
            var roll = rng.NextInt(6);

            var combatState = Owner.CombatState;
            if (combatState != null)
            {
                CardModel? emotionCard = roll switch
                {
                    0 => combatState.CreateCard<EmotionAnger>(Owner.Player),
                    1 => combatState.CreateCard<EmotionDisgust>(Owner.Player),
                    2 => combatState.CreateCard<EmotionSadness>(Owner.Player),
                    3 => combatState.CreateCard<EmotionFear>(Owner.Player),
                    4 => combatState.CreateCard<EmotionJoy>(Owner.Player),
                    5 => combatState.CreateCard<EmotionSurprise>(Owner.Player),
                    _ => null
                };

                if (emotionCard != null)
                    await CardPileCmd.Add(emotionCard, MainFile.CaseFilePile, CardPilePosition.Top);
            }
        }
    }
}
