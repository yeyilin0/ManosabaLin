using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class ThousandAuroraShatterstrike()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public const string ExtraDamageKey = "ExtraDamage";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new DamageVar(ExtraDamageKey, 1m, ValueProp.Move)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (cardPlay.Target == null)
            return;

        await DealAttack(choiceContext, cardPlay.Target, DynamicVars.Damage.BaseValue);

        var extraSegments = CountAuroraSwordsInHand();
        var extraDamage = DynamicVars[ExtraDamageKey].BaseValue;
        for (var i = 0; i < extraSegments; i++)
        {
            var target = RandomHittableEnemy();
            if (target == null)
                break;

            await DealAttack(choiceContext, target, extraDamage);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[ExtraDamageKey].UpgradeValueBy(1m);
    }

    private int CountAuroraSwordsInHand()
    {
        return PileType.Hand.GetPile(Owner).Cards.Count(card => card is IAuroraSwordCard);
    }

    private Creature? RandomHittableEnemy()
    {
        var combatState = CombatState ?? Owner.Creature.CombatState;
        return Owner.RunState.Rng.CombatTargets.NextItem(combatState?.HittableEnemies ?? []);
    }

    private async Task DealAttack(PlayerChoiceContext choiceContext, Creature target, decimal damage)
    {
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}
