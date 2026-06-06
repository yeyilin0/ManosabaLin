using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Ema.Afflictions;

public sealed class BlackHandAffliction : AfflictionModel
{
    public override bool CanAfflictCardType(CardType cardType) => true;
    public override bool CanAfflictUnplayableCards => true;
    public override bool IsStackable => false;

    public override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Card?.Owner?.Creature is not { } creature) return Task.CompletedTask;
        if (side != creature.Side) return Task.CompletedTask;

        Card.GiveSingleTurnRetain();
        return Task.CompletedTask;
    }
}
