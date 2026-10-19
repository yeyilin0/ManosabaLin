using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Yalisalin.Powers;
using ManosabaLin.Characters.Yalisalin.Vfx;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     爆裂魔法（X 费 攻击・全体・稀有）：
///     先给予所有敌人等同于消耗能量的易伤与虚弱，以及 1 层【能力失效】，
///     再对所有敌人造成「消耗能量 × 10」点伤害（升级 ×13）。
///     一场战斗中只能打出 1 张；能量为 0 时无法打出；
///     打出后立即结束本回合，并从本场战斗中移除。
/// </summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class ExplosionMagic()
    : ManosabaCardTemplate(-1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    private const int BaseDamagePerEnergy = 10;
    private const int UpgradedDamagePerEnergy = 13;

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("DamagePerEnergy", BaseDamagePerEnergy)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<VulnerablePower>();
            yield return HoverTipFactory.FromPower<WeakPower>();
            yield return HoverTipFactory.FromPower<AbilityNullifyPower>();
        }
    }

    /// <summary>
    ///     能量为 0 时无法打出；本场战斗已经打出过 1 张时也无法打出。
    /// </summary>
    protected override bool IsPlayableC
    {
        get
        {
            if (!base.IsPlayableC)
                return false;

            if (Owner is not { } owner)
                return false;

            if ((owner.PlayerCombatState?.Energy ?? 0) <= 0)
                return false;

            return owner.Creature.GetPower<ExplosionMagicUsedPower>() is null;
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await ResolveExplosion(choiceContext, cardPlay);

        // 「打出移除」：与「子弹」同款，直接在 OnPlay 里下移除命令，不挂组件。
        await CardPileCmd.RemoveFromCombat(this);
    }

    private async Task ResolveExplosion(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner is not { } owner || CombatState is not { } combatState)
            return;

        var spent = ResolveEnergyXValue();
        if (spent <= 0)
            return;

        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 本场战斗只能打出一张：先落标记，避免任何形式的重复结算。
        await PowerCmd.Apply<ExplosionMagicUsedPower>(
            choiceContext, owner.Creature, 1m, owner.Creature, this, true);

        var enemies = combatState.HittableEnemies.Where(static enemy => enemy.IsAlive).ToList();

        // 先上 debuff，再结算伤害。
        foreach (var enemy in enemies)
        {
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext, enemy, spent, owner.Creature, this, false);
            await PowerCmd.Apply<WeakPower>(
                choiceContext, enemy, spent, owner.Creature, this, false);
            await PowerCmd.Apply<AbilityNullifyPower>(
                choiceContext, enemy, 1m, owner.Creature, this, false);
        }

        using var explosion = ExplosionMagicVfx.Play(owner.Creature, enemies);
        if (explosion != null && !await explosion.Impact)
            return; // The combat room was removed before detonation.

        var damage = spent * DynamicVars["DamagePerEnergy"].BaseValue;
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithNoAttackerAnim()
            .Execute(choiceContext);

        if (explosion != null)
            await explosion.Finished;

        // 打出后立即结束本回合。
        PlayerCmd.EndTurn(owner, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["DamagePerEnergy"].UpgradeValueBy(UpgradedDamagePerEnergy - BaseDamagePerEnergy);
    }
}
