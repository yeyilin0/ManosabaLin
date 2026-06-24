using ManosabaLin.Characters.Heidemarie.Powers;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class SwordCurtain()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Common, TargetType.Self)
{
    public const string BlockPerAuroraKey = nameof(SwordCurtainPower);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SwordCurtainPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<SwordCurtainPower>();
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await PowerCmd.Apply<SwordCurtainPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[BlockPerAuroraKey].BaseValue,
            Owner.Creature,
            this,
            false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[BlockPerAuroraKey].UpgradeValueBy(1m);
    }
}
