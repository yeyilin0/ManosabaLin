using ManosabaLin.Characters.Heidemarie.Powers;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class UnnamedCard36()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public const string MarkDamageBonusKey = nameof(UnnamedCard36Power);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<UnnamedCard36Power>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<UnnamedCard36Power>();
            yield return HoverTipFactory.FromPower<MarkPower>();
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await PowerCmd.Apply<UnnamedCard36Power>(
            choiceContext,
            Owner.Creature,
            DynamicVars[MarkDamageBonusKey].BaseValue,
            Owner.Creature,
            this,
            false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[MarkDamageBonusKey].UpgradeValueBy(1m);
    }
}
