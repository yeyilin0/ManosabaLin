namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinSilenceIntentEnergyOption()
    : AnanlinNonRandomCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.None, false)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1)
    ];
}
