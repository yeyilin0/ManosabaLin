namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinSilenceIntentBlockOption()
    : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.None, false)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move)
    ];
}
