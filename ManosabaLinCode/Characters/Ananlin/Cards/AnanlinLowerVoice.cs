using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinLowerVoice() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move),
        new PowerVar<SilentPower>("Silence", 2m),
        new PowerVar<SilentPower>("AttackIntentSilence", 2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<SilentPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (this.Sketchbook() is not { } sketchbook) return;

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        var silence = DynamicVars["Silence"].IntValue;
        if (sketchbook.HasAnyEnemyAttackIntent())
            silence += DynamicVars["AttackIntentSilence"].IntValue;

        await sketchbook.AddSilence(choiceContext, silence, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
