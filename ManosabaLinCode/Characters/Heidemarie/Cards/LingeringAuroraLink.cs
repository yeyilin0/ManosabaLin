using ManosabaLin.Characters.Common.Components;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class LingeringAuroraLink()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const int SelectCount = 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var candidates = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .ToArray();
        if (candidates.Length == 0)
            return;

        var selectCount = Math.Min(SelectCount, candidates.Length);
        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(new LocString("cards", Id.Entry + ".selectionScreenPrompt"), selectCount))).ToArray();

        foreach (var card in selected)
        {
            card.TryAddComponent(new LinkComponent());
            await CardPileCmd.Add(card, PileType.Hand, clonedBy: this);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
