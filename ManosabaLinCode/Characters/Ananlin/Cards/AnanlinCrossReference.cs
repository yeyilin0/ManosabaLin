namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinCrossReference() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar("Cards", 3)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        for (var i = 0; i < DynamicVars.Cards.IntValue; i++)
            await this.AddMarginPageToHand(IsUpgraded);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
