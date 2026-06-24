using ManosabaLin.Characters.Common.Components.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Common.Components;

public sealed partial class SwordGraveComponent : KeywordLikeComponent
{
    public override void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isInitialShuffle)
    {
        var card = Card;
        if (isInitialShuffle || card == null || card.Owner != player || card.Pile?.Type != PileType.Discard)
            return;

        cards.Remove(card);
    }
}
