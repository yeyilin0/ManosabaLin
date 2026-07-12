using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinIndexPage() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AnanlinIndexPagePower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<BlankPage>(),
        HoverTipFactory.FromPower<AnanlinIndexPagePower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await PowerCmd.Apply<AnanlinIndexPagePower>(
            choiceContext, Owner.Creature, DynamicVars["AnanlinIndexPagePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["AnanlinIndexPagePower"].UpgradeValueBy(1m);
    }
}
