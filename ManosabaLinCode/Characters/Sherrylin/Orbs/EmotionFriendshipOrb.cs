using Godot;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

[RegisterOrb]
public sealed class EmotionFriendshipOrb : EmotionOrb<EmotionFriendship>
{
    private readonly HashSet<CardModel> _costIncreased = [];
    private int _cardsPlayed;

    protected override Color OrbColor => new(1f, 0.5f, 0.8f);

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (card.Owner?.Creature != Owner.Creature) return false;

        if (_costIncreased.Contains(card))
        {
            modifiedCost = originalCost + 1;
            return true;
        }

        return false;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner.Creature) return;

        if (_costIncreased.Contains(cardPlay.Card))
            _costIncreased.Remove(cardPlay.Card);
        else
            _costIncreased.Add(cardPlay.Card);

        _cardsPlayed++;

        await CardPileCmd.Draw(choiceContext, 2, Owner);

        if (_cardsPlayed % 2 == 0)
        {
            await PlayerCmd.GainEnergy(1m, Owner);
        }

        if (_cardsPlayed % 3 == 0)
        {
            await PowerCmd.Apply<TempStrength>(
                choiceContext, Owner.Creature, 1, Owner.Creature, null, false);
        }

        if (_cardsPlayed % 4 == 0)
        {
            var hand = PileType.Hand.GetPile(Owner).Cards
                .Where(c => c.Type is CardType.Curse or CardType.Status)
                .ToList();
            if (hand.Count > 0)
            {
                var rng = Owner.Creature.CombatState.RunState.Rng.CombatCardSelection;
                await CardCmd.Exhaust(choiceContext, hand[rng.NextInt(hand.Count)]);
            }
        }
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Creature.Side)
            _costIncreased.Clear();
    }
}
