using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinMiriasMoviePromise() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinMoviePromisePower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var candidates = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card != this && CanPromise(card))
            .ToList();
        if (candidates.Count == 0) return;

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1))).FirstOrDefault();
        if (selected is null) return;

        var promise = await PowerCmd.Apply<AnanlinMoviePromisePower>(
            choiceContext, Owner.Creature, 1, Owner.Creature, this);
        if (promise is null) return;

        promise.AddPromise(selected);
        await CardPileCmd.RemoveFromCombat(selected);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }

    private static bool CanPromise(CardModel card)
    {
        return card.Rarity is not CardRarity.Status
            and not CardRarity.Curse
            and not CardRarity.Quest
            and not CardRarity.Event;
    }
}
