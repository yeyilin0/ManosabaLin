namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinStrangeSmellPower : ManosabaPowerTemplate
{
    private const decimal SpreadRatio = 0.5m;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Percent", 50m),
        new PowerVar<VulnerablePower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (!props.IsPoweredAttack()) return;
        if (result.UnblockedDamage <= 0 || Amount <= 0) return;

        Flash();
        await PowerCmd.ModifyAmount(choiceContext, this, -1, dealer, cardSource);

        var others = CombatState.HittableEnemies
            .Where(enemy => enemy != Owner && enemy.IsAlive)
            .ToArray();
        if (others.Length == 0)
        {
            await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner, 1m, dealer, cardSource);
            return;
        }

        var spreadDamage = Math.Floor(result.UnblockedDamage * SpreadRatio);
        if (spreadDamage <= 0) return;

        foreach (var enemy in others)
        {
            await CreatureCmd.Damage(
                choiceContext,
                enemy,
                spreadDamage,
                ValueProp.Unpowered | ValueProp.Move,
                dealer,
                cardSource,
                null);
        }
    }
}
