using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinLowVoiceRepetition() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => IsUpgraded;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SilentPower>("BaseSilence", 1m),
        new CardsVar(1),
        new BlockVar(0m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<SilentPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (this.Sketchbook() is not { } sketchbook) return;

        await sketchbook.AddSilence(
            choiceContext,
            sketchbook.SkillsPlayedThisTurn + DynamicVars["BaseSilence"].IntValue,
            this);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);

        if (DynamicVars.Block.IntValue > 0)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}
