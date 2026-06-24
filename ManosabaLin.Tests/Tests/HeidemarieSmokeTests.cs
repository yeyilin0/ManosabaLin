using ManosabaLin.Characters.Heidemarie;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieSmokeTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-smoke");
    }

    [Fact]
    public async Task Heidemarie_loads_basic_combat_setup()
    {
        await WaitForIdle();
        await PlayerCmd.SetEnergy(3, Player);
        await WaitForIdle();
        var enemy = EnemyAt(0);
        var strike = await AddToHand<StrikeIronclad>();

        await Play(strike, enemy);

        Assert.IsType<Heidemarie>(Player.Character);
        Assert.IsType<HeidemarieCardPool>(Player.Character.CardPool);
        Assert.True(Player.Creature.MaxHp > 0);
        Assert.NotNull(enemy);
    }
}
