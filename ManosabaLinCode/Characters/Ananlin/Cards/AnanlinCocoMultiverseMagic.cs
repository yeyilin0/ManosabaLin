namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinCocoMultiverseMagic()
    : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false)
{
    

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }
}
