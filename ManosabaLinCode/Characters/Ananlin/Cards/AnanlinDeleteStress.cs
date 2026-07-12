using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinDeleteStress()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const string SilenceVar = "Silence";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SilentPower>(SilenceVar, 3m),
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

    protected override bool IsPlayableC =>
        base.IsPlayableC
        && Owner.Creature.GetPower<SilentPower>()?.Amount >= DynamicVars[SilenceVar].IntValue;

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (cardPlay.Target is not { } target) return;

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
        EnergyCost.UpgradeBy(-1);
    }
}
