using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Yalisalin;
using ManosabaLin.Characters.Yalisalin.Capabilities;
using ManosabaLin.Characters.Yalisalin.Cards;
using ManosabaLin.Characters.Yalisalin.Components;
using ManosabaLin.Characters.Yalisalin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Text.RegularExpressions;
using TestTheSpire;
using STS2RitsuLib.Models.Capabilities;
using Xunit;

namespace ManosabaLin.Tests.Cases;

/// <summary>
/// 火组件卡测试（带发夹）。
/// 火组件卡：给手牌卡添加【火焰组件】，组件被烧毁时触发额外效果。
/// </summary>
public sealed class YalisalinFireComponentTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Yalisalin>()
            .KeepStartingRelics()
            .AddEnemy<BigDummy>()
            .WithSeed("manosaba-yalisalin-firecomponent");
    }

    // 注：Glasshug/Unwantedkindness/Beforeforgiven/Fifthselfproof 之前因 headless 弹窗崩溃被移除，
    // 2026-09-09 已在测试 Entry 安装自动卡牌选择器（CardSelectCmd.UseSelector localOnly）修复，
    // 现在重新启用测试。

    [Fact]
    public async Task Glasshug_gives_eight_block()
    {
        var card = await AddToHand<Glasshug>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.Equal(8, Player.Creature.Block);
    }

    /// <summary>
    /// （把道歉烧成灰·回归）——被余火烧掉的牌先自动打出一次，且不消耗能量。
    /// 牌堆补到 13 张以上，避免发夹额外连接一张本来就 0 费的随机牌，让被烧的是 1 费防御。
    /// </summary>
    [Fact]
    public async Task Burnedapology_autoplays_burned_card_for_free()
    {
        await ApplyPower<BurnedApologyPower>(Player.Creature, 1);
        for (var i = 0; i < 13; i++)
        {
            var filler = Combat.CreateCard<YalisalinDefend>(Player);
            await CardPileCmd.AddGeneratedCardToCombat(filler, PileType.Draw, Player);
        }

        var card = await AddToHand<Unwantedkindness>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        var exhaust = PileType.Exhaust.GetPile(Player).Cards.ToArray();
        Assert.Single(exhaust);
        Assert.IsType<YalisalinDefend>(exhaust[0]);

        // 只付了不需要的善意自己的 1 费；被烧的防御免费打出（5 格挡）
        Assert.Equal(9, Player.PlayerCombatState!.Energy);
        Assert.Equal(7 + 5, Player.Creature.Block);
    }

    [Fact]
    public async Task Unwantedkindness_gives_seven_block()
    {
        var card = await AddToHand<Unwantedkindness>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.Equal(7, Player.Creature.Block);
    }

    /// <summary>
    /// （不需要的善意·回归）——打出后若本次通过余火烧掉了牌，应抽 1 张。
    /// 修复前：手动打出时源卡在 Play 堆，EnumerateModifierObjects（手/抽/弃）枚举不到源卡，
    /// Unwantedkindness.AfterFireComponentBurned 永不触发 → 「烧牌没抽牌」。
    /// 自动选择器固定选第一项（源卡）→ 连接牌被烧 → 修复后应抽 1。
    /// </summary>
    [Fact]
    public async Task Unwantedkindness_burns_card_and_draws_one()
    {
        var card = await AddToHand<Unwantedkindness>();
        await AddToHand<YalisalinDefend>(); // 连接牌候选（会被烧）

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();

        var handBefore = PileType.Hand.GetPile(Player).Cards.ToArray();

        await Play(card);

        // 被烧的连接牌进入消耗堆
        var exhaust = PileType.Exhaust.GetPile(Player).Cards.ToArray();
        Assert.Single(exhaust);

        var linkedWasInHand = handBefore.Contains(exhaust[0]);

        // 修复后：手牌 = 原手牌 - 打出的本卡 - 从手牌烧掉的连接牌(若在手里) + 抽 1
        var expectedHand = handBefore.Length - 1 - (linkedWasInHand ? 1 : 0) + 1;
        Assert.Equal(expectedHand, PileType.Hand.GetPile(Player).Cards.Count);
    }

    /// <summary>
    /// （被原谅之前）——12 基础伤害；本次余火成功烧牌后额外 8 伤。
    /// 自动选择器固定选原牌 → 连接牌被烧 → 合计 12+8=20。
    /// 2026-09-14 修复源卡回调缺失（EnumerateModifierObjects 漏掉 Play 堆中的源卡）后，
    /// AfterFireComponentBurned 真正生效；旧断言 12 伤是 bug 时代的产物。
    /// </summary>
    [Fact]
    public async Task Beforeforgiven_deals_twelve_damage()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<Beforeforgiven>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        Assert.Equal(hpBefore - 20, enemy.CurrentHp);
    }

    [Fact]
    public async Task Burnedecho_deals_six_damage_without_burn_count()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<Burnedecho>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        // 本回合无组件燃烧记录 → 6 伤
        Assert.Equal(hpBefore - 6, enemy.CurrentHp);
    }

    [Fact]
    public async Task Fifthselfproof_deals_no_damage_without_fire_use_count()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<Fifthselfproof>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        // FireUseCount=0 → 无额外组件效果，本卡无基础伤害 → 敌人血量不变
        Assert.Equal(hpBefore, enemy.CurrentHp);
    }

    [Fact]
    public async Task Holdmypain_plays_without_error()
    {
        var card = await AddToHand<Holdmypain>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    [Fact]
    public async Task Dazzlingtolerance_plays_without_error()
    {
        var card = await AddToHand<Dazzlingtolerance>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    [Fact]
    public async Task Burnedapology_plays_without_error()
    {
        var card = await AddToHand<Burnedapology>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    [Fact]
    public async Task Unneededgoodchild_plays_without_error()
    {
        var card = await AddToHand<Unneededgoodchild>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    [Fact]
    public async Task Twoseparatedends_plays_without_error()
    {
        var card = await AddToHand<Twoseparatedends>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    [Fact]
    public async Task Warmthshouldnotstay_plays_without_error()
    {
        var card = await AddToHand<Warmthshouldnotstay>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    /// <summary>（归还的发带）——5 格挡 + 5 伤；弃牌堆无卡可回收时选择跳过。</summary>
    [Fact]
    public async Task Returnedhairribbon_gives_block_and_damage()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<Returnedhairribbon>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        Assert.Equal(5, Player.Creature.Block);
        Assert.Equal(hpBefore - 5, enemy.CurrentHp);
    }

    /// <summary>（不要看我）——手牌无候选（仅自身）时选择跳过，不崩溃。</summary>
    [Fact]
    public async Task Dontlookatme_plays_without_error()
    {
        var card = await AddToHand<Dontlookatme>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.True(true);
    }

    /// <summary>（破碎的信任）——6 伤；手牌无候选时选择跳过。</summary>
    [Fact]
    public async Task Brokentrust_deals_six_damage()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var card = await AddToHand<Brokentrust>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, enemy);

        Assert.Equal(hpBefore - 6, enemy.CurrentHp);
    }

    /// <summary>（静止也会痛）——手牌仅自身时 FromHand 无候选，跳过不崩溃。</summary>
    [Fact]
    public async Task Stayingstillhurts_plays_without_error()
    {
        var card = await AddToHand<Stayingstillhurts>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.True(true);
    }

    /// <summary>（不要带我回家）——10 格挡；手牌无候选时选择跳过。</summary>
    [Fact]
    public async Task Dontbringmehome_gives_ten_block()
    {
        var card = await AddToHand<Dontbringmehome>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);

        Assert.Equal(10, Player.Creature.Block);
    }

    /// <summary>
    /// （余火右键·联机同步编码回归）——右键「添火」在联机下必须走同步通道：
    /// UI 侧只登记 <c>RightClickRecords</c>（<c>0</c> = 跳过，<c>n &gt; 0</c> = 作用在
    /// <c>ChoiceOptions[n-1]</c>），两端拿同一份索引回放才会得到相同的 <c>AppliedRightClicks</c>。
    ///
    /// 2026-09-25 的联机不同步根因正是「右键直接改本机 context」：
    /// 房主白拿 1 层紫藤亚里沙的魔法 + 1 点能量，客机什么都没有 ⇒ checksum #102 分歧。
    /// 这条用例锁住修复后的编码与 FIFO 回放语义。
    /// </summary>
    [Fact]
    public async Task FireComponent_right_click_records_encode_and_replay_identically()
    {
        var source = await AddToHand<Glasshug>();
        var other = await AddToHand<YalisalinDefend>();
        var component = source.GetOrCreateCapability<YalisalinFireComponentCapability>();

        var context = new YalisalinFireComponentContext(Player, NewCardPlay(source), component);
        context.AddChoiceOption(source);
        context.AddChoiceOption(other);

        // 队首有两个待应用强化：第一个右键给「other」，第二个直接跳过。
        context.AddRightClickRequest(new YalisalinFireRightClickRequest(
            YalisalinFireRightClickKind.PainKeeper, "test.painKeeper"));
        context.AddRightClickRequest(new YalisalinFireRightClickRequest(
            YalisalinFireRightClickKind.PainKeeper, "test.painKeeper"));
        Assert.Equal(2, context.RemainingRightClickCount);

        context.RecordRightClickApplication(other);
        context.RecordRightClickSkip();

        // 编码：other 是 ChoiceOptions[1] ⇒ 2；跳过 ⇒ 0。
        Assert.Equal([2, 0], context.RightClickRecords.ToArray());
        Assert.Equal(0, context.RemainingRightClickCount);

        // 本机走的是「刚发给对手的那份列表」，远端走的是收到的那份 —— 回放结果必须一致。
        context.ApplyRightClickRecords(context.RightClickRecords);

        var applied = Assert.Single(context.AppliedRightClicks);
        Assert.Equal(YalisalinFireRightClickKind.PainKeeper, applied.Kind);
        Assert.Same(other, applied.Card);
        Assert.Empty(context.PendingRightClicks);

        // 末尾打出一次，满足 harness 的「至少 1 个战斗动作」校验（否则整轮 TERMINATE）。
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(other);
    }

    /// <summary>越界/超长的索引不能被回放，也不能把队首卡死（防远端脏数据）。</summary>
    [Fact]
    public async Task FireComponent_right_click_records_ignore_out_of_range_indexes()
    {
        var source = await AddToHand<Glasshug>();
        var other = await AddToHand<YalisalinDefend>();
        var component = source.GetOrCreateCapability<YalisalinFireComponentCapability>();

        var context = new YalisalinFireComponentContext(Player, NewCardPlay(source), component);
        context.AddChoiceOption(source);
        context.AddChoiceOption(other);
        context.AddRightClickRequest(new YalisalinFireRightClickRequest(
            YalisalinFireRightClickKind.PainKeeper, "test.painKeeper"));

        // 99 越界、-5 非法：都只推进队首，不产生任何效果，也不抛异常。
        context.ApplyRightClickRecords([99, -5]);

        Assert.Empty(context.AppliedRightClicks);
        Assert.Empty(context.PendingRightClicks);

        // 回放条数多于队列长度时多余的条目要被忽略掉。
        context.AddRightClickRequest(new YalisalinFireRightClickRequest(
            YalisalinFireRightClickKind.PainKeeper, "test.painKeeper"));
        context.ApplyRightClickRecords([1, 1, 1]);
        Assert.Single(context.AppliedRightClicks);
        Assert.Empty(context.PendingRightClicks);

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(other);
    }

    private static CardPlay NewCardPlay(CardModel card) => new()
    {
        Card = card,
        Player = card.Owner,
        Target = null,
        ResultPile = PileType.Discard,
        Resources = new ResourceInfo
        {
            EnergySpent = 0,
            EnergyValue = card.EnergyCost.GetAmountToSpend(),
            StarsSpent = 0,
            StarValue = Math.Max(0, card.GetStarCostWithModifiers())
        },
        IsAutoPlay = false,
        PlayIndex = 0,
        PlayCount = 1
    };

    /// <summary>剥掉富文本标记（<c>[color=#rrggbb]</c>、<c>[/color]</c>、<c>[b]</c> 等），只留纯文本。</summary>
    private static string StripTextMarkup(string text) =>
        Regex.Replace(text, @"\[/?[A-Za-z]+(=[^\]]*)?\]", string.Empty);
}