using ManosabaLin.Characters.Yalisalin;
using ManosabaLin.Characters.Yalisalin.Cards;
using ManosabaLin.Characters.Yalisalin.Powers;
using ManosabaLin.Characters.Yalisalin.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

/// <summary>
/// 火色 v2（带发夹）：6 格量表按格位定色（1-2 浅橙 / 3-4 亮黄 / 5-6 赤红），
/// 卡牌从低往高给予，攻击牌造成伤害后从最新格消耗，连续两次同色触发连续奖励。
/// </summary>
public sealed class YalisalinFireColorMoreTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Yalisalin>()
            .KeepStartingRelics()
            .AddEnemy<BigDummy>()
            .WithSeed("manosaba-yalisalin-firemore");
    }

    private YalisalinsHairpin Hairpin
    {
        get
        {
            Assert.True(YalisalinFireColorSystem.TryGetHairpin(Player, out var hairpin));
            return hairpin;
        }
    }

    private async Task PlayWithEnergy(CardModel card, Creature? target = null)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, target);
    }

    [Fact]
    public async Task Slot_colors_follow_position()
    {
        Assert.Equal(YalisalinFireColor.LightOrange, YalisalinsHairpin.SlotColor(1));
        Assert.Equal(YalisalinFireColor.LightOrange, YalisalinsHairpin.SlotColor(2));
        Assert.Equal(YalisalinFireColor.BrightYellow, YalisalinsHairpin.SlotColor(3));
        Assert.Equal(YalisalinFireColor.BrightYellow, YalisalinsHairpin.SlotColor(4));
        Assert.Equal(YalisalinFireColor.Red, YalisalinsHairpin.SlotColor(5));
        Assert.Equal(YalisalinFireColor.Red, YalisalinsHairpin.SlotColor(6));

        // TestTheSpire 要求每条测试至少执行 1 个战斗动作
        await PlayWithEnergy(await AddToHand<YalisalinDefend>());
    }

    /// <summary>攻击不再给予火色；给予由卡牌完成（没看见的火种：无火色时给 2 格）。</summary>
    [Fact]
    public async Task Attack_no_longer_gives_fire_and_cards_give_from_bottom()
    {
        var enemy = EnemyAt(0);

        await PlayWithEnergy(await AddToHand<YalisalinAttack>(), enemy);
        Assert.Equal(0, Hairpin.GetFireColorCount(enemy));

        await PlayWithEnergy(await AddToHand<Unseenkindling>(), enemy);
        Assert.Equal(2, Hairpin.GetFireColorCount(enemy));
        Assert.All(Hairpin.GetFireColorSegments(enemy), s => Assert.Equal(YalisalinFireColor.LightOrange, s.Color));
    }

    /// <summary>满格后攻击一次：消耗的是最新的第 6 格（赤红），不是最早的浅橙。</summary>
    [Fact]
    public async Task Attack_consumes_newest_slot_first()
    {
        var enemy = EnemyAt(0);
        await PlayWithEnergy(await AddToHand<Tomorrowburn>(), enemy);
        Assert.Equal(6, Hairpin.GetFireColorCount(enemy));

        var logStart = Hairpin.ConsumptionLog.Count;
        await PlayWithEnergy(await AddToHand<YalisalinAttack>(), enemy);

        Assert.Equal(5, Hairpin.GetFireColorCount(enemy));
        Assert.Equal(YalisalinFireColor.Red, Hairpin.ConsumptionLog[logStart]);
    }

    /// <summary>连续消耗两格浅橙：每格 4 格挡，第二格凑成连续。</summary>
    [Fact]
    public async Task Two_orange_in_a_row_trigger_continuous()
    {
        var enemy = EnemyAt(0);
        await PlayWithEnergy(await AddToHand<Unseenkindling>(), enemy);
        Assert.Equal(2, Hairpin.GetFireColorCount(enemy));

        var continuousBefore = Hairpin.ContinuousTriggersThisCombat;
        var blockBefore = Player.Creature.Block;

        // 升级后的临界擦边：消耗 1 格并无条件再给 1 格，所以用未升级版，靠「不满格」跳过回填。
        await PlayWithEnergy(await AddToHand<Grazingcritical>(), enemy);
        await PlayWithEnergy(await AddToHand<Grazingcritical>(), enemy);

        Assert.Equal(0, Hairpin.GetFireColorCount(enemy));
        Assert.Equal(blockBefore + 8, Player.Creature.Block);
        Assert.Equal(continuousBefore + 1, Hairpin.ContinuousTriggersThisCombat);
    }

    /// <summary>
    /// 超出格数：满格时再给予 6 格，按第 1..6 格的颜色一次性补结算，
    /// 三对同色各触发一次连续（赤红连续给 1 力量）。
    /// </summary>
    [Fact]
    public async Task Tomorrowburn_overflow_resolves_slot_colors_once()
    {
        var enemy = EnemyAt(0);
        await PlayWithEnergy(await AddToHand<Tomorrowburn>(), enemy);
        Assert.Equal(6, Hairpin.GetFireColorCount(enemy));

        var logStart = Hairpin.ConsumptionLog.Count;
        var continuousBefore = Hairpin.ContinuousTriggersThisCombat;
        await PlayWithEnergy(await AddToHand<Tomorrowburn>(), enemy);

        Assert.Equal(6, Hairpin.GetFireColorCount(enemy));
        Assert.Equal(
            [
                YalisalinFireColor.LightOrange, YalisalinFireColor.LightOrange,
                YalisalinFireColor.BrightYellow, YalisalinFireColor.BrightYellow,
                YalisalinFireColor.Red, YalisalinFireColor.Red
            ],
            Hairpin.ConsumptionLog.Skip(logStart).ToArray());
        Assert.Equal(continuousBefore + 3, Hairpin.ContinuousTriggersThisCombat);
        Assert.Equal(1, CardTestAssertions.PowerAmount<StrengthPower>(Player.Creature));
    }

    /// <summary>反向验算：消耗本回合给予的最早一格（浅橙），消耗效果翻倍 → 5 格挡 + 4×2。</summary>
    [Fact]
    public async Task Reversecalculation_consumes_earliest_given_this_turn_doubled()
    {
        var enemy = EnemyAt(0);
        await PlayWithEnergy(await AddToHand<Tomorrowburn>(), enemy);

        var blockBefore = Player.Creature.Block;
        var logStart = Hairpin.ConsumptionLog.Count;
        await PlayWithEnergy(await AddToHand<Reversecalculation>(), enemy);

        Assert.Equal(5, Hairpin.GetFireColorCount(enemy));
        Assert.Equal(YalisalinFireColor.LightOrange, Hairpin.ConsumptionLog[logStart]);
        Assert.Equal(blockBefore + 5 + 8, Player.Creature.Block);
    }

    /// <summary>同一道错题：两段攻击消耗 亮黄→浅橙（异色），下一张技能牌 0 费。</summary>
    [Fact]
    public async Task Samewrongproblem_different_colors_make_next_skill_free()
    {
        var enemy = EnemyAt(0);
        await PlayWithEnergy(await AddToHand<Unseenkindling>(), enemy); // 2 格
        await PlayWithEnergy(await AddToHand<Unseenkindling>(), enemy); // 已有火色：+1 → 第 3 格亮黄
        Assert.Equal(3, Hairpin.GetFireColorCount(enemy));

        await PlayWithEnergy(await AddToHand<Samewrongproblem>(), enemy);
        Assert.Equal(1, Hairpin.PendingFreeSkillCount);

        var skill = await AddToHand<YalisalinDefend>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(skill);
        Assert.Equal(10, Player.PlayerCombatState!.Energy);
        Assert.Equal(0, Hairpin.PendingFreeSkillCount);
    }

    /// <summary>被缚的普罗米修斯：之后的消耗不触发连续，每次对该敌人追加 6 点伤害。</summary>
    [Fact]
    public async Task BoundPrometheus_blocks_continuous_and_adds_damage()
    {
        var enemy = EnemyAt(0);
        await PlayWithEnergy(await AddToHand<Tomorrowburn>(), enemy);

        await PlayWithEnergy(await AddToHand<BoundPrometheus>(), enemy);
        Assert.NotNull(Player.Creature.GetPower<BoundPrometheusPower>());
        Assert.Equal(5, Hairpin.GetFireColorCount(enemy));

        var continuousBefore = Hairpin.ContinuousTriggersThisCombat;
        var hpBefore = enemy.CurrentHp;
        await PlayWithEnergy(await AddToHand<Grazingcritical>(), enemy);

        Assert.Equal(continuousBefore, Hairpin.ContinuousTriggersThisCombat);
        Assert.True(hpBefore - enemy.CurrentHp >= BoundPrometheusPower.DamagePerConsume);
    }

    /// <summary>窗上的罚单：每次给予额外 +1 格。</summary>
    [Fact]
    public async Task Ticketonwindow_adds_one_to_each_give()
    {
        var enemy = EnemyAt(0);
        await PlayWithEnergy(await AddToHand<Ticketonwindow>());
        await PlayWithEnergy(await AddToHand<Unseenkindling>(), enemy);

        Assert.Equal(3, Hairpin.GetFireColorCount(enemy));
    }

    /// <summary>烬火洗礼：清空所有敌人的火色，至少 6 格时抽 3 张并获得 3 点能量。</summary>
    [Fact]
    public async Task EmberBaptism_consumes_all_and_rewards_full_gauge()
    {
        var enemy = EnemyAt(0);
        await PlayWithEnergy(await AddToHand<Tomorrowburn>(), enemy);

        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<EmberBaptism>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.Equal(0, Hairpin.GetFireColorCount(enemy));
        Assert.True(hpBefore - enemy.CurrentHp >= 36);
        // 10 - 3 费 + 3 能量 + 亮黄连续 1 能量
        Assert.Equal(11, Player.PlayerCombatState!.Energy);
    }

    [Fact]
    public async Task Burntthermometerpaper_loses_three_life_and_gives_four()
    {
        var enemy = EnemyAt(0);
        var hpBefore = Player.Creature.CurrentHp;
        await PlayWithEnergy(await AddToHand<Burntthermometerpaper>(), enemy);

        Assert.Equal(hpBefore - 3, Player.Creature.CurrentHp);
        Assert.Equal(4, Hairpin.GetFireColorCount(enemy));
    }

    [Fact]
    public async Task Ashinpages_gives_four_to_single_enemy()
    {
        var enemy = EnemyAt(0);
        var card = await AddToHand<Ashinpages>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();

        // AllEnemies 牌不能带目标，否则 harness 判成打出失败
        await Play(card);
        Assert.Equal(4, Hairpin.GetFireColorCount(enemy));
    }

    [Fact]
    public async Task Pocketmatchbox_damages_when_entering_new_color_band()
    {
        var enemy = EnemyAt(0);
        await PlayWithEnergy(await AddToHand<Unseenkindling>(), enemy); // 2 格浅橙

        var hpBefore = enemy.CurrentHp;
        await PlayWithEnergy(await AddToHand<Pocketmatchbox>(), enemy); // 第 3 格亮黄 ≠ 第 2 格浅橙
        Assert.Equal(3, Hairpin.GetFireColorCount(enemy));
        Assert.Equal(hpBefore - 8, enemy.CurrentHp);
    }

    [Fact]
    public async Task Power_cards_apply_their_effects()
    {
        await PlayWithEnergy(await AddToHand<Unusedconclusion>());
        await PlayWithEnergy(await AddToHand<Dontcooldown>());
        await PlayWithEnergy(await AddToHand<Thirteenthlistener>());

        Assert.NotNull(Player.Creature.GetPower<MixedConclusionPower>());
        Assert.NotNull(Player.Creature.GetPower<ThirteenthListenerPower>());
        Assert.Equal(4, Hairpin.TurnStartFireGift);
    }

    [Fact]
    public async Task Deadlinehandoff_deals_three_hits_of_five()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        await PlayWithEnergy(await AddToHand<Deadlinehandoff>(), enemy);
        Assert.Equal(hpBefore - 15, enemy.CurrentHp);
    }

    /// <summary>卡面文本的变量都能解析（未升级与升级后），避免文案引用了卡上没有的动态变量。</summary>
    [Fact]
    public async Task Fire_color_card_descriptions_render()
    {
        AssertDescriptionRenders<Afterschooltestburn>();
        AssertDescriptionRenders<Ashinpages>();
        AssertDescriptionRenders<Temperatureproof>();
        AssertDescriptionRenders<YalisalinWitchPrisoner>();
        AssertDescriptionRenders<Reversecalculation>();
        AssertDescriptionRenders<Dontcooldown>();
        AssertDescriptionRenders<Grazingcritical>();
        AssertDescriptionRenders<Pocketmatchbox>();
        AssertDescriptionRenders<Ticketonwindow>();
        AssertDescriptionRenders<Unusedconclusion>();
        AssertDescriptionRenders<Samewrongproblem>();
        AssertDescriptionRenders<Burntthermometerpaper>();
        AssertDescriptionRenders<EmberBaptism>();
        AssertDescriptionRenders<Deadlinehandoff>();
        AssertDescriptionRenders<Tomorrowburn>();
        AssertDescriptionRenders<KindlingSparkToken>();
        AssertDescriptionRenders<Thirteenthlistener>();
        AssertDescriptionRenders<BoundPrometheus>();
        AssertDescriptionRenders<Burnedapology>();

        await PlayWithEnergy(await AddToHand<YalisalinDefend>());
    }

    private void AssertDescriptionRenders<TCard>() where TCard : CardModel
    {
        var card = Combat.CreateCard<TCard>(Player);
        foreach (var upgraded in new[] { false, true })
        {
            if (upgraded)
                CardCmd.Upgrade(card);

            var text = card.GetDescriptionForPile(PileType.Hand);
            Assert.False(string.IsNullOrWhiteSpace(text), $"{typeof(TCard).Name} 描述为空");
            Assert.DoesNotContain("{", text);
            Assert.DoesNotContain("升温", text);
            Assert.DoesNotContain("封存", text);
        }
    }

    [Fact]
    public async Task Temperatureproof_gives_then_detonates_orange_for_double_block()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        await PlayWithEnergy(await AddToHand<Temperatureproof>(), enemy);

        Assert.Equal(hpBefore - 8, enemy.CurrentHp);
        Assert.Equal(0, Hairpin.GetFireColorCount(enemy));
        // 浅橙消耗效果 4 + 本牌 5 × 2（引爆的是浅橙）
        Assert.Equal(14, Player.Creature.Block);
    }
}
