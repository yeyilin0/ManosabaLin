using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinSilentAmplification()
    : ManosabaCardTemplate(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AnanlinSilentAmplificationPower>(1m),
        new IntVar("Rewrites", 3m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<AnanlinSilentAmplificationPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (Owner.Creature.GetPower<AnanlinSilentAmplificationPower>() is not null)
            return;

        var power = await PowerCmd.Apply<AnanlinSilentAmplificationPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["AnanlinSilentAmplificationPower"].BaseValue,
            Owner.Creature,
            this);
        power?.SetRewritesPerAmplification(DynamicVars["Rewrites"].IntValue);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Rewrites"].UpgradeValueBy(-1m);
    }
}
