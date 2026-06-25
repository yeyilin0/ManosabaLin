using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class LinkPatternReweaver()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const decimal BounceAmount = 1m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var candidates = PileType.Discard.GetPile(Owner).Cards
            .Where(IsAuroraOrCrimsonSword)
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
        if (selectedCards.Length == 0)
            return;

        foreach (var card in selectedCards)
            card.TryAddComponent(new BounceComponent(BounceAmount));

        await CardPileCmd.Add(selectedCards, PileType.Hand, clonedBy: this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }

    private static bool IsAuroraOrCrimsonSword(CardModel card)
    {
        return card is IAuroraSwordCard or ICrimsonSwordCard;
    }
}
