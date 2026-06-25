using ManosabaLin.Characters.Heidemarie.Powers;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class UnnamedCard40()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public const string ChancePercentKey = "ChancePercent";
    public const string CrimsonSwordCountKey = "CrimsonSwordCount";
    public const string PowerLayerKey = nameof(UnnamedCard40Power);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(ChancePercentKey, UnnamedCard40Power.BaseChancePercent),
        new CardsVar(CrimsonSwordCountKey, UnnamedCard40Power.BaseCrimsonSwordCount),
        new PowerVar<UnnamedCard40Power>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<UnnamedCard40Power>(); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var power = await PowerCmd.Apply<UnnamedCard40Power>(
            choiceContext,
            Owner.Creature,
            DynamicVars[PowerLayerKey].BaseValue,
            Owner.Creature,
            this,
            false);

        power?.AddLayer(
            DynamicVars[ChancePercentKey].IntValue,
            DynamicVars[CrimsonSwordCountKey].IntValue);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[ChancePercentKey].UpgradeValueBy(1m);
        DynamicVars[CrimsonSwordCountKey].UpgradeValueBy(1m);
    }
}
