using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace ManosabaLin.Characters.Common.Components.Abstracts;

public abstract class KeywordLikeComponent : CardComponent
{
    public override IEnumerable<IHoverTip> HoverTips => GetHoverTipArray(this);

    public override bool TryMergeWith(ICardComponent incoming, ApplyComponentOptions options, out ICardComponent? merged)
    {
        if (incoming.GetType() != GetType())
        {
            merged = null;
            return false;
        }

        merged = this;
        return true;
    }

    public override bool TrySubtractiveMergeWith(ICardComponent incoming, ApplyComponentOptions options, out ICardComponent? merged)
    {
        if (incoming.GetType() != GetType())
        {
            merged = null;
            return false;
        }

        merged = null;
        return true;
    }


    private static HoverTip CreateHoverTip(string componentId)
    {
        return new HoverTip(
            new LocString("cards", $"{componentId}.hovertip.title"),
            new LocString("cards", $"{componentId}.hovertip.description")
        );
    }

    private static readonly ConcurrentDictionary<Type, IHoverTip[]> HoverTipCache = [];

    private static IHoverTip[] GetHoverTipArray(KeywordLikeComponent instance)
    {
        return HoverTipCache.GetOrAdd(
            instance.GetType(),
            _ => [CreateHoverTip(instance.ComponentId)]
        );
    }

    public static IHoverTip[] GetHoverTip<T>() where T : KeywordLikeComponent, new()
    {
        return HoverTipCache.GetOrAdd(
            typeof(T),
            _ => [CreateHoverTip(new T().ComponentId)]
        );
    }
}
