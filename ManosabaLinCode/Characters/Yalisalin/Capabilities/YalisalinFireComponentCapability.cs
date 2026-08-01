using ManosabaLin.Characters.Yalisalin.Cards;
using ManosabaLin.Characters.Yalisalin.Components;
using ManosabaLin.Characters.Yalisalin.Relics;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Yalisalin.Capabilities;

[RegisterModelCapability]
[RegisterDefaultModelCapability(typeof(Unwantedkindness))]
[RegisterDefaultModelCapability(typeof(Brokentrust))]
[RegisterDefaultModelCapability(typeof(Beforeforgiven))]
[RegisterDefaultModelCapability(typeof(Glasshug))]
[RegisterDefaultModelCapability(typeof(Stayingstillhurts))]
public sealed class YalisalinFireComponentCapability : CardPlayCapability,
    ICardDescriptionContributor,
    ICardHoverTipContributor,
    ICardGlowContributor,
    IYalisalinFireComponentModifier
{
    private const string LocPrefix = "ManosabaLin.YalisalinFireComponent";

    public bool ShouldGlowGold(CardModel card) => false;

    public bool ShouldGlowRed(CardModel card) => false;

    public IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context)
    {
        yield return new CardDescriptionFragment(
            new LocString("cards", $"{LocPrefix}.prefix"),
            CardDescriptionFragmentPlacement.BeforeBase);
    }

    public IEnumerable<IHoverTip> GetHoverTips(CardModel card)
    {
        yield return CreateHoverTip(card.IsCanonical ? null : card.Owner);
    }

    public static IHoverTip CreateHoverTip(Player? owner = null)
    {
        var title = new LocString("cards", $"{LocPrefix}.hovertip.title");
        var description = new LocString("cards", $"{LocPrefix}.hovertip.description")
            .GetFormattedText();

        if (owner != null)
        {
            var additions = YalisalinsHairpin.GetFireComponentEnhancementDescriptions(owner).ToArray();
            if (additions.Length > 0)
                description += "\n" + string.Join("\n", additions.Select(static text => "- " + text));
        }

        return new HoverTip(title, description);
    }

    protected override async Task<bool> BeforeOwnerCardOnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.PlayIndex != 0)
            return false;
        if (cardPlay.IsAutoPlay)
            return false;
        if (Owner == null || !ReferenceEquals(Owner, cardPlay.Card))
            return false;
        if (YalisalinFireComponentResolver.IsSuppressed(cardPlay.Card))
            return false;

        var context = await YalisalinFireComponentResolver.Resolve(
            choiceContext,
            cardPlay,
            this);

        return context.ShouldSkipSourceCardCore;
    }

    protected override Task OnOwnerCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }
}
