using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinBackstageResentment()
    : ManosabaCardTemplate(2, CardType.Power, CardRarity.Rare, TargetType.Self),
        IAnanlinPeaceOfMindSpecialCard
{
    private const string PeaceKey = "Peace";
    private const string SilenceKey = "Silence";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AnanlinPeaceOfMindPower>(PeaceKey, 2m),
        new PowerVar<SilentPower>(SilenceKey, 13m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>(),
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<AnanlinBrainwashPower>(),
        HoverTipFactory.FromPower<AnanlinBrainwashBacklashPower>(),
        HoverTipFactory.FromCard<BlankPage>(),
        HoverTipFactory.FromCard<MarginPage>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await this.GainPeaceOfMind(choiceContext, DynamicVars[PeaceKey].IntValue);
        await this.AddSilence(choiceContext, DynamicVars[SilenceKey].IntValue);

        var power = await PowerCmd.Apply<AnanlinBackstageResentmentPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
        if (power is not null && IsUpgraded)
            power.FirstAuditionEffectTriggersAgain = true;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[PeaceKey].UpgradeValueBy(1m);
    }
}
