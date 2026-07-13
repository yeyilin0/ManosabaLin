using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinSpecifiedGenre() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("BlankPages", 1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<BlankPage>(),
        HoverTipFactory.FromPower<AnanlinSpecifiedGenrePower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (CombatState is not { } combatState) return;

        var options = new CardModel[]
        {
            combatState.CreateCard<AnanlinGenreAttackOption>(Owner),
            combatState.CreateCard<AnanlinGenreSkillOption>(Owner),
            combatState.CreateCard<AnanlinGenrePowerOption>(Owner)
        };

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            options,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1))).FirstOrDefault();
        if (selected is null) return;

        var power = await PowerCmd.Apply<AnanlinSpecifiedGenrePower>(
            choiceContext, Owner.Creature, DynamicVars["BlankPages"].BaseValue, Owner.Creature, this);
        if (power is null) return;

        power.PreferredType = selected switch
        {
            AnanlinGenreAttackOption => CardType.Attack,
            AnanlinGenrePowerOption => CardType.Power,
            _ => CardType.Skill
        };
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["BlankPages"].UpgradeValueBy(1m);
    }
}
