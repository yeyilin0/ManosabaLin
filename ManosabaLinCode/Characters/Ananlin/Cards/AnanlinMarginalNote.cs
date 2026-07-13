namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinMarginalNote() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<BlankPage>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (this.Sketchbook() is not { } sketchbook) return;
        if (CombatState is null) return;

        var candidates = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card != this && sketchbook.IsFromRecordedPool(card))
            .ToList();
        if (candidates.Count == 0) return;

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1))).FirstOrDefault();
        if (selected is null) return;

        var blankPage = CombatState.CreateCard<BlankPage>(Owner);
        if (IsUpgraded)
            CardCmd.Upgrade(blankPage);

        await CardCmd.Transform(selected, blankPage);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
