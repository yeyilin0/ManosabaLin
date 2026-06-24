namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class UnnamedCard38() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public const string DiscardCountKey = "DiscardCount";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(DiscardCountKey, 1m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await CardPileCmd.AutoPlayFromDrawPile(choiceContext, Owner, 1, CardPilePosition.Top, forceExhaust: false);

        var discardCount = DynamicVars[DiscardCountKey].IntValue;
        if (discardCount <= 0)
            return;

        var selected = (await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, discardCount),
            null,
            this)).ToArray();

        if (selected.Length == 0)
            return;

        await CardCmd.Discard(choiceContext, selected);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[DiscardCountKey].UpgradeValueBy(1m);
    }
}
