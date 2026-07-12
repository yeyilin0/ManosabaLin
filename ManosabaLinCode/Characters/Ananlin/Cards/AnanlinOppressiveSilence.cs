using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinOppressiveSilence() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AnanlinOppressiveSilencePower>(1m),
        new PowerVar<SilentPower>("Silence", 0m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<AnanlinOppressiveSilencePower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await PowerCmd.Apply<AnanlinOppressiveSilencePower>(
            choiceContext, Owner.Creature, DynamicVars["AnanlinOppressiveSilencePower"].BaseValue, Owner.Creature, this);

        var silence = DynamicVars["Silence"].IntValue;
        if (silence <= 0) return;

        if (this.Sketchbook() is { } sketchbook)
            await sketchbook.AddSilence(choiceContext, silence, this);
        else
            await PowerCmd.Apply<SilentPower>(choiceContext, Owner.Creature, silence, Owner.Creature, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Silence"].UpgradeValueBy(3m);
    }
}
