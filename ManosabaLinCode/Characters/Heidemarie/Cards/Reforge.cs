using ManosabaLin.Characters.Common.Components;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class Reforge()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private CardModel[]? _pendingLinkCards;

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new LinkComponent()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        _pendingLinkCards = null;

        var handCards = PileType.Hand.GetPile(Owner).Cards
            .Where(card => !ReferenceEquals(card, this))
            .ToArray();
        foreach (var card in handCards)
            card.TryRemoveComponent<LinkComponent>();

        var candidates = handCards
            .Where(card => card.Pile?.Type == PileType.Hand)
            .ToArray();
        if (candidates.Length == 0)
            return;

        var selectCount = Math.Min(DynamicVars.Cards.IntValue, candidates.Length);
        if (selectCount <= 0)
            return;

        var selectedCards = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(
                new LocString("cards", Id.Entry + ".selectionScreenPrompt"),
                selectCount,
                selectCount))).ToArray();
        if (selectedCards.Length > 0)
            _pendingLinkCards = selectedCards;
    }

    protected override Task AfterCardPlayedLate(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(cardPlay.Card, this))
            return Task.CompletedTask;

        var pendingLinkCards = _pendingLinkCards;
        _pendingLinkCards = null;
        if (pendingLinkCards is not { Length: > 0 })
            return Task.CompletedTask;

        foreach (var card in pendingLinkCards)
        {
            if (card.Pile?.Type == PileType.Hand)
                card.TryAddComponent(new LinkComponent());
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
