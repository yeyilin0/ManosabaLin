using ManosabaLin.Characters.Ananlin.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class BlankPage() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var sketchbook = Owner.Relics.OfType<AnansSketchbook>().FirstOrDefault();
        if (sketchbook is null) return;

        await sketchbook.UseBlankPage(choiceContext, this);
    }
}
