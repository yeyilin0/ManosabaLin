using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Yalisalin.Capabilities;
using ManosabaLin.Characters.Yalisalin.Cards;
using ManosabaLin.Characters.Yalisalin.Components;
using ManosabaLin.Characters.Yalisalin.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Yalisalin.Relics;

/// <summary>
///     亚里沙的发夹：承载火色量表与余火组件的各种强化。
///
///     火色量表（每名敌人各一条，由每个亚里沙玩家各自独立记录）：
///     - 共 <see cref="MaxSegments" /> 格，颜色由格位决定：1-2 浅橙、3-4 亮黄、5-6 赤红。
///     - 卡牌效果从最低的空格往上给予；量表恒为 1..n 的连续前缀，因此只需记录已填格数。
///     - 用攻击牌对敌人造成伤害后，从最新（最高）的一格开始消耗，并触发该格颜色的消耗效果。
///     - 连续两次消耗同色火色触发该色的「连续」奖励（两两成对，第三次同色重新起算）。
/// </summary>
[RegisterRelic(typeof(YalisalinRelicPool))]
[RegisterCharacterStarterRelic(typeof(Yalisalin))]
public sealed class YalisalinsHairpin : ManosabaRelicTemplate, IYalisalinFireComponentModifier
{
    public const string LocalizationEntry = "MANOSABA_LIN_RELIC_YALISALINS_HAIRPIN";
    public const int MaxSegments = 6;
    private const int OrangeConsumeBlock = 4;
    private const int RedConsumeBaseDamage = 3;
    private const int TicketEnergyThreshold = 5;

    private readonly Dictionary<Creature, YalisalinFireColorGauge> _gauges = [];
    // 「请别看见我」待触发队列：每项 = 该次打出时是否已升级（烧牌时额外给 6 格挡）。
    // 2026-10-18 用户改 zhs：「本回合下一次余火烧掉牌时，抽1张牌」——原为「被选中的那张牌被烧掉」，
    // 故不再按牌索引，改为「本回合下一次烧牌（任意牌）消耗一项」的 FIFO 队列，回合末清空。
    private readonly List<bool> _dontLookAtMePending = [];
    private readonly List<(CardModel Card, int Block)> _glassReturnCards = [];
    private readonly Dictionary<CardModel, BringHomePendingCard> _bringHomeCards = [];
    private readonly List<CardModel> _burnedCardsThisTurn = [];
    private readonly List<YalisalinFireColor> _consumptionLog = [];
    private readonly YalisalinFireColorChain _chain = new();

    // 火色效果（赤红伤害等）本身也是卡牌来源的伤害；结算期间不再触发「攻击后消耗火色」，否则会一路连锁烧穿量表。
    private int _fireEffectDepth;

    [SavedProperty] public int PainKeeperPerTurnLimit { get; private set; }
    [SavedProperty] public int PainKeeperUsedThisTurn { get; private set; }
    [SavedProperty] public int UnneededGoodChildPendingCount { get; private set; }
    [SavedProperty] public int UnneededGoodChildPendingEnergy { get; private set; }
    [SavedProperty] public bool SeparatedEndsEnabled { get; private set; }
    [SavedProperty] public int SeparatedEndsBlock { get; private set; }
    [SavedProperty] public int SeparatedEndsDraw { get; private set; }
    [SavedProperty] public int FireComponentBurnsThisTurn { get; private set; }
    [SavedProperty] public int ManualFireComponentsCompletedThisCombat { get; private set; }
    [SavedProperty] public int PendingBringHomeEnergy { get; private set; }
    [SavedProperty] public int PendingBringHomeDraw { get; private set; }
    [SavedProperty] public int YellowNextTurnEnergyGrantedThisTurn { get; private set; }
    [SavedProperty] public int PendingYellowCostReduction { get; private set; }
    [SavedProperty] public int RedConsumeDamage { get; private set; }

    /// <summary>【夺火】计数：每场战斗第 1 次打出夺火时 +1，整局游戏持续成长（存于发夹这个初始遗物）。</summary>
    [SavedProperty] public int UsurpTheFlameCount { get; private set; }

    /// <summary>本场战斗是否已经给夺火计过数，防止同一场多次打出重复 +1。</summary>
    private bool _usurpCountGainedThisCombat;

    /// <summary>待发放的夺火「额外回合」数（强化后的夺火打出时 +1，回合结束时按此数发放）。</summary>
    [SavedProperty] public int PendingUsurpExtraTurns { get; private set; }

    /// <summary>「不要冷却」：每回合开始时随机给予敌人的火色格数（多张叠加）。</summary>
    [SavedProperty] public int TurnStartFireGift { get; private set; }

    /// <summary>「窗上的车票」：每次给予火色额外多给的格数（多张叠加）。</summary>
    [SavedProperty] public int TicketStacks { get; private set; }

    [SavedProperty] public int FireGivenThisTurn { get; private set; }
    [SavedProperty] public bool TicketEnergyGrantedThisTurn { get; private set; }

    /// <summary>「同样错误的问题」：下一张技能牌费用变为 0 的待用次数。</summary>
    [SavedProperty] public int PendingFreeSkillCount { get; private set; }

    [SavedProperty] public int ContinuousTriggersThisCombat { get; private set; }

    // —— 原罪体系战斗计数（罪业圣盾 / 嫉恨反噬 共用同一个过失计数器）——
    private bool _sinHookActive;
    [SavedProperty] public int SinForgiveThisCombat { get; private set; }
    [SavedProperty] public int SinPunishThisCombat { get; private set; }
    [SavedProperty] public int SinForgiveThisTurn { get; private set; }
    [SavedProperty] public int SinPunishThisTurn { get; private set; }
    [SavedProperty] public int SinForgiveLastTurn { get; private set; }
    [SavedProperty] public int SinPunishLastTurn { get; private set; }

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => false;
    public override int DisplayAmount => 0;

    /// <summary>
    ///     在「连续」奖励之外，每次触发连续时额外再结算几次连续奖励。
    ///     只在「截稿前的交接」结算期间被临时抬高，不跨卡牌保留。
    /// </summary>
    public int ExtraContinuousTriggers { get; set; }

    /// <summary>本场战斗中被实际消耗（含超出格数的补结算）的火色，按发生顺序。卡牌据此判断「这次打出消耗了什么」。</summary>
    public IReadOnlyList<YalisalinFireColor> ConsumptionLog => _consumptionLog;

    /// <summary>
    ///     【坏结局】毒化：所有牌里带着「紫藤亚里沙的坏结局」时，火色无法触发消耗效果。
    ///     格位照常被消耗、消耗记录照常写入，只是不结算该色的基础奖励与「连续」奖励。
    /// </summary>
    public bool IsFireColorConsumeSuppressed => YalisalinBadEnding.IsHeldBy(Owner);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return new HoverTip(
                new LocString("relics", $"{Id.Entry}.fireColor.title"),
                new LocString("relics", $"{Id.Entry}.fireColor.description"));

            yield return YalisalinFireComponentCapability.CreateHoverTip(Owner);
        }
    }

    /// <summary>默认 6 格时的格位颜色（1-2 浅橙、3-4 亮黄、5-6 赤红）。</summary>
    public static YalisalinFireColor SlotColor(int slot)
    {
        return SlotColor(slot, MaxSegments);
    }

    /// <summary>
    ///     按给定格数把三色均分成三段：每段 <c>maxSegments / 3</c> 格。
    ///     6 格 ⇒ 1-2 / 3-4 / 5-6；12 格（「高塔」改造后）⇒ 1-4 / 5-8 / 9-12。
    /// </summary>
    public static YalisalinFireColor SlotColor(int slot, int maxSegments)
    {
        var band = Math.Max(1, maxSegments / 3);
        return slot switch
        {
            _ when slot <= band => YalisalinFireColor.LightOrange,
            _ when slot <= band * 2 => YalisalinFireColor.BrightYellow,
            _ => YalisalinFireColor.Red
        };
    }

    /// <summary>
    ///     当前生效的火色格上限：被「高塔」改造后为 12 格，否则为默认 <see cref="MaxSegments" />。
    /// </summary>
    public int CurrentMaxSegments =>
        Owner?.Creature?.GetPower<YalisalinTowerPower>()?.FireColorSegments ?? MaxSegments;

    /// <summary>按当前生效的火色格上限取格位颜色。</summary>
    public YalisalinFireColor SlotColorFor(int slot)
    {
        return SlotColor(slot, CurrentMaxSegments);
    }

    public static IEnumerable<string> GetFireComponentEnhancementDescriptions(Player owner)
    {
        return YalisalinFireColorSystem.TryGetHairpin(owner, out var hairpin)
            ? hairpin.GetFireComponentEnhancementDescriptions()
            : [];
    }

    public void EnablePainKeeper(int perTurnLimit)
    {
        PainKeeperPerTurnLimit += Math.Max(0, perTurnLimit);
        Flash();
    }

    public void EnableSeparatedEnds(int block, int draw)
    {
        SeparatedEndsEnabled = true;
        SeparatedEndsBlock = Math.Max(SeparatedEndsBlock, block);
        SeparatedEndsDraw = Math.Max(SeparatedEndsDraw, draw);
        Flash();
    }

    public void AddTurnStartFireGift(int amount)
    {
        if (amount <= 0)
            return;

        TurnStartFireGift += amount;
        Flash();
    }

    public void AddTicketStack()
    {
        TicketStacks++;
        Flash();
    }

    public void QueueFreeSkill()
    {
        PendingFreeSkillCount++;
        Flash();
    }

    /// <summary>
    ///     本回合被余火烧掉、且仍然可以返回手牌的牌（按烧掉顺序，已去重）。
    ///     供「第二次点燃」随机取回使用；已经被取回（不在消耗堆）的牌不再算候选。
    /// </summary>
    public CardModel[] BurnedCardsThisTurn =>
        _burnedCardsThisTurn
            .Where(static card => !card.HasBeenRemovedFromState && card.Pile?.Type == PileType.Exhaust)
            .ToArray();

    public int GetFireColorCount(Creature target)
    {
        return _gauges.TryGetValue(target, out var gauge) ? gauge.Filled : 0;
    }

    public bool TargetHasFireColor(Creature target)
    {
        return GetFireColorCount(target) > 0;
    }

    public bool IsFireColorFull(Creature target)
    {
        return GetFireColorCount(target) >= CurrentMaxSegments;
    }

    public void QueueUnneededGoodChild(int energyGain)
    {
        UnneededGoodChildPendingCount++;
        UnneededGoodChildPendingEnergy = Math.Max(UnneededGoodChildPendingEnergy, energyGain);
        Flash();
    }

    public void TrackDontLookAtMe(bool gainBlock)
    {
        _dontLookAtMePending.Add(gainBlock);
    }

    public void QueueGlassReturn(CardModel card, int block)
    {
        if (_glassReturnCards.Any(entry => entry.Card == card))
            return;

        _glassReturnCards.Add((card, block));
    }

    /// <summary>登记一张「下回合返回手牌」的牌（不给格挡）。</summary>
    public void QueueHandReturn(CardModel card)
    {
        QueueGlassReturn(card, 0);
    }

    public void TrackBringHome(CardModel card, int energy, int draw)
    {
        _bringHomeCards[card] = new BringHomePendingCard(energy, draw);
    }

    private IEnumerable<string> GetFireComponentEnhancementDescriptions()
    {
        if (Owner.Creature.CombatState != null
            && YalisalinFireComponentRules.AllCombatCards(Owner).Count() < 13)
            yield return YalisalinFireComponentContext.Text("enhancement.absentThirteenth");

        if (PainKeeperPerTurnLimit > 0)
            yield return YalisalinFireComponentContext.Text(
                "enhancement.painKeeper.count",
                ("Count", PainKeeperPerTurnLimit));

        if (Owner.Creature.GetPower<DazzlingTolerancePower>() is { Amount: > 0 } dazzling)
            yield return YalisalinFireComponentContext.Text(
                "enhancement.dazzlingTolerance",
                ("Damage", dazzling.Amount),
                ("Block", dazzling.Amount * 2));

        if (Owner.Creature.GetPower<BurnedApologyPower>() != null)
            yield return YalisalinFireComponentContext.Text("enhancement.burnedApology");

        if (SeparatedEndsEnabled)
            yield return YalisalinFireComponentContext.Text(
                "enhancement.separatedEnds",
                ("Block", SeparatedEndsBlock),
                ("Cards", SeparatedEndsDraw));

        if (Owner.Creature.GetPower<WarmthShouldNotStayPower>() != null)
            yield return YalisalinFireComponentContext.Text("enhancement.warmthShouldNotStay");

        if (UnneededGoodChildPendingCount > 0)
            yield return YalisalinFireComponentContext.Text(
                "enhancement.unneededGoodChild",
                ("Damage", UnneededGoodChildPendingCount),
                ("Energy", UnneededGoodChildPendingEnergy));
    }

    public override Task BeforeCombatStart()
    {
        HookSinEvents();
        ResetCombatState();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        UnhookSinEvents();
        ResetCombatState();
        return Task.CompletedTask;
    }

    private void ResetCombatState()
    {
        _gauges.Clear();
        _dontLookAtMePending.Clear();
        _glassReturnCards.Clear();
        _bringHomeCards.Clear();
        _burnedCardsThisTurn.Clear();
        _consumptionLog.Clear();
        _chain.Reset();
        _fireEffectDepth = 0;
        ExtraContinuousTriggers = 0;
        SinForgiveThisCombat = 0;
        SinPunishThisCombat = 0;
        SinForgiveThisTurn = 0;
        SinPunishThisTurn = 0;
        SinForgiveLastTurn = 0;
        SinPunishLastTurn = 0;
        PainKeeperPerTurnLimit = 0;
        PainKeeperUsedThisTurn = 0;
        UnneededGoodChildPendingCount = 0;
        UnneededGoodChildPendingEnergy = 0;
        SeparatedEndsEnabled = false;
        SeparatedEndsBlock = 0;
        SeparatedEndsDraw = 0;
        FireComponentBurnsThisTurn = 0;
        ManualFireComponentsCompletedThisCombat = 0;
        PendingBringHomeEnergy = 0;
        PendingBringHomeDraw = 0;
        YellowNextTurnEnergyGrantedThisTurn = 0;
        PendingYellowCostReduction = 0;
        RedConsumeDamage = RedConsumeBaseDamage;
        TurnStartFireGift = 0;
        TicketStacks = 0;
        FireGivenThisTurn = 0;
        TicketEnergyGrantedThisTurn = false;
        PendingFreeSkillCount = 0;
        ContinuousTriggersThisCombat = 0;
        _usurpCountGainedThisCombat = false;
        PendingUsurpExtraTurns = 0;
    }

    /// <summary>
    ///     【夺火】计数：每场战斗第 1 次打出夺火时调用，保证每场只 +1 一次。
    ///     由 <c>UsurpTheFlame</c> 在打出时调用；<see cref="_usurpCountGainedThisCombat" /> 在
    ///     <see cref="ResetCombatState" /> 里于每场战斗开始时清零。
    /// </summary>
    public void GainUsurpTheFlameCountOnce()
    {
        if (_usurpCountGainedThisCombat)
            return;
        _usurpCountGainedThisCombat = true;
        UsurpTheFlameCount++;
    }

    /// <summary>
    ///     【夺火】右键「强化」时扣掉计数（不足则返回 false，不改动）。
    ///     由 <c>UsurpTheFlame.OnRightClick</c> 调用。
    /// </summary>
    public bool TrySpendUsurpTheFlameCount(int amount)
    {
        if (amount <= 0 || UsurpTheFlameCount < amount)
            return false;
        UsurpTheFlameCount -= amount;
        return true;
    }

    /// <summary>
    ///     强化后的夺火打出时调用：记下 1 个待发放的「额外回合」。
    ///     实际发放由覆写的原版钩子 <see cref="ShouldTakeExtraTurn" /> 在回合结束时完成。
    /// </summary>
    public void QueueUsurpExtraTurn()
    {
        PendingUsurpExtraTurns++;
    }

    /// <summary>
    ///     ⭐ 原版额外回合钩子（<c>AbstractModel</c> 虚方法，与「佩尔之眼」PaelsEye 同款）——
    ///     有排队中的夺火额外回合时，让持有者再走一个回合。**不是 Harmony patch**。
    /// </summary>
    public override bool ShouldTakeExtraTurn(Player player)
    {
        return PendingUsurpExtraTurns > 0 && player == Owner;
    }

    /// <summary>额外回合走完之后消耗掉排队数。</summary>
    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player != Owner)
            return Task.CompletedTask;

        if (PendingUsurpExtraTurns > 0)
            PendingUsurpExtraTurns--;
        Flash();
        return Task.CompletedTask;
    }

    private void HookSinEvents()
    {
        if (_sinHookActive) return;
        Originalsin.ForgiveTriggered += OnSinForgive;
        Originalsin.PunishTriggered += OnSinPunish;
        _sinHookActive = true;
    }

    private void UnhookSinEvents()
    {
        if (!_sinHookActive) return;
        Originalsin.ForgiveTriggered -= OnSinForgive;
        Originalsin.PunishTriggered -= OnSinPunish;
        _sinHookActive = false;
    }

    private async void OnSinForgive(PlayerChoiceContext choiceContext, CardModel forgiven)
    {
        try
        {
            if (forgiven.Owner?.Creature != Owner.Creature) return;
            SinForgiveThisTurn++;
            SinForgiveThisCombat++;
            Flash();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Log.Error($"YalisalinsHairpin.OnSinForgive failed: {ex}");
        }
    }

    private async void OnSinPunish(PlayerChoiceContext choiceContext, CardModel punished)
    {
        try
        {
            if (punished.Owner?.Creature != Owner.Creature) return;
            SinPunishThisTurn++;
            SinPunishThisCombat++;
            Flash();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Log.Error($"YalisalinsHairpin.OnSinPunish failed: {ex}");
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return;

        PainKeeperUsedThisTurn = 0;
        FireComponentBurnsThisTurn = 0;
        _burnedCardsThisTurn.Clear();
        YellowNextTurnEnergyGrantedThisTurn = 0;
        PendingYellowCostReduction = 0;
        FireGivenThisTurn = 0;
        TicketEnergyGrantedThisTurn = false;
        _chain.Reset();
        foreach (var gauge in _gauges.Values)
        {
            gauge.MarkTurnStart();
            gauge.ResetOverflowTracking();
        }

        // 新回合开始：把已结束回合的宽恕/自惩数转移到“上一回合”
        SinForgiveLastTurn = SinForgiveThisTurn;
        SinPunishLastTurn = SinPunishThisTurn;
        SinForgiveThisTurn = 0;
        SinPunishThisTurn = 0;

        foreach (var (card, block) in _glassReturnCards.ToArray())
        {
            if (!card.HasBeenRemovedFromState)
            {
                await CardPileCmd.Add(card, PileType.Hand);
                card.EnergyCost.SetThisTurnOrUntilPlayed(0, reduceOnly: true);
                if (block > 0)
                    await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay: null);
            }

            _glassReturnCards.Remove((card, block));
        }

        if (PendingBringHomeEnergy > 0)
            await PlayerCmd.GainEnergy(PendingBringHomeEnergy, Owner);

        if (PendingBringHomeDraw > 0)
            await CardPileCmd.Draw(choiceContext, PendingBringHomeDraw, Owner);

        PendingBringHomeEnergy = 0;
        PendingBringHomeDraw = 0;

        await ResolveTurnStartFireGift(choiceContext);
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner.Creature))
            return;

        UnneededGoodChildPendingEnergy = 0;
        UnneededGoodChildPendingCount = 0;
        PendingYellowCostReduction = 0;

        // 「别看我」只管本回合下一次烧牌（任意牌）；牌身上的余火保留。
        _dontLookAtMePending.Clear();

        foreach (var (card, pending) in _bringHomeCards.ToArray())
        {
            if (card.Pile?.Type == PileType.Hand && !card.HasBeenRemovedFromState)
            {
                await CardCmd.Exhaust(choiceContext, card);
                PendingBringHomeEnergy += pending.Energy;
                PendingBringHomeDraw += pending.Draw;
            }

            _bringHomeCards.Remove(card);
        }
    }

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!IsOwnCardInHandOrPlay(card) || card.EnergyCost.CostsX)
            return false;

        if (PendingFreeSkillCount > 0 && card.Type == CardType.Skill)
            modifiedCost = 0m;
        else if (PendingYellowCostReduction > 0)
            modifiedCost = Math.Max(0m, originalCost - PendingYellowCostReduction);

        return modifiedCost != originalCost;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (!cardPlay.IsFirstInSeries
            || cardPlay.IsAutoPlay
            || !IsOwnCardInHandOrPlay(cardPlay.Card)
            || cardPlay.Card.EnergyCost.CostsX)
            return Task.CompletedTask;

        // 与费用修改的优先级一致：技能牌先吃「0 费」，吃掉了就不再占用亮黄的 -1。
        if (PendingFreeSkillCount > 0 && cardPlay.Card.Type == CardType.Skill)
        {
            PendingFreeSkillCount--;
            Flash();
        }
        else if (PendingYellowCostReduction > 0)
        {
            PendingYellowCostReduction = 0;
            Flash();
        }

        return Task.CompletedTask;
    }

    public void ModifyFireComponentChoiceOptions(YalisalinFireComponentContext context)
    {
        if (YalisalinFireComponentRules.AllCombatCards(Owner).Count() >= 13)
            return;

        var generated = YalisalinFireComponentRules.RandomYalisalinCard(Owner);
        if (generated == null)
            return;

        generated.EnergyCost.SetThisTurnOrUntilPlayed(0, reduceOnly: true);
        context.AddTemporaryCostOffset(generated, -999);
        context.AddExclusiveBurnCard(generated);
        context.BurnOnlyExclusiveCards = true;
    }

    public void ModifyFireComponentRightClickQueue(YalisalinFireComponentContext context)
    {
        if (PainKeeperPerTurnLimit > 0 && PainKeeperUsedThisTurn < PainKeeperPerTurnLimit)
        {
            context.AddRightClickRequest(new YalisalinFireRightClickRequest(
                YalisalinFireRightClickKind.PainKeeper,
                YalisalinFireComponentContext.Text("rightClick.painKeeper.prompt")));
        }

        for (var i = 0; i < UnneededGoodChildPendingCount; i++)
        {
            context.AddRightClickRequest(new YalisalinFireRightClickRequest(
                YalisalinFireRightClickKind.UnneededGoodChildCostUp,
                YalisalinFireComponentContext.Text("rightClick.unneededGoodChild.prompt")));
        }
    }

    public async Task AfterFireComponentChoiceCompleted(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context)
    {
        if (context.CountsAsManualUse && context.ChoiceCompleted)
            ManualFireComponentsCompletedThisCombat++;

        foreach (var applied in context.AppliedRightClicks.Where(applied => applied.Kind == YalisalinFireRightClickKind.PainKeeper))
            await ResolvePainKeeper(choiceContext, context, applied.Card);

        if (!SeparatedEndsEnabled || context.ChosenCard == null)
            return;

        if (context.ChosenCard == context.SourceCard)
        {
            await CreatureCmd.GainBlock(Owner.Creature, SeparatedEndsBlock, ValueProp.Move, context.CardPlay);
            return;
        }

        if (SeparatedEndsDraw > 0)
            await CardPileCmd.Draw(choiceContext, SeparatedEndsDraw, Owner);

        context.AddTemporaryCostOffset(context.ChosenCard, -1);
    }

    public async Task BeforeFireComponentBurned(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context)
    {
        // 把道歉烧成灰（独立能力 Power）：余火烧掉的牌先自动打出1次再烧掉
        if (Owner.Creature.GetPower<BurnedApologyPower>() != null)
            context.CustomData["AutoPlayBurnedCard"] = true;

        if (UnneededGoodChildPendingCount <= 0 || context.BurnedCard == null)
            return;

        var cost = context.GetEffectiveCost(context.BurnedCard);
        if (cost > 0 && Owner.Creature.CombatState is { } combatState)
        {
            await DamageCmd.Attack(UnneededGoodChildPendingCount)
                .FromCard(context.SourceCard, context.CardPlay)
                .TargetingRandomOpponents(combatState)
                .WithHitCount(cost)
                .Execute(choiceContext);
        }

        if (cost > 3 && YalisalinFireComponentRules.HasFireComponent(context.BurnedCard))
        {
            await PlayerCmd.GainEnergy(UnneededGoodChildPendingEnergy, Owner);

            // 「不被需要的好孩子」卡面：费用大于3且自带余火时「获得能量然后打出其」。
            // 复用「把道歉烧成灰」的免费自动打出通道：先打出、再烧掉。
            context.CustomData["AutoPlayBurnedCard"] = true;
        }

        UnneededGoodChildPendingEnergy = 0;
        UnneededGoodChildPendingCount = 0;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (result.TotalDamage <= 0 || _fireEffectDepth > 0)
            return;

        if (!IsYalisalinAttackCardDamage(target, dealer, cardSource) || !CanTrack(target))
            return;

        await ConsumeFireColor(choiceContext, target, 1, cardSource);
    }

    public async Task AfterFireComponentBurned(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context)
    {
        if (context.BurnedCard != null)
            FireComponentBurnsThisTurn++;

        if (context.BurnedCard is { } burned)
        {
            _burnedCardsThisTurn.Remove(burned);
            _burnedCardsThisTurn.Add(burned);

            // 痛觉的重量（独立能力 Power）：累计余火烧牌数，达到阈值获得力量
            if (Owner.Creature.GetPower<WeightOfPainPower>() is { } weightOfPain)
                await weightOfPain.ResolveBurned(choiceContext);

            // 你的包容很刺眼（独立能力 Power）：余火烧掉牌时造成伤害+格挡
            if (Owner.Creature.GetPower<DazzlingTolerancePower>() is { } tolerance)
                await tolerance.ResolveBurned(choiceContext, burned, context.SourceCard, context.CardPlay);

            await ResolveDontLookAtMe(choiceContext);
        }
    }

    public Task AfterFireComponentResolved(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context)
    {
        // 不该留下的温柔（独立能力 Power）：余火选择完成时随机给无余火的牌加余火
        if (Owner.Creature.GetPower<WarmthShouldNotStayPower>() is { } warmth)
            warmth.ResolveAfterComponent(Owner);

        return Task.CompletedTask;
    }

    private async Task ResolvePainKeeper(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context,
        CardModel card)
    {
        if (PainKeeperUsedThisTurn >= PainKeeperPerTurnLimit)
            return;

        PainKeeperUsedThisTurn++;
        await PowerCmd.Apply<YlsmPower>(choiceContext, Owner.Creature, 1, Owner.Creature, context.SourceCard, false);

        switch (card.Type)
        {
            // 攻击牌：给予全部存活敌人各 1 层亚里沙的魔法（不再是只给当前目标）。
            case CardType.Attack:
                if (Owner.Creature.CombatState is { } combatState)
                    foreach (var enemy in combatState.Enemies.Where(e => e.IsAlive).ToList())
                        await PowerCmd.Apply<YlsmPower>(
                            choiceContext, enemy, 1, Owner.Creature, context.SourceCard, false);
                break;
            case CardType.Skill:
                await PlayerCmd.GainEnergy(1, Owner);
                break;
            case CardType.Power:
                YalisalinFireComponentRules.TryAddFireComponent(
                    YalisalinFireComponentRules.RandomCardWithoutFireComponent(Owner));
                break;
        }
    }

    private async Task ResolveDontLookAtMe(PlayerChoiceContext choiceContext)
    {
        if (_dontLookAtMePending.Count == 0)
            return;

        // FIFO：一次烧牌只消耗一次「请别看见我」的触发。
        var gainBlock = _dontLookAtMePending[0];
        _dontLookAtMePending.RemoveAt(0);

        await CardPileCmd.Draw(choiceContext, 1, Owner);
        if (gainBlock)
            await CreatureCmd.GainBlock(Owner.Creature, 6, ValueProp.Move, cardPlay: null);
    }

    /// <summary>
    ///     给予目标火色，从最低的空格往上填。「窗上的车票」的额外格数在这里统一加上。
    ///
    ///     量表已满时多出来的格数默认直接丢弃；<paramref name="overflowTriggersConsume" /> 为真时（「予燎」类效果），
    ///     多出的每一格按「溢出色序」结算该格位的消耗效果：第 1-2 格浅橙、第 3-4 格亮黄、第 5-6 格赤红，
    ///     第 7 格回到浅橙（<c>SlotColor(n % 6 + 1)</c>，每 6 格一循环）。
    ///     溢出**不改变量表格数**（始终满格），所以只要持续给出火色就会持续溢出、持续结算。
    ///     溢出色序与连续链都记在目标各自的量表上，因此同一回合内先后多次溢出会接着往后数，
    ///     并像普通消耗那样两两同色构成「连续」；这条链与普通消耗的链互不干扰。
    /// </summary>
    /// <returns>实际填进量表的格数。</returns>
    public async Task<int> GiveFireColor(
        PlayerChoiceContext choiceContext,
        Creature target,
        int amount,
        CardModel? source = null,
        bool overflowTriggersConsume = false)
    {
        if (!CanTrack(target) || amount <= 0)
            return 0;

        var total = amount + TicketStacks;
        var gauge = GetOrCreateGauge(target);
        var added = gauge.Fill(total, CurrentMaxSegments);
        var overflow = total - added;

        FireGivenThisTurn += added + (overflowTriggersConsume ? overflow : 0);
        if (added > 0)
            Flash();

        if (TicketStacks > 0 && !TicketEnergyGrantedThisTurn && FireGivenThisTurn >= TicketEnergyThreshold)
        {
            TicketEnergyGrantedThisTurn = true;
            await PlayerCmd.GainEnergy(TicketStacks, Owner);
        }

        if (overflowTriggersConsume && overflow > 0)
        {
            // 溢出色序与连续链挂在目标的量表上，跨多次给予持续累计（回合开始才重置）。
            for (var i = 0; i < overflow; i++)
            {
                var slot = gauge.TakeNextOverflowSlot(CurrentMaxSegments);
                await ResolveConsumedColor(choiceContext, target, SlotColorFor(slot), source, gauge.OverflowChain, 1);
            }
        }

        return added;
    }

    /// <summary>从最新（最高）的一格开始消耗目标的火色。</summary>
    /// <returns>按消耗顺序排列的颜色。</returns>
    public async Task<IReadOnlyList<YalisalinFireColor>> ConsumeFireColor(
        PlayerChoiceContext choiceContext,
        Creature target,
        int amount,
        CardModel? source = null,
        int effectMultiplier = 1)
    {
        List<YalisalinFireColor> consumed = [];
        if (!_gauges.TryGetValue(target, out var gauge))
            return consumed;

        for (var i = 0; i < amount && gauge.Filled > 0; i++)
            consumed.Add(await ConsumeSlot(choiceContext, target, gauge, gauge.Filled, source, effectMultiplier));

        return consumed;
    }

    public Task<IReadOnlyList<YalisalinFireColor>> ConsumeAllFireColor(
        PlayerChoiceContext choiceContext,
        Creature target,
        CardModel? source = null)
    {
        return ConsumeFireColor(choiceContext, target, GetFireColorCount(target), source);
    }

    /// <summary>
    ///     消耗目标本回合被给予的火色里最早的一格（「倒着算」的特例：不从最新格开始）。
    ///     量表按格位定色，抽走中间一格后上方各格整体下落，颜色跟随新格位。
    /// </summary>
    public async Task<YalisalinFireColor?> ConsumeEarliestGivenThisTurn(
        PlayerChoiceContext choiceContext,
        Creature target,
        CardModel? source = null,
        int effectMultiplier = 1)
    {
        if (!_gauges.TryGetValue(target, out var gauge) || gauge.GivenThisTurn <= 0)
            return null;

        return await ConsumeSlot(choiceContext, target, gauge, gauge.TurnStartFloor + 1, source, effectMultiplier);
    }

    /// <summary>只结算某色的单格消耗奖励，不进连续链、不计入消耗记录（用于「两份不同的证词」等额外奖励）。</summary>
    public async Task ResolveExtraFireColorReward(
        PlayerChoiceContext choiceContext,
        YalisalinFireColor color,
        CardModel? source = null)
    {
        _fireEffectDepth++;
        try
        {
            await ResolveBaseReward(choiceContext, color, source);
        }
        finally
        {
            _fireEffectDepth--;
        }

        Flash();
    }

    public IReadOnlyList<YalisalinFireColorSegment> GetFireColorSegments(Creature target)
    {
        var filled = GetFireColorCount(target);
        return Enumerable.Range(1, filled)
            .Select(slot => new YalisalinFireColorSegment(SlotColorFor(slot), slot))
            .ToArray();
    }

    private async Task<YalisalinFireColor> ConsumeSlot(
        PlayerChoiceContext choiceContext,
        Creature target,
        YalisalinFireColorGauge gauge,
        int slot,
        CardModel? source,
        int effectMultiplier)
    {
        var color = SlotColorFor(slot);
        gauge.RemoveSlot();
        Flash();
        await ResolveConsumedColor(choiceContext, target, color, source, _chain, effectMultiplier);
        return color;
    }

    private async Task ResolveConsumedColor(
        PlayerChoiceContext choiceContext,
        Creature target,
        YalisalinFireColor color,
        CardModel? source,
        YalisalinFireColorChain chain,
        int effectMultiplier)
    {
        var bound = Owner.Creature.GetPower<BoundPrometheusPower>();
        if (bound?.LockedColor is { } lockedColor)
            color = lockedColor;

        _consumptionLog.Add(color);

        // 【坏结局】毒化：火色不再触发消耗效果（格位已消耗、记录已写入，直接返回）。
        if (IsFireColorConsumeSuppressed)
            return;

        _fireEffectDepth++;
        try
        {
            for (var i = 0; i < Math.Max(1, effectMultiplier); i++)
                await ResolveBaseReward(choiceContext, color, source);

            // 「被缚的普罗米修斯」期间所有消耗视为同色、且不触发连续；改为每次消耗额外造成伤害。
            if (bound != null)
            {
                await bound.OnFireColorConsumed(choiceContext, target);
                return;
            }

            if (!chain.Advance(color))
                return;

            ContinuousTriggersThisCombat++;
            for (var i = 0; i < 1 + Math.Max(0, ExtraContinuousTriggers); i++)
                await ResolveContinuousReward(choiceContext, color, source);

            if (Owner.Creature.GetPower<MixedConclusionPower>() is { } mixed)
                await mixed.OnContinuousTriggered(choiceContext);

            if (Owner.Creature.GetPower<ThirteenthListenerPower>() is { } listener)
                await listener.OnContinuousTriggered(choiceContext, target, source);
        }
        finally
        {
            _fireEffectDepth--;
        }
    }

    private async Task ResolveBaseReward(
        PlayerChoiceContext choiceContext,
        YalisalinFireColor color,
        CardModel? source)
    {
        switch (color)
        {
            case YalisalinFireColor.LightOrange:
                await CreatureCmd.GainBlock(Owner.Creature, OrangeConsumeBlock, ValueProp.Move, cardPlay: null);
                break;
            case YalisalinFireColor.BrightYellow:
                PendingYellowCostReduction++;
                await ApplyYellowNextTurnEnergy(choiceContext, 1, source);
                break;
            case YalisalinFireColor.Red:
                var damage = RedConsumeDamage;
                var damagedEnemies = await DealRedFireColorDamage(choiceContext, damage, source);
                foreach (var enemy in damagedEnemies.Where(static enemy => enemy.IsAlive).Distinct())
                    await PowerCmd.Apply<YlsmPower>(choiceContext, enemy, damage, Owner.Creature, source, false);

                RedConsumeDamage++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(color), color, null);
        }
    }

    private async Task ResolveContinuousReward(
        PlayerChoiceContext choiceContext,
        YalisalinFireColor color,
        CardModel? source)
    {
        switch (color)
        {
            case YalisalinFireColor.LightOrange:
                await CardPileCmd.Draw(choiceContext, 1, Owner);
                break;
            case YalisalinFireColor.BrightYellow:
                await PlayerCmd.GainEnergy(1, Owner);
                break;
            case YalisalinFireColor.Red:
                // 「额外造成一次第 2 段赤红的伤害」：第 2 段结算后赤红伤害已 +1，这里取回第 2 段当时的数值。
                await DealRedFireColorDamage(choiceContext, RedConsumeDamage - 1, source);
                await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, source, false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(color), color, null);
        }
    }

    private async Task ResolveTurnStartFireGift(PlayerChoiceContext choiceContext)
    {
        if (TurnStartFireGift <= 0 || Owner.Creature.CombatState is not { } combatState)
            return;

        var target = Owner.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies.Where(CanTrack));
        if (target == null)
            return;

        await GiveFireColor(choiceContext, target, TurnStartFireGift);
    }

    private async Task ApplyYellowNextTurnEnergy(
        PlayerChoiceContext choiceContext,
        int yellow,
        CardModel? source)
    {
        var maxEnergy = Math.Max(0, Owner.PlayerCombatState?.MaxEnergy ?? Owner.MaxEnergy);
        var remaining = maxEnergy - YellowNextTurnEnergyGrantedThisTurn;
        if (remaining <= 0)
            return;

        var amount = Math.Min(yellow, remaining);
        YellowNextTurnEnergyGrantedThisTurn += amount;
        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, amount, Owner.Creature, source);
    }

    private async Task<IReadOnlyList<Creature>> DealRedFireColorDamage(
        PlayerChoiceContext choiceContext,
        int damage,
        CardModel? source)
    {
        if (Owner.Creature.CombatState is not { } combatState)
            return [];

        if (source != null)
        {
            var attack = await DamageCmd.Attack(damage)
                .FromCard(source, null)
                .TargetingRandomOpponents(combatState)
                .Execute(choiceContext);

            return attack.Results
                .SelectMany(static result => result)
                .Select(static result => result.Receiver)
                .Distinct()
                .ToArray();
        }

        var targets = combatState.HittableEnemies
            .Where(static enemy => enemy.IsAlive)
            .ToList();
        var target = Owner.RunState.Rng.CombatTargets.NextItem(targets);
        if (target == null)
            return [];

        await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Move, Owner.Creature, null, null);
        return [target];
    }

    private bool IsOwnCardInHandOrPlay(CardModel card)
    {
        return card.Owner == Owner && card.Pile?.Type is PileType.Hand or PileType.Play;
    }

    private bool IsYalisalinAttackCardDamage(
        Creature target,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner.Creature)
            return false;

        if (target.Side == Owner.Creature.Side)
            return false;

        return cardSource?.Owner == Owner && cardSource.Type == CardType.Attack;
    }

    public bool CanTrack(Creature target)
    {
        return target.IsAlive && target.Side != Owner.Creature.Side;
    }

    private YalisalinFireColorGauge GetOrCreateGauge(Creature target)
    {
        if (_gauges.TryGetValue(target, out var gauge))
            return gauge;

        gauge = new YalisalinFireColorGauge();
        _gauges[target] = gauge;
        return gauge;
    }
}

public enum YalisalinFireColor
{
    LightOrange,
    BrightYellow,
    Red
}

/// <param name="Order">格位（1 起）。</param>
public readonly record struct YalisalinFireColorSegment(YalisalinFireColor Color, long Order)
{
    public Color DisplayColor => Color switch
    {
        YalisalinFireColor.LightOrange => new Color("#c4631c"),
        YalisalinFireColor.BrightYellow => new Color("#7f1d1d"),
        YalisalinFireColor.Red => new Color("#dc143c"),
        _ => Colors.White
    };
}

internal readonly record struct BringHomePendingCard(int Energy, int Draw);

/// <summary>
///     一名敌人的火色量表。格位决定颜色，量表恒为 1..<see cref="Filled" /> 的连续前缀，
///     所以这里只记已填格数，以及本回合开始以来的最低水位（用于「本回合给予的火色」）。
/// </summary>
internal sealed class YalisalinFireColorGauge
{
    public int Filled { get; private set; }

    /// <summary>本回合开始以来量表降到过的最低格数；其上方的格子都是本回合给予的。</summary>
    public int TurnStartFloor { get; private set; }

    public int GivenThisTurn => Filled - TurnStartFloor;

    public int Fill(int amount, int maxSegments)
    {
        var added = Math.Clamp(amount, 0, maxSegments - Filled);
        Filled += added;
        return added;
    }

    public void RemoveSlot()
    {
        if (Filled <= 0)
            return;

        Filled--;
        TurnStartFloor = Math.Min(TurnStartFloor, Filled);
    }

    public void MarkTurnStart()
    {
        TurnStartFloor = Filled;
    }

    /// <summary>
    ///     本回合该目标已溢出的累计格数，决定下一格溢出使用哪个格位的颜色（<c>n % 6 + 1</c>，每 6 格一循环）。
    /// </summary>
    public int OverflowConsumedThisTurn { get; private set; }

    /// <summary>溢出的连续链（与普通消耗的 <c>_chain</c> 分开，互不干扰）。</summary>
    public YalisalinFireColorChain OverflowChain { get; } = new();

    /// <summary>取下一格溢出对应的格位（1..当前格数上限），并推进溢出计数。</summary>
    public int TakeNextOverflowSlot(int maxSegments)
    {
        var slot = OverflowConsumedThisTurn % Math.Max(1, maxSegments) + 1;
        OverflowConsumedThisTurn++;
        return slot;
    }

    /// <summary>回合开始：清空溢出色序与溢出连续链。</summary>
    public void ResetOverflowTracking()
    {
        OverflowChain.Reset();
        OverflowConsumedThisTurn = 0;
    }
}

/// <summary>连续消耗链：同色两两成对触发一次「连续」，异色重新起算。</summary>
internal sealed class YalisalinFireColorChain
{
    private YalisalinFireColor? _color;
    private int _length;

    /// <returns>这一次消耗是否凑成了一对同色连续。</returns>
    public bool Advance(YalisalinFireColor color)
    {
        if (_color == color)
        {
            _length++;
        }
        else
        {
            _color = color;
            _length = 1;
        }

        if (_length < 2)
            return false;

        _length = 0;
        return true;
    }

    public void Reset()
    {
        _color = null;
        _length = 0;
    }
}
