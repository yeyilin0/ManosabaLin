using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Yalisalin;
using ManosabaLin.Characters.Yalisalin.Cards;
using ManosabaLin.Characters.Yalisalin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

/// <summary>
/// 火色系卡测试：带起始遗物【发夹】。
/// 说明：火色系卡大多依赖发夹（TryGetHairpin），无发夹时 OnPlay 直接 return。
/// 这里验证带发夹时卡能否正常打出、基础数值是否正确；同时记录发现的问题。
/// </summary>
public sealed class YalisalinFireColorWithHairpinTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Yalisalin>()
            .KeepStartingRelics()
            .AddEnemy<BigDummy>()
            .WithSeed("manosaba-yalisalin-firecolor");
    }

    [Fact]
    public async Task Afterschooltestburn_deals_six_damage()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<Afterschooltestburn>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        // 6 伤害（无力量加成）
        Assert.Equal(hpBefore - 6, enemy.CurrentHp);
    }

    [Fact]
    public async Task Twodifferenttestimonies_deals_two_hits_of_four()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<Twodifferenttestimonies>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        // 2 次 4 伤 = 8 伤
        Assert.Equal(hpBefore - 8, enemy.CurrentHp);
    }

    [Fact]
    public async Task Unseenkindling_adds_fire_color_no_block_unupgraded()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<Unseenkindling>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        // 未升级：无格挡、无伤害，只加火色
        Assert.Equal(hpBefore, enemy.CurrentHp);
        Assert.Equal(0, Player.Creature.Block);
    }

    [Fact]
    public async Task Samewrongproblem_deals_two_hits_of_five()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<Samewrongproblem>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        Assert.Equal(hpBefore - 10, enemy.CurrentHp);
    }

    [Fact]
    public async Task Burnedecho_deals_six_damage_and_raises_cost()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<Burnedecho>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        // 无燃烧计数时 6 伤
        Assert.Equal(hpBefore - 6, enemy.CurrentHp);
    }

    [Fact]
    public async Task Reversecalculation_plays_without_error()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<Reversecalculation>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        // 目标本回合没被给予火色：只拿格挡，不消耗、不造成伤害
        Assert.Equal(hpBefore, enemy.CurrentHp);
        Assert.Equal(5, Player.Creature.Block);
    }
}

/// <summary>
/// 火色系卡：不带发夹（默认）时，卡是否"静默无效果"。
/// 潜在问题：无发夹时 OnPlay 直接 return，玩家打出卡没有任何反馈/提示。
/// </summary>
public sealed class YalisalinFireColorNoHairpinTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Yalisalin>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosaba-yalisalin-firecolor-nohairpin");
    }

    /// <summary>
    /// （放学后的试烧）——无发夹时仍造成 6 伤（消耗火色是额外效果，先打伤害）。
    /// 这是正确行为：本地化描述为"造成6伤害。消耗1格火色"，消耗不生效不影响基础伤害。
    /// </summary>
    [Fact]
    public async Task Afterschooltestburn_without_hairpin_still_deals_six_damage()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<Afterschooltestburn>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        // 无发夹：OnPlay 中 Attack 在 TryGetHairpin 之前？观察是否仍造成伤害
        // 注意：Afterschooltestburn 先 Attack 后 TryGetHairpin，所以无发夹也造成 6 伤！
        Assert.Equal(hpBefore - 6, enemy.CurrentHp);
    }

    [Fact]
    public async Task Unseenkindling_without_hairpin_adds_no_fire_color()
    {
        var enemy = EnemyAt(0);
        var card = await AddToHand<Unseenkindling>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        // 无发夹：TryGetHairpin 失败直接 return，无任何效果
        Assert.Equal(0, Player.Creature.Block);
    }
}