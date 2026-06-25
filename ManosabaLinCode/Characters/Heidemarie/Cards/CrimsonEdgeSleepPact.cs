using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class CrimsonEdgeSleepPact()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public const string BounceVar = "Bounce";
    public const string CrimsonSwordsKey = "CrimsonSwords";

    private const decimal SelfBounceAmount = 1m;
    private bool _discardedFromHandByDiscardCommand;

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
    [
        new RestComponent(),
        new BounceComponent(SelfBounceAmount)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new DynamicVar(BounceVar, 1m),
        new DynamicVar(CrimsonSwordsKey, 1m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var discardCount = DynamicVars.Cards.IntValue;
        if (discardCount <= 0)
            return;

        var selected = (await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            Owner,
            new CardSelectorPrefs(
                new LocString("cards", Id.Entry + ".selectionScreenPrompt"),
                discardCount),
            null,
            this)).ToArray();

        if (selected.Length == 0)
            return;

        var bounceAmount = DynamicVars[BounceVar].BaseValue;
        foreach (var card in selected)
            card.TryAddComponent(new BounceComponent(bounceAmount));

        await CardCmd.Discard(choiceContext, selected);
    }

    protected override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? source,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this))
            return Task.CompletedTask;

        _discardedFromHandByDiscardCommand =
            oldPileType == PileType.Hand && card.Pile?.Type == PileType.Discard;

        return Task.CompletedTask;
    }

    protected override async Task AfterCardDiscarded(
        PlayerChoiceContext choiceContext,
        CardModel card,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this) || !_discardedFromHandByDiscardCommand)
            return;

        _discardedFromHandByDiscardCommand = false;
        await SwordTokenGeneration.AddCrimsonSwordsToHand(
            choiceContext,
            Owner,
            DynamicVars[CrimsonSwordsKey].IntValue,
            this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[BounceVar].UpgradeValueBy(1m);
    }
}
