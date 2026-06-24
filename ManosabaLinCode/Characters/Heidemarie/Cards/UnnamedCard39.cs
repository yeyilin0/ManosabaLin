using ManosabaLin.Characters.Heidemarie.Powers;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class UnnamedCard39()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public const string MarkVar = nameof(MarkPower);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new PowerVar<MarkPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<MarkPower>(); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var combatState = CombatState ?? Owner.Creature.CombatState;
        if (combatState == null)
            return;

        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var unblockedHitTargets = attack.Results
            .SelectMany(static results => results)
            .Where(static result => result.UnblockedDamage > 0)
            .Select(static result => result.Receiver)
            .Distinct()
            .Count();
        var markGain = unblockedHitTargets * DynamicVars[MarkVar].BaseValue;

        if (markGain <= 0m)
            return;

        await PowerCmd.Apply<MarkPower>(
            choiceContext,
            Owner.Creature,
            markGain,
            Owner.Creature,
            this,
            false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars[MarkVar].UpgradeValueBy(1m);
    }
}
