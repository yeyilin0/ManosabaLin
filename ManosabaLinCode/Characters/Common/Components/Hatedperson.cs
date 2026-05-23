using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MinionLib.Component;
using MinionLib.Component.Core;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Emalin.Components;

public sealed partial class Hatedperson : CardComponent
{
    public static IHoverTip HoverTip => field ??= new Hatedperson().HoverTips.First();

    public override IEnumerable<IHoverTip> HoverTips =>
    [
        new HoverTip(
            new LocString("cards", $"{ComponentId}.hovertip.title"),
            new LocString("cards", $"{ComponentId}.hovertip.description")
        )
    ];
}