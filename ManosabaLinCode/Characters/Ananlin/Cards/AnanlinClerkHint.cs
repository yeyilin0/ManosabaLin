using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinClerkHint() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SilentPower>("GainSilence", 3m),
        new PowerVar<SilentPower>("SpendSilence", 3m),
        new CardsVar(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<SilentPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (this.Sketchbook() is not { } sketchbook) return;

        var gainOption = CombatState.CreateCard<AnanlinClerkHintSilenceOption>(Owner);
        var drawOption = CombatState.CreateCard<AnanlinClerkHintDrawOption>(Owner);
        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            [gainOption, drawOption],
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1))).FirstOrDefault();

        if (selected is AnanlinClerkHintDrawOption)
        {
            var spent = await sketchbook.SpendSilence(choiceContext, DynamicVars["SpendSilence"].IntValue, this);
            if (spent == DynamicVars["SpendSilence"].IntValue)
            {
                await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
                return;
            }
        }

        await sketchbook.AddSilence(choiceContext, DynamicVars["GainSilence"].IntValue, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["GainSilence"].UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
