using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinSilentForeshadow() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SilentPower>("Silence", 4m),
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<SilentPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (this.Sketchbook() is not { } sketchbook) return;

        await sketchbook.AddSilence(choiceContext, DynamicVars["Silence"].IntValue, this);
        if (sketchbook.AttacksPlayedThisTurn == 0)
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Silence"].UpgradeValueBy(1m);
    }
}
