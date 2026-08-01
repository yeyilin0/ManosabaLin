namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinSilenceIntentVigorOption()
    : AnanlinNonRandomCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.None, false)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<VigorPower>(2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<VigorPower>()
    ];
}
