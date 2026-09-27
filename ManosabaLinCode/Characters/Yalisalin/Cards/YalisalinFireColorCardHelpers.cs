using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

internal static class YalisalinFireColorCardHelpers
{
    public static async Task Attack(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        CardModel source,
        Creature target,
        decimal damage)
    {
        await DamageCmd.Attack(damage)
            .FromCard(source, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    /// <summary>
    ///     把 <paramref name="amount" /> 格火色逐格随机分给存活敌人，再按敌人合并成一次给予，
    ///     这样「予燎」的超出格数能按每名敌人一次性补结算。
    /// </summary>
    public static async Task GiveRandomlyAmongEnemies(
        PlayerChoiceContext choiceContext,
        Player owner,
        int amount,
        CardModel source,
        bool overflowTriggersConsume)
    {
        if (owner.Creature.CombatState is not { } combatState)
            return;

        var enemies = combatState.Enemies.Where(static enemy => enemy.IsAlive).ToArray();
        if (enemies.Length == 0)
            return;

        var rng = owner.RunState.Rng.CombatTargets;
        var counts = new Dictionary<Creature, int>();
        for (var i = 0; i < amount; i++)
        {
            var target = rng.NextItem(enemies)!;
            counts[target] = counts.GetValueOrDefault(target) + 1;
        }

        foreach (var enemy in enemies)
        {
            if (counts.TryGetValue(enemy, out var count))
                await YalisalinFireColorSystem.GiveFireColor(
                    choiceContext, owner, enemy, count, source, overflowTriggersConsume);
        }
    }
}
