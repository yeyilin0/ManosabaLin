using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace ManosabaLin.Characters.Emalin.Components;

public sealed partial class Witchification : CardComponent
{
    public static IHoverTip HoverTip => field ??= new Witchification().HoverTips.First();

    public override IEnumerable<IHoverTip> HoverTips =>
    [
        new HoverTip(
            new LocString("cards", $"{ComponentId}.hovertip.title"),
            new LocString("cards", $"{ComponentId}.hovertip.description")
        )
    ];

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        return Card == card ? playCount + 1 : playCount;
    }

    public override bool TryMergeWith(ICardComponent incoming, ApplyComponentOptions options,
        out ICardComponent? merged)
    {
        if (incoming is Witchification witchification)
        {
            merged = this;
            return true;
        }

        merged = null;
        return false;
    }

    public override bool TrySubtractiveMergeWith(ICardComponent incoming, ApplyComponentOptions options,
        out ICardComponent? merged)
    {
        if (incoming is Witchification)
        {
            merged = null;
            return true;
        }

        merged = null;
        return false;
    }
}
