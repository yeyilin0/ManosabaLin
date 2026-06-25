using ManosabaLin.Characters.Common.Components.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace ManosabaLin.Characters.Common.Components;

public sealed partial class BounceComponent : KeywordLikeComponent
{
    public BounceComponent() { }

    public BounceComponent(decimal amount)
    {
        Amount = amount;
    }

    [ComponentState<DynamicVar>]
    public partial decimal Amount { get; set; }

    public override async Task BeforeSideTurnStartPrefix(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState,
        ComponentContext componentContext)
    {
        var card = Card;
        if (card == null || Amount <= 0 || side != CombatSide.Player || !participants.Contains(card.Owner.Creature))
            return;

        var pileType = card.Pile?.Type;
        if (pileType is not (PileType.Draw or PileType.Discard))
            return;

        var hand = PileType.Hand.GetPile(card.Owner);
        if (hand.Cards.Count >= CardPile.MaxCardsInHand)
            return;

        await CardPileCmd.Add(card, PileType.Hand);
        if (card.Pile?.Type != PileType.Hand)
            return;

        Amount -= 1;
        if (Amount <= 0)
            ComponentsCard?.RefRemoveComponent(this);

        await BounceReturnEvents.DispatchReturnedToHand(
            choiceContext,
            combatState,
            new BounceReturnEvent(card.Owner, card, pileType.Value, this));
    }

    public override bool TryMergeWith(ICardComponent incoming, ApplyComponentOptions options, out ICardComponent? merged)
    {
        if (incoming is not BounceComponent bounce)
        {
            merged = null;
            return false;
        }

        Amount += bounce.Amount;
        merged = Amount <= 0 ? null : this;
        return true;
    }

    public override bool TrySubtractiveMergeWith(
        ICardComponent incoming,
        ApplyComponentOptions options,
        out ICardComponent? merged)
    {
        if (incoming is not BounceComponent bounce)
        {
            merged = null;
            return false;
        }

        Amount -= bounce.Amount;
        merged = Amount <= 0 ? null : this;
        return true;
    }
}
