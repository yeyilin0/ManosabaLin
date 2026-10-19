using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Yalisalin.Cards;
using ManosabaLin.Characters.Yalisalin.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     传火的「点火」效果库。
///     <para>
///     打出【传火】时消耗当前王魂数 N，把 N 随机分拆成若干份（每份 1~3，所有分拆方案等权），
///     每一份从对应等级的效果库里随机摇出<b>一项</b>具体效果并立刻结算：
///     1 → <see cref="FireTide.Spark" />（火星），2 → <see cref="FireTide.Blaze" />（烈焰），3 → <see cref="FireTide.Inferno" />（燎原）。
///     </para>
///     <para>
///     分拆采样是「均匀抽样」而非枚举：f(n) 为 n 的「每份 1~3」组合数，
///     第一份取 p 的概率 = f(n-p)/f(n)，然后对剩余递归 —— 每种分拆方案概率严格相等
///     （N=3 时 [1,1,1] / [1,2] / [2,1] / [3] 各 25%）。每次只掷一次、不消耗王魂计数。
///     </para>
///     <para>
///     联机：分拆抽样与效果摇取都走 <c>Owner.RunState.Rng.CombatCardSelection</c>，
///     两端按同一执行顺序消耗同一串随机数，逐字节一致。
///     </para>
/// </summary>
internal static class LinkTheFireEffects
{
    /// <summary>点火等级：数值即消耗的王魂计数。</summary>
    internal enum FireTide
    {
        Spark = 1,
        Blaze = 2,
        Inferno = 3,
    }

    /// <summary>单次点火结算的上下文。</summary>
    internal sealed record FireContext(
        PlayerChoiceContext ChoiceContext,
        CardPlay CardPlay,
        LinkTheFire Card,
        Player Owner);

    private sealed record FireEffect(FireTide Tide, int Weight, string Name, Func<FireContext, Task> Apply);

    // ── 效果库 ────────────────────────────────────────────────────────────────
    // Weight 全部为 1（等权）；想调概率只改这里的权重（递归项想减半就给 1、其余 2 之类）。
    // Name 仅用于日志与代码可读性，不在游戏内显示（无战斗内文本通道）。

    private static readonly FireEffect[] Table =
    [
        // ═══ 等级 1 · 火星（20） ═══
        new(FireTide.Spark, 1, "笔记残页", static c => DrawCards(c, 1)),
        new(FireTide.Spark, 1, "门缝里的视线", static c => GainBlock(c, 8)),
        new(FireTide.Spark, 1, "侦探的直觉", DetectiveIntuition),
        new(FireTide.Spark, 1, "数到三就好了", static c => ApplyPower<DexterityPower>(c, c.Owner.Creature, 1)),
        new(FireTide.Spark, 1, "群聊的@全体", static c => DamageRandomEnemy(c, 8)),
        new(FireTide.Spark, 1, "伪证的余波", static c => ApplyPowerToRandomEnemy<WeakPower>(c, 1)),
        new(FireTide.Spark, 1, "借阅到期的书", BorrowedBookExpired),
        new(FireTide.Spark, 1, "蝴蝶挂饰的一振", static c => ApplyPower<StrengthPower>(c, c.Owner.Creature, 1)),
        new(FireTide.Spark, 1, "蜡烛的火苗", CandleFlame),
        new(FireTide.Spark, 1, "草莓牛奶", static c => Heal(c, 6)),
        new(FireTide.Spark, 1, "电梯里的镜子", static c => DamageRandomEnemy(c, 9)),
        new(FireTide.Spark, 1, "谁落在现场的润喉糖", LozengerAtScene),
        new(FireTide.Spark, 1, "似曾相识", static c => DiscardToHand(c, 1)),
        new(FireTide.Spark, 1, "口袋里的火柴盒", MatchboxInPocket),
        new(FireTide.Spark, 1, "没被采纳的线索", UnadoptedClue),
        new(FireTide.Spark, 1, "已读乱回", ReadNoReply),
        new(FireTide.Spark, 1, "放学后的试烧", AfterSchoolTrialBurn),
        new(FireTide.Spark, 1, "两份不一样的证词", TwoDifferentTestimonies),
        new(FireTide.Spark, 1, "信任的裂痕", static c => ApplyPowerToRandomEnemy<VulnerablePower>(c, 1)),
        new(FireTide.Spark, 1, "友方的一张卡", static c => FriendCardsToHand(c, 1, free: false)),

        // ═══ 等级 2 · 火势（20） ═══
        new(FireTide.Blaze, 1, "审判的预感", static c => DamageRandomEnemy(c, 14)),
        new(FireTide.Blaze, 1, "情绪的洪流", static c => DrawCards(c, 2)),
        new(FireTide.Blaze, 1, "怀疑的种子", static c => ApplyPower<StrengthPower>(c, c.Owner.Creature, 2)),
        new(FireTide.Blaze, 1, "梅露露的冰饮", static c => GainBlock(c, 14)),
        new(FireTide.Blaze, 1, "诺亚的声援", static c => Heal(c, 12)),
        new(FireTide.Blaze, 1, "未读消息99+", UnreadMessages99Plus),
        new(FireTide.Blaze, 1, "轮回的引渡", static c => HandToDrawThenDraw(c, 1, 2)),
        new(FireTide.Blaze, 1, "压迫性沉默", static c => ApplyPowerToAllEnemies<WeakPower>(c, 1)),
        new(FireTide.Blaze, 1, "樱花色的回想", static c => ApplyPower<WitchFactorEnchantPower>(c, c.Owner.Creature, 1)),
        new(FireTide.Blaze, 1, "熨平的刘海", static c => ApplyPower<IronedBangsPower>(c, c.Owner.Creature, IronedBangsPower.Reduction)),
        new(FireTide.Blaze, 1, "坏结局的草稿", static c => DamageRandomEnemy(c, 13)),
        new(FireTide.Blaze, 1, "群管理的禁言", static c => ApplyPowerToRandomEnemy<GroupAdminMutePower>(c, 1)),
        new(FireTide.Blaze, 1, "二次闷烧", static c => Trigger(c, FireTide.Spark, 2)),
        new(FireTide.Blaze, 1, "轮回的两巡", static c => HandToDrawThenDraw(c, 2, 3)),
        new(FireTide.Blaze, 1, "循环法典", static c => DiscardToDrawThenDraw(c, 1, 1)),
        new(FireTide.Blaze, 1, "罪疑蚀骨", SuspicionErodesBone),
        new(FireTide.Blaze, 1, "伪证的代价", static c => ApplyPowerToRandomEnemy<TempStrengthDown>(c, 2)),
        new(FireTide.Blaze, 1, "宣判回声", static c => DamageAllEnemies(c, 7)),
        new(FireTide.Blaze, 1, "信任的重量", WeightOfTrust),
        new(FireTide.Blaze, 1, "压线提交", DeadlineSubmit),

        // ═══ 等级 3 · 燎原（20） ═══
        new(FireTide.Inferno, 1, "审判日", static c => DamageAllEnemies(c, 20)),
        new(FireTide.Inferno, 1, "全员的名推理", MasterDeduction),
        new(FireTide.Inferno, 1, "灵魂满载", SoulFullyLoaded),
        new(FireTide.Inferno, 1, "羁绊的火焰", BondFlame),
        new(FireTide.Inferno, 1, "魔女因子的暴走", WitchFactorRampage),
        new(FireTide.Inferno, 1, "亲近的蔓延", static c => { GainEnergy(c, 2); return DrawCards(c, 1); }),
        new(FireTide.Inferno, 1, "疏远的审判", static c => ApplyPowerToAllEnemies<VulnerablePower>(c, 2)),
        new(FireTide.Inferno, 1, "正义执行", JusticeExecution),
        new(FireTide.Inferno, 1, "先古的火种", static c => { c.Card.Grow(2); return Task.CompletedTask; }),
        new(FireTide.Inferno, 1, "金卡的闪光", GoldenCardGlint),
        new(FireTide.Inferno, 1, "三次闷烧", static c => Trigger(c, FireTide.Blaze, 2)),
        new(FireTide.Inferno, 1, "轮回的闭环", SamsaraClosedLoop),
        new(FireTide.Inferno, 1, "永劫的轮回", static c => ApplyPower<EternalSamsaraPower>(c, c.Owner.Creature, 1)),
        new(FireTide.Inferno, 1, "全席宣判", static c => DamageAllEnemies(c, 15)),
        new(FireTide.Inferno, 1, "无限制魔法格斗", static c => ApplyPower<UnrestrictedMagicFightPower>(c, c.Owner.Creature, UnrestrictedMagicFightPower.Bonus)),
        new(FireTide.Inferno, 1, "一鼓作气", SurgeOfEnergy),
        new(FireTide.Inferno, 1, "死亡的彩排", DeathRehearsal),
        new(FireTide.Inferno, 1, "蓄力释放", ChargedRelease),
        new(FireTide.Inferno, 1, "吾有一友", static c => FriendCardsToHand(c, 2, free: true)),
        new(FireTide.Inferno, 1, "十三重罪罚", ThirteenFelonies),
    ];

    /// <summary>王魂计数分拆的上限：f(30)≈5.38e7 仍在 int 内，再大没有意义。</summary>
    private const int MaxFireCount = 30;

    // ── 入口 ─────────────────────────────────────────────────────────────────

    /// <summary>
    ///     消耗 <paramref name="total" /> 点王魂计数：均匀抽一种「每份 1~3」的分拆，逐份点火。
    ///     不消耗火种计数本身（成长只由效果库里的「灵魂满载 / 先古的火种 / 先古的审判」推动）。
    /// </summary>
    internal static async Task Ignite(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        LinkTheFire card,
        int total)
    {
        if (total <= 0)
            return;

        var context = new FireContext(choiceContext, cardPlay, card, card.Owner);
        foreach (var part in SampleComposition(card.Owner, Math.Min(total, MaxFireCount)))
            await Trigger(context, (FireTide)part);
    }

    /// <summary>从 <paramref name="tide" /> 效果库随机摇一项并结算；<paramref name="times" /> 次独立摇取。</summary>
    internal static async Task Trigger(FireContext context, FireTide tide, int times = 1)
    {
        var pool = Array.FindAll(Table, e => e.Tide == tide);
        if (pool.Length == 0)
            return;

        var totalWeight = 0;
        foreach (var effect in pool)
            totalWeight += effect.Weight;

        for (var i = 0; i < times; i++)
        {
            // 死亡彩排类效果可能把自己烧掉；战斗也可能在连锁中结束 —— 后续触发全部中止，
            // 否则会在已拆除的战斗上继续结算（Emamonv 同款 NRE 教训）。
            if (context.Owner.Creature.IsDead || CombatManager.Instance.IsOverOrEnding)
                return;

            var roll = context.Owner.RunState.Rng.CombatCardSelection.NextInt(0, totalWeight);
            var cumulative = 0;
            FireEffect? selected = null;
            foreach (var effect in pool)
            {
                cumulative += effect.Weight;
                if (cumulative > roll)
                {
                    selected = effect;
                    break;
                }
            }

            selected ??= pool[^1];
            await selected.Apply(context);
        }
    }

    // ── 分拆采样 ──────────────────────────────────────────────────────────────

    /// <summary>f(n)：n 拆成每份 1~3 的组合数。f(0)=1。</summary>
    private static int Compositions(int n)
    {
        if (n < 0)
            return 0;

        // f(0)=1, f(1)=1, f(2)=2, f(3)=4, f(n)=f(n-1)+f(n-2)+f(n-3)
        if (n <= 2)
            return n == 0 ? 1 : n;

        var a = 1; // f(0)
        var b = 1; // f(1)
        var c = 2; // f(2)
        for (var i = 3; i <= n; i++)
        {
            var next = a + b + c;
            (a, b, c) = (b, c, next);
        }

        return c;
    }

    /// <summary>均匀抽一种 total 的「每份 1~3」分拆（顺序敏感，[1,2] 与 [2,1] 算两种方案）。</summary>
    private static List<int> SampleComposition(Player owner, int total)
    {
        var rng = owner.RunState.Rng.CombatCardSelection;
        var parts = new List<int>();
        var remaining = total;
        while (remaining > 0)
        {
            var ways = Compositions(remaining);
            var roll = rng.NextInt(0, ways);
            var afterOne = Compositions(remaining - 1);
            var afterTwo = Compositions(remaining - 2);
            int part;
            if (roll < afterOne)
                part = 1;
            else if (roll < afterOne + afterTwo)
                part = 2;
            else
                part = 3;

            parts.Add(part);
            remaining -= part;
        }

        return parts;
    }

    // ── 通用工具 ──────────────────────────────────────────────────────────────

    private static async Task DrawCards(FireContext c, int count)
    {
        if (count <= 0)
            return;

        await CardPileCmd.Draw(c.ChoiceContext, count, c.Owner);
    }

    private static async Task GainBlock(FireContext c, decimal amount)
    {
        await CreatureCmd.GainBlock(c.Owner.Creature, amount, ValueProp.Move, c.CardPlay);
    }

    private static async Task Heal(FireContext c, decimal amount)
    {
        if (c.Owner.Creature.IsDead)
            return;

        await CreatureCmd.Heal(c.Owner.Creature, amount);
    }

    private static void GainEnergy(FireContext c, int amount)
    {
        if (amount == 0)
            return;

        if (amount > 0)
            PlayerCmd.GainEnergy(amount, c.Owner);
        else
            PlayerCmd.LoseEnergy(-amount, c.Owner);
    }

    private static async Task ApplyPower<TPower>(FireContext c, Creature target, decimal amount)
        where TPower : PowerModel
    {
        if (target == null || target.IsDead)
            return;

        await PowerCmd.Apply<TPower>(c.ChoiceContext, target, amount, c.Owner.Creature, c.Card);
    }

    private static async Task ApplyPowerToAllEnemies<TPower>(FireContext c, decimal amount)
        where TPower : PowerModel
    {
        var combatState = c.Owner.Creature.CombatState;
        if (combatState == null)
            return;

        foreach (var enemy in combatState.HittableEnemies)
            await ApplyPower<TPower>(c, enemy, amount);
    }

    private static async Task ApplyPowerToRandomEnemy<TPower>(FireContext c, decimal amount)
        where TPower : PowerModel
    {
        if (RandomEnemy(c.Owner) is not { } enemy)
            return;

        await ApplyPower<TPower>(c, enemy, amount);
    }

    private static Creature? RandomEnemy(Player owner)
    {
        var combatState = owner.Creature.CombatState;
        if (combatState == null)
            return null;

        return owner.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
    }

    private static async Task DamageEnemy(FireContext c, Creature target, decimal amount)
    {
        await DamageCmd.Attack(amount)
            .FromCard(c.Card, c.CardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(c.ChoiceContext);
    }

    private static async Task DamageRandomEnemy(FireContext c, decimal amount)
    {
        if (RandomEnemy(c.Owner) is not { } enemy)
            return;

        await DamageEnemy(c, enemy, amount);
    }

    private static async Task DamageAllEnemies(FireContext c, decimal amount)
    {
        var combatState = c.Owner.Creature.CombatState;
        if (combatState == null || !combatState.HittableEnemies.Any())
            return;

        await DamageCmd.Attack(amount)
            .FromCard(c.Card, c.CardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(c.ChoiceContext);
    }

    private static async Task LoseSelfHp(FireContext c, decimal amount)
    {
        if (amount <= 0 || c.Owner.Creature.IsDead)
            return;

        await CreatureCmd.Damage(
            c.ChoiceContext,
            c.Owner.Creature,
            amount,
            ValueProp.Unblockable | ValueProp.Unpowered,
            c.Card,
            null);
    }

    private static void MakeFreeThisTurn(CardModel card)
    {
        card.EnergyCost.AddThisTurnOrUntilPlayed(-999, reduceOnly: true);
    }

    private static IReadOnlyList<CardModel> HandCards(Player owner)
    {
        return PileType.Hand.GetPile(owner).Cards;
    }

    private static CardModel? TopOfDrawPile(Player owner)
    {
        // CardPile 里 index 0 是堆顶（InvestigationPower 同款口径）。
        return PileType.Draw.GetPile(owner).Cards.FirstOrDefault();
    }

    private static CardModel? RandomCardOf(IReadOnlyList<CardModel> cards, Player owner)
    {
        if (cards.Count == 0)
            return null;

        return owner.RunState.Rng.CombatCardSelection.NextItem(cards);
    }

    // ── 友方角色牌 / 金卡 ─────────────────────────────────────────────────────

    /// <summary>
    ///     友方角色牌 = 其他角色的角色卡池里的牌（排除无色、Token、 Ancient/Basic/Event，且可在战斗中生成）。
    ///     口径同「高塔」的额外连接牌（<see cref="Powers.YalisalinTowerPower" />）。
    /// </summary>
    private static CardModel? CreateFriendCard(Player owner)
    {
        if (owner.Creature.CombatState is not { } combatState)
            return null;

        var candidates = owner.UnlockState.CharacterCardPools
            .Where(pool => pool != owner.Character.CardPool)
            .SelectMany(pool => pool.GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint))
            .Where(static card => card.Rarity != CardRarity.Ancient
                                  && card.Rarity != CardRarity.Basic
                                  && card.Rarity != CardRarity.Event
                                  && card.CanBeGeneratedInCombat)
            .ToList();

        var template = RandomCardOf(candidates, owner);
        return template == null ? null : combatState.CreateCard(template, owner);
    }

    private static bool HasFriendCardInHand(Player owner)
    {
        var friendPools = owner.UnlockState.CharacterCardPools
            .Where(pool => pool != owner.Character.CardPool)
            .ToHashSet();

        return HandCards(owner).Any(card => card.Pool != null && friendPools.Contains(card.Pool));
    }

    // ── 选择键 ────────────────────────────────────────────────────────────────

    private static class SelectKeys
    {
        public const string Clue = "MANOSABA_LIN_CARD_LINK_THE_FIRE_EFFECT_SELECT_CLUE";
        public const string Testimony = "MANOSABA_LIN_CARD_LINK_THE_FIRE_EFFECT_SELECT_TESTIMONY";
        public const string Charge = "MANOSABA_LIN_CARD_LINK_THE_FIRE_EFFECT_SELECT_CHARGE";
    }

    private static async Task<CardModel?> SelectFromHand(FireContext c, string promptKey)
    {
        if (HandCards(c.Owner).Count == 0)
            return null;

        var prefs = new CardSelectorPrefs(new LocString("cards", promptKey), 1, 1);
        var selected = await CardSelectCmd.FromHand(c.ChoiceContext, c.Owner, prefs, null, c.Card);
        return selected.FirstOrDefault();
    }

    // ── 等级 1 效果 ───────────────────────────────────────────────────────────

    /// <summary>侦探的直觉：揭示抽牌堆顶 1 张，是攻击牌则抽它。</summary>
    private static async Task DetectiveIntuition(FireContext c)
    {
        if (TopOfDrawPile(c.Owner) is not { } top)
            return;

        if (top.Type == CardType.Attack)
            await CardPileCmd.Draw(c.ChoiceContext, 1, c.Owner);
    }

    /// <summary>借阅到期的书：抽牌堆顶 1 张本回合费用 -1。</summary>
    private static Task BorrowedBookExpired(FireContext c)
    {
        if (TopOfDrawPile(c.Owner) is not { } top)
            return Task.CompletedTask;

        top.EnergyCost.AddThisTurnOrUntilPlayed(-1, reduceOnly: true);
        return Task.CompletedTask;
    }

    /// <summary>蜡烛的火苗：造成 6 点伤害并获得 4 点格挡。</summary>
    private static async Task CandleFlame(FireContext c)
    {
        await DamageRandomEnemy(c, 6);
        await GainBlock(c, 4);
    }

    /// <summary>谁落在现场的润喉糖：获得 5 点格挡并回复 3 点生命。</summary>
    private static async Task LozengerAtScene(FireContext c)
    {
        await GainBlock(c, 5);
        await Heal(c, 3);
    }

    /// <summary>似曾相识：将 1 张随机弃牌置入手牌。</summary>
    private static async Task DiscardToHand(FireContext c, int count)
    {
        for (var i = 0; i < count; i++)
        {
            // 每次都重新取：牌堆是活集合，移走一张后旧引用可能已失效。
            if (RandomCardOf(PileType.Discard.GetPile(c.Owner).Cards, c.Owner) is not { } card)
                return;

            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top);
        }
    }

    /// <summary>口袋里的火柴盒：造成 4 点伤害，若击杀则抽 1 张牌。</summary>
    private static async Task MatchboxInPocket(FireContext c)
    {
        if (RandomEnemy(c.Owner) is not { } enemy)
            return;

        await DamageEnemy(c, enemy, 4);

        if (!enemy.IsAlive)
            await CardPileCmd.Draw(c.ChoiceContext, 1, c.Owner);
    }

    /// <summary>没被采纳的线索：揭示抽牌堆顶 2 张，将其中 1 张置入手牌。</summary>
    private static async Task UnadoptedClue(FireContext c)
    {
        var top = PileType.Draw.GetPile(c.Owner).Cards.Take(2).ToList();
        if (top.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(new LocString("cards", SelectKeys.Clue), 1, 1);
        var selected = await CardSelectCmd.FromSimpleGrid(c.ChoiceContext, top, c.Owner, prefs);
        if (selected.FirstOrDefault() is not { } chosen)
            return;

        await CardPileCmd.Add(chosen, PileType.Hand, CardPilePosition.Top);
    }

    /// <summary>已读乱回：抽 1 张牌，然后弃掉 1 张随机手牌。</summary>
    private static async Task ReadNoReply(FireContext c)
    {
        await CardPileCmd.Draw(c.ChoiceContext, 1, c.Owner);

        if (RandomCardOf(HandCards(c.Owner), c.Owner) is not { } card)
            return;

        await CardCmd.Discard(c.ChoiceContext, card);
    }

    /// <summary>放学后的试烧：造成 6 点伤害；若为本回合第一张牌则改为 9 点。</summary>
    private static async Task AfterSchoolTrialBurn(FireContext c)
    {
        var playedThisTurn = CombatManager.Instance.History.CardPlaysFinished
            .Any(entry => entry.HappenedThisTurn(c.Owner.Creature.CombatState)
                          && entry.CardPlay.Player == c.Owner);

        await DamageRandomEnemy(c, playedThisTurn ? 6 : 9);
    }

    /// <summary>两份不一样的证词：弃 1 张手牌，抽 2 张牌。</summary>
    private static async Task TwoDifferentTestimonies(FireContext c)
    {
        if (await SelectFromHand(c, SelectKeys.Testimony) is not { } chosen)
            return;

        await CardCmd.Discard(c.ChoiceContext, chosen);
        await CardPileCmd.Draw(c.ChoiceContext, 2, c.Owner);
    }

    // ── 等级 2 效果 ───────────────────────────────────────────────────────────

    /// <summary>未读消息99+：抽牌堆随机 1 张本回合费用归零。</summary>
    private static Task UnreadMessages99Plus(FireContext c)
    {
        if (RandomCardOf(PileType.Draw.GetPile(c.Owner).Cards, c.Owner) is not { } card)
            return Task.CompletedTask;

        MakeFreeThisTurn(card);
        return Task.CompletedTask;
    }

    /// <summary>轮回的引渡 / 轮回的两巡：N 张随机手牌洗回抽牌堆并抽 M 张。</summary>
    private static async Task HandToDrawThenDraw(FireContext c, int handCount, int drawCount)
    {
        for (var i = 0; i < handCount; i++)
        {
            if (RandomCardOf(HandCards(c.Owner), c.Owner) is not { } card)
                break;

            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Random);
        }

        await CardPileCmd.Draw(c.ChoiceContext, drawCount, c.Owner);
    }

    /// <summary>循环法典：将 1 张随机弃牌洗回抽牌堆，然后抽 1 张牌。</summary>
    private static async Task DiscardToDrawThenDraw(FireContext c, int discardCount, int drawCount)
    {
        for (var i = 0; i < discardCount; i++)
        {
            if (RandomCardOf(PileType.Discard.GetPile(c.Owner).Cards, c.Owner) is not { } card)
                break;

            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Random);
        }

        await CardPileCmd.Draw(c.ChoiceContext, drawCount, c.Owner);
    }

    /// <summary>罪疑蚀骨：对随机敌人造成 10 点伤害，其每有 1 层负面本伤害 +2。</summary>
    private static async Task SuspicionErodesBone(FireContext c)
    {
        if (RandomEnemy(c.Owner) is not { } enemy)
            return;

        var debuffStacks = enemy.Powers
            .Where(static power => power.Type == PowerType.Debuff)
            .Sum(static power => Math.Max(0, power.Amount));

        await DamageEnemy(c, enemy, 10 + 2 * debuffStacks);
    }

    /// <summary>信任的重量：获得 10 点格挡；手牌中有友方角色牌则改为 14 点。</summary>
    private static async Task WeightOfTrust(FireContext c)
    {
        await GainBlock(c, HasFriendCardInHand(c.Owner) ? 14 : 10);
    }

    /// <summary>压线提交：本回合 +1⚡，下回合 -1⚡。</summary>
    private static async Task DeadlineSubmit(FireContext c)
    {
        GainEnergy(c, 1);
        await ApplyPower<LoseEnergyPower>(c, c.Owner.Creature, 1);
    }

    // ── 等级 3 效果 ───────────────────────────────────────────────────────────

    /// <summary>全员的名推理：抽 3 张牌，本回合能量 +1。</summary>
    private static async Task MasterDeduction(FireContext c)
    {
        await CardPileCmd.Draw(c.ChoiceContext, 3, c.Owner);
        GainEnergy(c, 1);
    }

    /// <summary>灵魂满载：本牌成长 1 次并抽 2 张牌。</summary>
    private static async Task SoulFullyLoaded(FireContext c)
    {
        c.Card.Grow(1);
        await CardPileCmd.Draw(c.ChoiceContext, 2, c.Owner);
    }

    /// <summary>羁绊的火焰：获得 3 点力量与 16 点格挡。</summary>
    private static async Task BondFlame(FireContext c)
    {
        await ApplyPower<StrengthPower>(c, c.Owner.Creature, 3);
        await GainBlock(c, 16);
    }

    /// <summary>魔女因子的暴走：全体 10 点伤害并随机施加 2 层负面。</summary>
    private static async Task WitchFactorRampage(FireContext c)
    {
        await DamageAllEnemies(c, 10);

        for (var i = 0; i < 2; i++)
        {
            if (RandomEnemy(c.Owner) is not { } enemy)
                return;

            var pick = c.Owner.RunState.Rng.CombatTargets.NextInt(0, 3);
            switch (pick)
            {
                case 0:
                    await ApplyPower<WeakPower>(c, enemy, 1);
                    break;
                case 1:
                    await ApplyPower<VulnerablePower>(c, enemy, 1);
                    break;
                default:
                    await ApplyPower<FrailPower>(c, enemy, 1);
                    break;
            }
        }
    }

    /// <summary>正义执行：全体 12 点伤害，每层【嫌疑】额外 +3。</summary>
    private static async Task JusticeExecution(FireContext c)
    {
        var combatState = c.Owner.Creature.CombatState;
        if (combatState == null)
            return;

        foreach (var enemy in combatState.HittableEnemies)
        {
            var suspect = enemy.GetPower<SuspectPower>()?.Amount ?? 0m;
            await DamageEnemy(c, enemy, 12 + 3 * suspect);
        }
    }

    /// <summary>金卡的闪光：随机 3 张手牌本回合免费。</summary>
    private static Task GoldenCardGlint(FireContext c)
    {
        var hand = HandCards(c.Owner);
        for (var i = 0; i < 3 && hand.Count > 0; i++)
        {
            if (RandomCardOf(hand, c.Owner) is not { } card)
                break;

            MakeFreeThisTurn(card);
        }

        return Task.CompletedTask;
    }

    /// <summary>轮回的闭环：手牌全部洗回抽牌堆，然后抽 3 张牌。</summary>
    private static async Task SamsaraClosedLoop(FireContext c)
    {
        foreach (var card in HandCards(c.Owner).ToList())
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Random);

        await CardPileCmd.Draw(c.ChoiceContext, 3, c.Owner);
    }

    /// <summary>一鼓作气：获得 2 点力量，本回合结束时失去 1 点（永久 +1 + 临时 +1）。</summary>
    private static async Task SurgeOfEnergy(FireContext c)
    {
        await ApplyPower<StrengthPower>(c, c.Owner.Creature, 1);
        await ApplyPower<TempStrength>(c, c.Owner.Creature, 1);
    }

    /// <summary>死亡的彩排：失去 5 点生命，获得 2 点力量与 10 点格挡。</summary>
    private static async Task DeathRehearsal(FireContext c)
    {
        await LoseSelfHp(c, 5);
        await ApplyPower<StrengthPower>(c, c.Owner.Creature, 2);
        await GainBlock(c, 10);
    }

    /// <summary>蓄力释放：弃 1 张手牌，对随机敌人造成 18 点伤害。</summary>
    private static async Task ChargedRelease(FireContext c)
    {
        if (await SelectFromHand(c, SelectKeys.Charge) is not { } chosen)
            return;

        await CardCmd.Discard(c.ChoiceContext, chosen);
        await DamageRandomEnemy(c, 18);
    }

    /// <summary>十三重罪罚：对所有敌人施加 2 层虚弱与 2 层易伤。</summary>
    private static async Task ThirteenFelonies(FireContext c)
    {
        await ApplyPowerToAllEnemies<WeakPower>(c, 2);
        await ApplyPowerToAllEnemies<VulnerablePower>(c, 2);
    }

    // ── 友方角色牌入手 ────────────────────────────────────────────────────────

    private static async Task FriendCardsToHand(FireContext c, int count, bool free)
    {
        for (var i = 0; i < count; i++)
        {
            if (CreateFriendCard(c.Owner) is not { } card)
                return;

            if (free)
                MakeFreeThisTurn(card);

            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, c.Owner, CardPilePosition.Bottom);
        }
    }
}
