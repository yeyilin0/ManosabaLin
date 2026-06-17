using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Common;

public abstract class ManosabaCardCapability : CardCapability,
    ICardDescriptionContributor,
    ICardHoverTipContributor
{
    protected virtual string LocTable => "cards";
    protected virtual string LocKeyPrefix => Id.Entry;

    public virtual IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context)
    {
        if (L10NIfExists("beforeBase") is { } beforeBase)
        {
            AddExtraLocArguments(beforeBase);
            yield return new CardDescriptionFragment(beforeBase, CardDescriptionFragmentPlacement.BeforeBase);
        }

        if (L10NIfExists("afterBase") is { } afterBase)
        {
            AddExtraLocArguments(afterBase);
            yield return new CardDescriptionFragment(afterBase);
        }
    }

    public virtual IEnumerable<IHoverTip> GetHoverTips(CardModel card)
    {
        var title = L10NIfExists("hovertip.title");
        var description = L10NIfExists("hovertip.description");
        if (title != null && description != null)
        {
            DynamicVars.AddTo(title);
            DynamicVars.AddTo(description);
            AddExtraLocArguments(title);
            AddExtraLocArguments(description);
            yield return new HoverTip(title, description);
        }
    }

    private LocString? L10NIfExists(string suffix) => LocString.GetIfExists(LocTable, $"{LocKeyPrefix}.{suffix}");

    protected virtual void AddExtraLocArguments(LocString loc) { }
}
