namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinSilenceIntentDrawOption()
    : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.None, false)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];
}
