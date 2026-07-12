using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinGroupChatSmell()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    private const string ImmediateSmellKey = "ImmediateSmell";

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AnanlinGroupChatSmellPower>(1m),
        new PowerVar<AnanlinStrangeSmellPower>(ImmediateSmellKey, 0m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinGroupChatSmellPower>(),
        HoverTipFactory.FromPower<AnanlinStrangeSmellPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await PowerCmd.Apply<AnanlinGroupChatSmellPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["AnanlinGroupChatSmellPower"].BaseValue,
            Owner.Creature,
            this);

        var smell = DynamicVars[ImmediateSmellKey].IntValue;
        if (smell <= 0) return;

        var target = PickRandomEnemy();
        if (target is not null)
            await PowerCmd.Apply<AnanlinStrangeSmellPower>(choiceContext, target, smell, Owner.Creature, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[ImmediateSmellKey].UpgradeValueBy(2m);
    }

    private Creature? PickRandomEnemy()
    {
        var enemies = CombatState.HittableEnemies.ToArray();
        return enemies.Length == 0 ? null : Owner.RunState.Rng.CombatTargets.NextItem(enemies);
    }
}
