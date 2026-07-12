using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinConceivingNovelist()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AnanlinConceivingNovelistPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinConceivingNovelistPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var power = await PowerCmd.Apply<AnanlinConceivingNovelistPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["AnanlinConceivingNovelistPower"].BaseValue,
            Owner.Creature,
            this);

        if (power is not null && IsUpgraded)
            power.FreeCopies = true;
    }
}
