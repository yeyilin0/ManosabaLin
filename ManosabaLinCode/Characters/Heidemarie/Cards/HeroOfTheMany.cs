using ManosabaLin.Characters.Common.Components;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class HeroOfTheMany()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new LinkComponent()];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var candidates = CollectRelevantCards().ToArray();
        if (candidates.Length == 0)
            return;

        var maxSelect = Math.Min(DynamicVars.Cards.IntValue, candidates.Length);
        if (maxSelect <= 0)
            return;

        var selectedCards = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(
                new LocString("cards", Id.Entry + ".selectionScreenPrompt"),
                0,
                maxSelect))).ToArray();
        if (selectedCards.Length == 0)
            return;

        var cardsToMove = CollectCardsMatchingSelectedNames(selectedCards, candidates);
        foreach (var card in cardsToMove)
        {
            card.TryAddComponent(new LinkComponent());
            await CardPileCmd.Add(card, PileType.Draw, clonedBy: this);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }

    private IReadOnlyList<CardModel> CollectCardsMatchingSelectedNames(
        IEnumerable<CardModel> selectedCards,
        IReadOnlyList<CardModel> relevantCards)
    {
        var selectedNames = selectedCards
            .Select(card => card.Id.Entry)
            .Distinct()
            .ToArray();
        var cardsToMove = new List<CardModel>();
        var moved = new HashSet<CardModel>();

        foreach (var name in selectedNames)
        {
            foreach (var card in relevantCards)
            {
                if (card.Id.Entry != name || !moved.Add(card))
                    continue;

                cardsToMove.Add(card);
            }
        }

        return cardsToMove;
    }

    private IReadOnlyList<CardModel> CollectRelevantCards()
    {
        var cards = new List<CardModel>();
        var seen = new HashSet<CardModel>();

        foreach (var pileType in OrderedCombatPiles())
        {
            var pile = pileType.GetPile(Owner);
            foreach (var card in pile.Cards)
                AddIfRelevant(card, cards, seen);
        }

        var combatCards = Owner.PlayerCombatState?.AllCards;
        if (combatCards != null)
        {
            foreach (var card in combatCards)
                AddIfRelevant(card, cards, seen);
        }

        return cards;
    }

    private static IEnumerable<PileType> OrderedCombatPiles()
    {
        yield return PileType.Hand;
        yield return PileType.Draw;
        yield return PileType.Discard;
        yield return PileType.Exhaust;
        yield return PileType.Play;
    }

    private void AddIfRelevant(CardModel card, ICollection<CardModel> cards, ISet<CardModel> seen)
    {
        if (card.Owner != Owner || !seen.Add(card))
            return;

        cards.Add(card);
    }
}
