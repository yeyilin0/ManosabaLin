using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinKeepDistance() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move),
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<SilentPower>("SilenceCost", 1m),
        new PowerVar<VigorPower>("Vigor", 2m),
        new IntVar("Hits", 1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<VigorPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (cardPlay.Target is not { } target) return;

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        if (this.Sketchbook() is not { } sketchbook || sketchbook.HasAnyEnemyAttackIntent())
            return;

        var spent = await sketchbook.SpendSilence(choiceContext, DynamicVars["SilenceCost"].IntValue, this);
        if (spent == DynamicVars["SilenceCost"].IntValue)
            await PowerCmd.Apply<VigorPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["Vigor"].BaseValue,
                Owner.Creature,
                this);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars["Hits"].IntValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Hits"].UpgradeValueBy(1m);
    }
}
