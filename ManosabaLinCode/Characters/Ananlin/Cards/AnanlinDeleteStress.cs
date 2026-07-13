using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinDeleteStress()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const string SilenceVar = "Silence";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<SilentPower>(SilenceVar, 10m),
        new PowerVar<AnanlinDeletedStressPower>(1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<AnanlinDeletedStressPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (cardPlay.Target is not { } target) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        var silenceCost = DynamicVars[SilenceVar].IntValue;
        var silence = Owner.Creature.GetPower<SilentPower>();
        if (silence is null || silence.Amount < silenceCost) return;

        await PowerCmd.ModifyAmount(choiceContext, silence, -silenceCost, Owner.Creature, this);
        await PowerCmd.Apply<AnanlinDeletedStressPower>(
            choiceContext,
            target,
            DynamicVars["AnanlinDeletedStressPower"].BaseValue,
            Owner.Creature,
            this);

        if (this.Sketchbook() is { } sketchbook && !sketchbook.IsAttackIntent(target))
            sketchbook.TryForgetRecordedAttack(target);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[SilenceVar].UpgradeValueBy(-2m);
    }
}
