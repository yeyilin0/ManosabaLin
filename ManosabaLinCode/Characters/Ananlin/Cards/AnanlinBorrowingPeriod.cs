using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinBorrowingPeriod() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AnanlinBorrowingPeriodPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinBorrowingPeriodPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await PowerCmd.Apply<AnanlinBorrowingPeriodPower>(
            choiceContext, Owner.Creature, DynamicVars["AnanlinBorrowingPeriodPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["AnanlinBorrowingPeriodPower"].UpgradeValueBy(1m);
    }
}
