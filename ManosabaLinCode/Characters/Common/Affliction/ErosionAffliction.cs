// ErosionAffliction.cs
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Afflictions;

[RegisterAffliction]
public sealed class ErosionAffliction : AfflictionModel
{
    public override bool CanAfflictCardType(CardType cardType) => true;
    public override bool CanAfflictUnplayableCards => true;
    public override bool IsStackable => false;

    public override void AfterApplied()
    {
        Card?.AddKeyword(CardKeyword.Eternal);
        Card?.AddKeyword(CardKeyword.Innate);
    }

    public override void BeforeRemoved()
    {
        Card?.RemoveKeyword(CardKeyword.Eternal);
        Card?.RemoveKeyword(CardKeyword.Innate);
    }

    public override Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Card?.Owner?.Creature is not { } creature) return Task.CompletedTask;
        if (side != creature.Side) return Task.CompletedTask;

        Card.GiveSingleTurnRetain();
        return Task.CompletedTask;
    }
}
