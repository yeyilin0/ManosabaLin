using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinAvertedGaze()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self),
        IAnanlinPeaceOfMindSpecialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AnanlinAvertedGazePower>(1m),
        new PowerVar<AnanlinPeaceOfMindPower>("Peace", 1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>(),
        HoverTipFactory.FromPower<AnanlinAvertedGazePower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await PowerCmd.Apply<AnanlinAvertedGazePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["AnanlinAvertedGazePower"].IntValue,
            Owner.Creature,
            this);

        var peace = DynamicVars["Peace"].IntValue;
        if (peace > 0)
            await this.GainPeaceOfMind(choiceContext, peace);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Peace"].UpgradeValueBy(1m);
    }
}
