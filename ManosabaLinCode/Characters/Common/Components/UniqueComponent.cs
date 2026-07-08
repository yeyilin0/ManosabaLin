using ManosabaLin.Characters.Common.Components.Abstracts;
using MegaCrit.Sts2.Core.Models;
using System.Linq;

namespace ManosabaLin.Characters.Common.Components;

public sealed partial class UniqueComponent : KeywordLikeComponent
{
    public override bool ShouldAddToDeck(CardModel card)
    {
        if (Card == null || card.Id != Card.Id)
            return true;

        var owner = card.Owner;
        return !owner.Piles
            .SelectMany(p => p.Cards)
            .Any(c => c.Id.Entry == card.Id.Entry);
    }
}
