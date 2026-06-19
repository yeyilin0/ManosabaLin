using ManosabaLin.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

[RegisterOrb]
public sealed class EmotionJoyOrb : EmotionOrb<EmotionJoy>
{
    protected override Godot.Color OrbColor => new(1f, 0.8f, 0.2f);

    private readonly HashSet<CardType> _typesPlayedThisTurn = [];

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner) return;

        if (cardPlay.Card.Type == CardType.Attack)
        {
            await PowerCmd.Apply<TempStrengthDown>(
                choiceContext, Owner.Creature, 1, Owner.Creature, null, false);
        }

        if (cardPlay.Card.Type is CardType.Attack or CardType.Skill or CardType.Power)
        {
            _typesPlayedThisTurn.Add(cardPlay.Card.Type);

            if (_typesPlayedThisTurn.Count == 3)
            {
                _typesPlayedThisTurn.Clear();
                await PlayerCmd.GainEnergy(2m, Owner);
            }
        }
    }
}
