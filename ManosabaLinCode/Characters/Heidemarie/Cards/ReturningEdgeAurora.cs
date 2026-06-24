using ManosabaLin.Characters.Heidemarie.Powers;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class ReturningEdgeAurora()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public const string AuroraSwordCountKey = nameof(ReturningEdgeAuroraPower);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ReturningEdgeAuroraPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<ReturningEdgeAuroraPower>(); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await PowerCmd.Apply<ReturningEdgeAuroraPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[AuroraSwordCountKey].BaseValue,
            Owner.Creature,
            this,
            false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
