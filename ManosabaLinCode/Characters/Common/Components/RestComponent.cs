using ManosabaLin.Characters.Common.Components.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;

namespace ManosabaLin.Characters.Common.Components;

public sealed partial class RestComponent : KeywordLikeComponent
{
    private bool _discardedFromHandByDiscardCommand;
    private bool _isResolvingRest;

    public override Task AfterCardChangedPilesPrefix(
        CardModel card,
        PileType oldPileType,
        AbstractModel? source,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, Card))
            return Task.CompletedTask;

        _discardedFromHandByDiscardCommand =
            oldPileType == PileType.Hand && card.Pile?.Type == PileType.Discard;

        return Task.CompletedTask;
    }

    public override async Task AfterCardDiscardedPostfix(
        PlayerChoiceContext choiceContext,
        CardModel card,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, Card) || !_discardedFromHandByDiscardCommand || _isResolvingRest)
            return;

        _discardedFromHandByDiscardCommand = false;
        _isResolvingRest = true;
        try
        {
            await CardCmd.AutoPlay(choiceContext, card, null);
        }
        finally
        {
            _isResolvingRest = false;
        }
    }
}
