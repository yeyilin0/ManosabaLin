using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinTracing() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AnanlinTracingPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<BlankPage>(),
        HoverTipFactory.FromPower<AnanlinTracingPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var tracing = await PowerCmd.Apply<AnanlinTracingPower>(
            choiceContext, Owner.Creature, DynamicVars["AnanlinTracingPower"].BaseValue, Owner.Creature, this);
        if (IsUpgraded && tracing is not null)
            tracing.FreeCopies = true;

        await this.AddBlankPageToHand(false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
