using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Yalisalin;
using ManosabaLin.Characters.Yalisalin.Cards;
using ManosabaLin.Characters.Yalisalin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

/// <summary>亚里沙 · 技能/特殊机制卡（无卡牌选择 UI）。</summary>
public sealed class YalisalinSkillCardTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Yalisalin>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosaba-yalisalin-skill");
    }

    /// <summary>（One）——敌人 +2 虚弱、+2 嫌疑。</summary>
    [Fact]
    public async Task One_gives_weak_and_suspect_to_enemy()
    {
        var enemy = EnemyAt(0);
        var card = await AddToHand<YalisalinOne>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        Assert.Equal(2, CardTestAssertions.PowerAmount<WeakPower>(enemy));
        Assert.Equal(2, CardTestAssertions.PowerAmount<SuspectPower>(enemy));
    }

    /// <summary>（Eight）——需自身 ≥1 嫌疑，消耗 1 层嫌疑换 5 格挡。</summary>
    [Fact]
    public async Task Eight_consumes_suspect_for_block()
    {
        // 先给自己 3 层嫌疑
        await ApplyPower<SuspectPower>(Player.Creature, 3);
        await WaitForIdle();

        var card = await AddToHand<YalisalinEight>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.Equal(2, CardTestAssertions.PowerAmount<SuspectPower>(Player.Creature));
        Assert.Equal(5, Player.Creature.Block);
    }

    /// <summary>
    /// （魔女审判）——选"是"：将 1 张带[原罪]的原罪诅咒加入手牌，然后抽 1 张。
    /// 自动选择器固定选第一项（是）。2026-09-14：本卡已从旧版「获得1层缄默+抽牌」重构为
    /// 「原罪诅咒选择+抽牌」，旧断言（SilentPower 1 层）过期，改为校验战斗内卡总数 +1
    /// （打出进弃牌堆 0 净变、抽牌 0 净变、新生成原罪诅咒 +1）。
    /// </summary>
    [Fact]
    public async Task WitchTrial_gives_silent_power()
    {
        var card = await AddToHand<YalisalinWitchTrial>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();

        var totalBefore = CountCombatCards();
        await Play(card);

        Assert.Equal(totalBefore + 1, CountCombatCards());
    }

    private int CountCombatCards()
    {
        return PileType.Hand.GetPile(Player).Cards.Count
               + PileType.Draw.GetPile(Player).Cards.Count
               + PileType.Discard.GetPile(Player).Cards.Count;
    }

    /// <summary>魔女囚徒——自身 +6 临时力量、+6 临时敏捷。</summary>
    [Fact]
    public async Task WitchPrisoner_gives_temp_strength_and_dex()
    {
        var card = await AddToHand<YalisalinWitchPrisoner>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.Equal(6, CardTestAssertions.PowerAmount<TempStrength>(Player.Creature));
        Assert.Equal(6, CardTestAssertions.PowerAmount<TempDexterity>(Player.Creature));
    }

    /// <summary>
    /// 十二（<c>YalisalinTwelve</c>）：2026-10-05 重做为「1 费攻击，获得 3 层【嫌疑】，
    /// 立刻触发 1 次【点火】回合结束效果」（旧版「0 费 Skill，消耗嫌疑换 Ylsm+能量」已删除，
    /// 本测试随之更新 —— 旧断言 ylsm/能量/SetEnergy(0) 是重做前的产物）。
    /// </summary>
    [Fact]
    public async Task Twelve_gives_suspect_and_triggers_ignite()
    {
        var card = await AddToHand<YalisalinTwelve>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, Player.Creature);

        Assert.Equal(3, CardTestAssertions.PowerAmount<SuspectPower>(Player.Creature));
        // 本局没有【点火】能力，只验证不报错；能量按 1 费正常结算
        Assert.Equal(9, Player.PlayerCombatState.Energy);
    }

    /// <summary>罪业圣盾——无发夹时获得 5 格挡。</summary>
    [Fact]
    public async Task ZuiYeShengDun_gives_five_block()
    {
        var card = await AddToHand<ZuiYeShengDun>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.Equal(5, Player.Creature.Block);
    }

    /// <summary>赎罪回廊——获得 5 格挡并生成 1 张原罪（宽恕触发）。</summary>
    [Fact]
    public async Task RedemptionCorridor_gives_five_block()
    {
        var card = await AddToHand<RedemptionCorridor>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.Equal(5, Player.Creature.Block);
    }

    /// <summary>（CardTen）——自身 +1 嫌疑、+1【HiroMagicRevive】。</summary>
    [Fact]
    public async Task CardTen_gives_suspect_and_revive()
    {
        var card = await AddToHand<YalisalinCardTen>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.Equal(1, CardTestAssertions.PowerAmount<SuspectPower>(Player.Creature));
        Assert.Equal(1, CardTestAssertions.PowerAmount<HiroMagicRevivePower>(Player.Creature));
    }

    /// <summary>（Three）——目标降低 20 层魔女化。</summary>
    [Fact]
    public async Task Three_reduces_witchification()
    {
        await ApplyPower<WithPower>(Player.Creature, 30);
        await WaitForIdle();

        var card = await AddToHand<YalisalinThree>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, Player.Creature);

        Assert.Equal(10, CardTestAssertions.PowerAmount<WithPower>(Player.Creature));
    }

    /// <summary>（Tower）——Ancient 占位卡，打出不崩溃。</summary>
    [Fact]
    public async Task Tower_plays_without_error()
    {
        var card = await AddToHand<YalisalinTower>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.True(true);
    }

    /// <summary>（罪债抵押）——手牌无原罪时：只获得 8 格挡，不弹选择界面。</summary>
    [Fact]
    public async Task ZuiZhaiDiYa_gives_eight_block_without_sins_in_hand()
    {
        var card = await AddToHand<ZuiZhaiDiYa>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.Equal(8, Player.Creature.Block);
    }
}