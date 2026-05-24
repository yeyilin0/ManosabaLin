using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MinionLib.Component;

namespace ManosabaLin.Characters.Common.Components.Abstracts;

public abstract class KeywordLikeComponent : CardComponent
{
    public override IEnumerable<IHoverTip> HoverTips => [CreateHoverTip(ComponentId)];

    private static HoverTip CreateHoverTip(string componentId)
    {
        return new HoverTip(new LocString("cards", $"{componentId}.hovertip.title"),
            new LocString("cards", $"{componentId}.hovertip.description"));
    }

    public static HoverTip GetHoverTip<T>(T component) where T : KeywordLikeComponent, new()
    {
        return CreateHoverTip(new T().ComponentId);
    }
}
