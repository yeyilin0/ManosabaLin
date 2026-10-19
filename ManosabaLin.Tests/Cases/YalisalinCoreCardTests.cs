using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Yalisalin;
using ManosabaLin.Characters.Yalisalin.Cards;
using ManosabaLin.Characters.Yalisalin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

/// <summary>亚里沙 · 基础攻击/技能/Power 卡（无卡牌选择 UI，可直接打出并断言）。</summary>
public sealed class YalisalinCoreCardTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Yalisalin>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosaba-yalisalin-core");
    }

    /// <summary>未命名111（SinGift）——8 伤，随机获得 1 张带【原罪】的原罪诅咒入手。</summary>
    [Fact]
    public async Task SinGift_deals_eight_damage()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<SinGift>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        Assert.Equal(hpBefore - 8, enemy.CurrentHp);
    }

    /// <summary>未命名（Nyxm）——6 伤，自身 +1 层【Nyxm】。</summary>
    [Fact]
    public async Task Nyxm_deals_six_and_gives_self_nyxm()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<YalisalinNyxm>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        Assert.Equal(hpBefore - 6, enemy.CurrentHp);
        Assert.Equal(1, CardTestAssertions.PowerAmount<NyxmPower>(Player.Creature));
    }

    /// <summary>心理创伤——0 费，自身 +20 层【魔女化】。</summary>
    [Fact]
    public async Task MentalTrauma_gives_twenty_witchification()
    {
        var card = await AddToHand<YalisalinMentalTrauma>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.Equal(20, CardTestAssertions.PowerAmount<WithPower>(Player.Creature));
    }

    /// <summary>双轨日——获得【双轨日】Power。</summary>
    [Fact]
    public async Task DualTrackDay_gives_shuanggri_power()
    {
        var card = await AddToHand<DualTrackDay>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.Equal(1, CardTestAssertions.PowerAmount<ShuangGuiRiPower>(Player.Creature));
    }

    /// <summary>（YalisalinTwo）——自身 +1 层【嫌疑】。</summary>
    [Fact]
    public async Task Two_gives_suspect_to_self()
    {
        var card = await AddToHand<YalisalinTwo>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.Equal(1, CardTestAssertions.PowerAmount<SuspectPower>(Player.Creature));
    }

    /// <summary>嫉恨反噬——基础 13 伤（无起始发夹/无形惩时）。</summary>
    [Fact]
    public async Task JiHenFanShi_deals_thirteen_base_damage()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<JiHenFanShi>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        Assert.Equal(hpBefore - 13, enemy.CurrentHp);
    }

    /// <summary>（YalisalinKkm）——1 伤，目标 +1 层【Kkm】（AnyPlayer 目标）。</summary>
    [Fact]
    public async Task Kkm_deals_one_and_gives_target_kkm()
    {
        var hpBefore = Player.Creature.CurrentHp;
        var card = await AddToHand<YalisalinKkm>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, Player.Creature);

        Assert.Equal(hpBefore - 1, Player.Creature.CurrentHp);
        Assert.Equal(1, CardTestAssertions.PowerAmount<KkmPower>(Player.Creature));
    }
}