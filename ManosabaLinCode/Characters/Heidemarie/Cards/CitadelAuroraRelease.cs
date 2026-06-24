using ManosabaLin.Characters.Heidemarie.Powers;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class CitadelAuroraRelease()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public const string SwordThresholdKey = "SwordThreshold";
    public const string AuroraChainGainKey = nameof(AuroraChainPower);

    private const int BaseSwordThreshold = 2;
    private const int BaseAuroraChainGain = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(SwordThresholdKey, BaseSwordThreshold),
        new PowerVar<AuroraChainPower>(BaseAuroraChainGain)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<CitadelAuroraReleasePower>(); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (Owner.Creature.GetPower<CitadelAuroraReleasePower>() != null)
            return;

        var power = await PowerCmd.Apply<CitadelAuroraReleasePower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this,
            false);

        power?.Configure(
            DynamicVars[SwordThresholdKey].IntValue,
            DynamicVars[AuroraChainGainKey].IntValue);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[SwordThresholdKey].UpgradeValueBy(-1m);
    }
}
