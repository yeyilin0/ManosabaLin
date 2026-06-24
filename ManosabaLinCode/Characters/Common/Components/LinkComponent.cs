using ManosabaLin.Characters.Common.Components.Abstracts;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;

namespace ManosabaLin.Characters.Common.Components;

public sealed partial class LinkComponent : KeywordLikeComponent
{
    private CardModel[]? _linkedCardsAtPlayStart;

    public override Task BeforeCardPlayedPrefix(CardPlay cardPlay, ComponentContext componentContext)
    {
        if (!ReferenceEquals(cardPlay.Card, Card) || cardPlay.IsAutoPlay || !cardPlay.IsFirstInSeries)
            return Task.CompletedTask;

        _linkedCardsAtPlayStart = PileType.Hand.GetPile(cardPlay.Card.Owner).Cards
            .Where(card => !ReferenceEquals(card, cardPlay.Card) && card.HasComponent<LinkComponent>())
            .ToArray();

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayedPostfix(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(cardPlay.Card, Card) || cardPlay.IsAutoPlay || !cardPlay.IsLastInSeries)
            return;

        var snapshot = _linkedCardsAtPlayStart;
        _linkedCardsAtPlayStart = null;
        if (snapshot is not { Length: > 0 })
            return;

        var cardsToDiscard = snapshot
            .Where(card => card.Pile?.Type == PileType.Hand && card.HasComponent<LinkComponent>())
            .ToArray();

        if (cardsToDiscard.Length > 0)
            await CardCmd.Discard(choiceContext, cardsToDiscard);
    }
}
