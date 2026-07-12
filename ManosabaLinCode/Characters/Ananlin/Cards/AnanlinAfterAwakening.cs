using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinAfterAwakening()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self),
        IAnanlinPeaceOfMindSpecialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new PowerVar<SilentPower>("Silence", 1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>(),
        HoverTipFactory.FromPower<SilentPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var peace = this.PeaceOfMindAmount();

        if (this.HasLostPeaceOfMindThisTurn())
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        else
            await this.GainPeaceOfMind(choiceContext);

        if (peace > 0 && this.Sketchbook() is { } sketchbook)
            await sketchbook.AddSilence(choiceContext, peace * DynamicVars["Silence"].IntValue, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
