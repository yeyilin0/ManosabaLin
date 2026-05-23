using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MinionLib.Component;

namespace ManosabaLin.Characters.Emalin.Components;
public sealed partial class Executorofjustice : CardComponent
{
    public static IHoverTip HoverTip => field ??= new Executorofjustice().HoverTips.First();

    public override IEnumerable<IHoverTip> HoverTips =>
    [
        new HoverTip(
            new LocString("cards", $"{ComponentId}.hovertip.title"),
            new LocString("cards", $"{ComponentId}.hovertip.description")
        )
    ];
}