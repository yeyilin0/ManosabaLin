using ManosabaLin.Characters.Hiro;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class CharacterSmokeTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Hiro>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-hiro-smoke");
    }

    [Fact]
    public async Task Hiro_loads_and_can_play_starter_attack()
    {
        Assert.IsType<Hiro>(Player.Character);
        Assert.Equal(70, Player.Creature.MaxHp);

        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var attack = await AddToHand<HiroAttack>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(attack, enemy);

        Assert.Equal(hpBefore - 6, enemy.CurrentHp);
    }
}
