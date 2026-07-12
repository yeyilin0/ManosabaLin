using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinClutchSleeve()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self),
        IAnanlinPeaceOfMindSpecialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("CostReduction", 1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var candidates = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card != this && AnanlinCardHelpers.IsPlayableCombatCard(card))
            .ToList();
        if (candidates.Count == 0) return;

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1))).FirstOrDefault();
        if (selected is null) return;

        selected.GiveSingleTurnRetain();

        var reduction = this.PeaceOfMindAmount() * DynamicVars["CostReduction"].IntValue;
        if (reduction > 0)
            selected.EnergyCost.AddUntilPlayed(-reduction, reduceOnly: true);

        var power = await PowerCmd.Apply<AnanlinClutchSleevePower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
        power?.Track(selected);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
