using ManosabaLin.Characters.Common.Components.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Common.Components;

public sealed partial class UniqueComponent: KeywordLikeComponent
{
    public override bool ShouldAddToDeck(CardModel card)
    {
        return Card?.Id.Entry != card.Id.Entry;
    }
}
