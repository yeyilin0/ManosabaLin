using ManosabaLin.Characters.Common.Components.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Emalin.Components;

public sealed partial class Witchification : KeywordLikeComponent
{
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        return Card == card ? playCount + 1 : playCount;
    }
}
