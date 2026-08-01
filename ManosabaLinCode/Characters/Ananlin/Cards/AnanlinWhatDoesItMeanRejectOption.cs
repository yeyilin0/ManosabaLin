namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinWhatDoesItMeanRejectOption()
    : AnanlinNonRandomCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.None, false)
{
}
