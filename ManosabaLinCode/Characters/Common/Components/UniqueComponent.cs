using ManosabaLin.Characters.Common.Components.Abstracts;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using System.Linq;

namespace ManosabaLin.Characters.Common.Components;

public sealed partial class UniqueComponent : KeywordLikeComponent
{
    public override bool ShouldAddToDeck(CardModel card)
    {
        var owner = card.Owner;
        // 检查该玩家所有牌堆
        return !owner.Piles
            .SelectMany(p => p.Cards)
            .Any(c => c.Id.Entry == card.Id.Entry);
    }
}
