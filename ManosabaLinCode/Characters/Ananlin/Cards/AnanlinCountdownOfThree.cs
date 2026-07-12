using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinCountdownOfThree()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self),
        IAnanlinPeaceOfMindSpecialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Bonus", 2)
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
        var power = await PowerCmd.Apply<AnanlinCountdownOfThreePower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
        power?.Arm(this, this.PeaceOfMindAmount() * DynamicVars["Bonus"].IntValue);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
