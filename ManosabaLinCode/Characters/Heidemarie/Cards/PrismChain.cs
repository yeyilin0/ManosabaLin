using ManosabaLin.Characters.Common.Components;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class PrismChain() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public const string LinkCardsKey = "LinkCards";

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
    [
        new LinkComponent()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new DynamicVar(LinkCardsKey, 1m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);

        var candidates = PileType.Hand.GetPile(Owner).Cards
            .Where(card => !ReferenceEquals(card, this) && !card.HasComponent<LinkComponent>())
            .ToArray();
        if (candidates.Length == 0)
            return;

        var selectCount = Math.Min(DynamicVars[LinkCardsKey].IntValue, candidates.Length);
        if (selectCount <= 0)
            return;

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(
                new LocString("cards", Id.Entry + ".selectionScreenPrompt"),
                0,
                selectCount))).ToArray();

        foreach (var card in selected)
            card.TryAddComponent(new LinkComponent());
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[LinkCardsKey].UpgradeValueBy(1m);
    }
}
