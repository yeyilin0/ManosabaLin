using ManosabaLin.Characters.Ananlin.Relics;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class MarginPage() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var sketchbook = Owner.Relics.OfType<AnansSketchbook>().FirstOrDefault();
        if (sketchbook is null) return;

        await sketchbook.UseMarginPage(choiceContext, this);
    }
}
