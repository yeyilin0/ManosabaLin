namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class AnanlinCocoMultiverseMagic()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }
}
