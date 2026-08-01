using ManosabaLin.Characters.Yalisalin.Capabilities;
using ManosabaLin.Characters.Yalisalin.Cards;
using ManosabaLin.Characters.Yalisalin.Components;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Yalisalin.Relics;

[RegisterRelic(typeof(YalisalinRelicPool))]
[RegisterCharacterStarterRelic(typeof(Yalisalin))]
public sealed class YalisalinsHairpin : ManosabaRelicTemplate, IYalisalinFireComponentModifier
{
    public const string LocalizationEntry = "MANOSABA_LIN_RELIC_YALISALINS_HAIRPIN";
    public const int MaxSegments = 6;
    private const decimal WitchificationRequiredForCarbonization = 100m;
    private const int OrangeConsumeBlock = 4;
    private const int RedConsumeBaseDamage = 3;

    private readonly Dictionary<Creature, YalisalinFireColorGauge> _gauges = [];
    private readonly Dictionary<CardModel, bool> _dontLookAtMeCards = [];
    private readonly List<(CardModel Card, int Block)> _glassReturnCards = [];
    private readonly Dictionary<CardModel, BringHomePendingCard> _bringHomeCards = [];
    private Creature? _currentFireColorTarget;
    private long _conversionSequence;

    [SavedProperty] public int PainKeeperPerTurnLimit { get; private set; }
    [SavedProperty] public int PainKeeperUsedThisTurn { get; private set; }
    [SavedProperty] public int DazzlingToleranceStacks { get; private set; }
    [SavedProperty] public bool BurnedApologyEnabled { get; private set; }
    [SavedProperty] public int UnneededGoodChildPendingCount { get; private set; }
    [SavedProperty] public int UnneededGoodChildPendingEnergy { get; private set; }
    [SavedProperty] public bool SeparatedEndsEnabled { get; private set; }
    [SavedProperty] public int SeparatedEndsBlock { get; private set; }
    [SavedProperty] public int SeparatedEndsDraw { get; private set; }
    [SavedProperty] public bool WarmthShouldNotStayEnabled { get; private set; }
    [SavedProperty] public int FireComponentBurnsThisTurn { get; private set; }
    [SavedProperty] public int ManualFireComponentsCompletedThisCombat { get; private set; }
    [SavedProperty] public int PendingBringHomeEnergy { get; private set; }
    [SavedProperty] public int PendingBringHomeDraw { get; private set; }
    [SavedProperty] public int YellowNextTurnEnergyGrantedThisTurn { get; private set; }
    [SavedProperty] public int PendingYellowCostReduction { get; private set; }
    [SavedProperty] public int RedConsumeDamage { get; private set; }
    [SavedProperty] public int PreserveHighestFireColor { get; private set; }
    [SavedProperty] public int PreserveHighestAtTurnStart { get; private set; }
    [SavedProperty] public bool PreserveHighestRewriteEnergyEnabled { get; private set; }
    [SavedProperty] public bool PreserveHighestRewriteEnergyUsedThisTurn { get; private set; }
    [SavedProperty] public bool MixedConclusionEnabled { get; private set; }
    [SavedProperty] public bool MixedConclusionUsedThisTurn { get; private set; }
    [SavedProperty] public bool FullRefillPreserveEnabled { get; private set; }
    [SavedProperty] public bool FullRefillPreserveUsedThisTurn { get; private set; }
    [SavedProperty] public bool ThirteenthListeningEnabled { get; private set; }
    [SavedProperty] public bool ThirteenthCoverUsedThisTurn { get; private set; }
    [SavedProperty] public bool ThirteenthRewriteUsedThisTurn { get; private set; }
    [SavedProperty] public int SealedLightOrange { get; private set; }
    [SavedProperty] public int SealedBrightYellow { get; private set; }
    [SavedProperty] public int SealedRed { get; private set; }
    [SavedProperty] public int SealedBlackRed { get; private set; }
    [SavedProperty] public int OrangeConsumePairProgress { get; private set; }
    [SavedProperty] public int YellowConsumePairProgress { get; private set; }
    [SavedProperty] public int RedConsumePairProgress { get; private set; }
    [SavedProperty] public bool HasLastConsumedFireColorThisTurn { get; private set; }
    [SavedProperty] public YalisalinFireColor LastConsumedFireColorThisTurn { get; private set; }

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => false;
    public override int DisplayAmount => 0;
    public bool IsCarbonizationUnlocked => Owner.Creature.GetPower<WithPower>()?.Amount >= WitchificationRequiredForCarbonization;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return new HoverTip(
                new LocString("relics", $"{Id.Entry}.fireColor.title"),
                new LocString("relics", $"{Id.Entry}.fireColor.description"));

            if (PreserveHighestFireColor > 0 || PreserveHighestAtTurnStart > 0)
            {
                yield return new HoverTip(
                    new LocString("relics", $"{Id.Entry}.fireColor.preserveHighest.title"),
                    new LocString("relics", $"{Id.Entry}.fireColor.preserveHighest.description"));
            }

            yield return YalisalinFireComponentCapability.CreateHoverTip(Owner);
        }
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

    public void EnableDazzlingTolerance()
    {
        DazzlingToleranceStacks++;
        Flash();
    }

    public void EnableBurnedApology()
    {
        BurnedApologyEnabled = true;
        Flash();
    }

    public void EnableSeparatedEnds(int block, int draw)
    {
        SeparatedEndsEnabled = true;
        SeparatedEndsBlock = Math.Max(SeparatedEndsBlock, block);
        SeparatedEndsDraw = Math.Max(SeparatedEndsDraw, draw);
        Flash();
    }

    public void EnableWarmthShouldNotStay()
    {
        WarmthShouldNotStayEnabled = true;
        Flash();
    }

    public void EnableMixedConclusion()
    {
        MixedConclusionEnabled = true;
        Flash();
    }

    public void EnablePreserveHighestAtTurnStart()
    {
        PreserveHighestAtTurnStart++;
        Flash();
    }

    public void EnablePreserveHighestRewriteEnergy()
    {
        PreserveHighestRewriteEnergyEnabled = true;
        Flash();
    }

    public void EnableFullRefillPreserve()
    {
        FullRefillPreserveEnabled = true;
        Flash();
    }

    public void EnableThirteenthListening()
    {
        ThirteenthListeningEnabled = true;
        Flash();
    }

    public void GainPreserveHighestFireColor(int amount)
    {
        if (amount <= 0)
            return;

        PreserveHighestFireColor += amount;
        Flash();
    }

    public void GrantSealedFire(YalisalinFireColor color, int amount = 1)
    {
        if (amount <= 0)
            return;

        switch (color)
        {
            case YalisalinFireColor.LightOrange:
                SealedLightOrange += amount;
                break;
            case YalisalinFireColor.BrightYellow:
                SealedBrightYellow += amount;
                break;
            case YalisalinFireColor.Red:
                SealedRed += amount;
                break;
            case YalisalinFireColor.BlackRed:
                SealedBlackRed += amount;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(color), color, null);
        }

        Flash();
    }

    public bool HasAnySealedFire()
    {
        return SealedLightOrange > 0
               || SealedBrightYellow > 0
               || SealedRed > 0
               || SealedBlackRed > 0;
    }

    public bool TryCopySealedFire()
    {
        if (!TryGetSealedFireColorToCopy(out var color))
            return false;

        GrantSealedFire(color);
        return true;
    }

    public bool TryGetEarliestFireColor(Creature target, out YalisalinFireColor color)
    {
        color = default;
        return _gauges.TryGetValue(target, out var gauge)
               && gauge.TryGetEarliestColor(out color);
    }

    public bool TryGetHighestFireColor(Creature target, out YalisalinFireColor color)
    {
        color = default;
        return _gauges.TryGetValue(target, out var gauge)
               && gauge.TryGetHighestColor(out color);
    }

    public bool TargetHasFireColor(Creature target)
    {
        return _gauges.TryGetValue(target, out var gauge) && gauge.Count > 0;
    }

    public bool IsFireColorFull(Creature target)
    {
        return _gauges.TryGetValue(target, out var gauge) && gauge.IsFull;
    }

    public bool TryGetLastConsumedFireColorThisTurn(out YalisalinFireColor color)
    {
        color = LastConsumedFireColorThisTurn;
        return HasLastConsumedFireColorThisTurn;
    }

    public bool TryUseSealedFire(
        Creature target,
        YalisalinFireColor color,
        int slotIndex = 0,
        CardModel? source = null)
    {
        if (!CanTrack(target) || !TrySpendSealedFire(color))
            return false;

        _currentFireColorTarget = target;
        var gauge = GetOrCreateGauge(target);
        gauge.InsertColor(color, slotIndex, ref _conversionSequence);

        if (ThirteenthListeningEnabled && !ThirteenthCoverUsedThisTurn && gauge.IsFull)
        {
            ThirteenthCoverUsedThisTurn = true;
            GainPreserveHighestFireColor(1);
        }

        Flash();
        return true;
    }

    public void QueueUnneededGoodChild(int energyGain)
    {
        UnneededGoodChildPendingCount++;
        UnneededGoodChildPendingEnergy = Math.Max(UnneededGoodChildPendingEnergy, energyGain);
        Flash();
    }

    public void TrackDontLookAtMe(CardModel card, bool gainBlock)
    {
        _dontLookAtMeCards[card] = gainBlock;
    }

    public void QueueGlassReturn(CardModel card, int block)
    {
        if (_glassReturnCards.Any(entry => entry.Card == card))
            return;

        _glassReturnCards.Add((card, block));
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

        if (DazzlingToleranceStacks > 0)
            yield return YalisalinFireComponentContext.Text(
                "enhancement.dazzlingTolerance",
                ("Damage", DazzlingToleranceStacks),
                ("Block", DazzlingToleranceStacks * 2));

        if (BurnedApologyEnabled)
            yield return YalisalinFireComponentContext.Text("enhancement.burnedApology");

        if (SeparatedEndsEnabled)
            yield return YalisalinFireComponentContext.Text(
                "enhancement.separatedEnds",
                ("Block", SeparatedEndsBlock),
                ("Cards", SeparatedEndsDraw));

        if (WarmthShouldNotStayEnabled)
            yield return YalisalinFireComponentContext.Text("enhancement.warmthShouldNotStay");

        if (UnneededGoodChildPendingCount > 0)
            yield return YalisalinFireComponentContext.Text(
                "enhancement.unneededGoodChild",
                ("Damage", UnneededGoodChildPendingCount),
                ("Energy", UnneededGoodChildPendingEnergy));
    }

    public override Task BeforeCombatStart()
    {
        _gauges.Clear();
        _dontLookAtMeCards.Clear();
        _glassReturnCards.Clear();
        _bringHomeCards.Clear();
        _currentFireColorTarget = null;
        _conversionSequence = 0;
        PainKeeperPerTurnLimit = 0;
        PainKeeperUsedThisTurn = 0;
        DazzlingToleranceStacks = 0;
        BurnedApologyEnabled = false;
        UnneededGoodChildPendingCount = 0;
        UnneededGoodChildPendingEnergy = 0;
        SeparatedEndsEnabled = false;
        SeparatedEndsBlock = 0;
        SeparatedEndsDraw = 0;
        WarmthShouldNotStayEnabled = false;
        FireComponentBurnsThisTurn = 0;
        ManualFireComponentsCompletedThisCombat = 0;
        PendingBringHomeEnergy = 0;
        PendingBringHomeDraw = 0;
        YellowNextTurnEnergyGrantedThisTurn = 0;
        PendingYellowCostReduction = 0;
        RedConsumeDamage = RedConsumeBaseDamage;
        PreserveHighestFireColor = 0;
        PreserveHighestAtTurnStart = 0;
        PreserveHighestRewriteEnergyEnabled = false;
        PreserveHighestRewriteEnergyUsedThisTurn = false;
        MixedConclusionEnabled = false;
        MixedConclusionUsedThisTurn = false;
        FullRefillPreserveEnabled = false;
        FullRefillPreserveUsedThisTurn = false;
        ThirteenthListeningEnabled = false;
        ThirteenthCoverUsedThisTurn = false;
        ThirteenthRewriteUsedThisTurn = false;
        SealedLightOrange = 0;
        SealedBrightYellow = 0;
        SealedRed = 0;
        SealedBlackRed = 0;
        OrangeConsumePairProgress = 0;
        YellowConsumePairProgress = 0;
        RedConsumePairProgress = 0;
        HasLastConsumedFireColorThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _gauges.Clear();
        _dontLookAtMeCards.Clear();
        _glassReturnCards.Clear();
        _bringHomeCards.Clear();
        _currentFireColorTarget = null;
        _conversionSequence = 0;
        PainKeeperPerTurnLimit = 0;
        PainKeeperUsedThisTurn = 0;
        DazzlingToleranceStacks = 0;
        BurnedApologyEnabled = false;
        UnneededGoodChildPendingCount = 0;
        UnneededGoodChildPendingEnergy = 0;
        SeparatedEndsEnabled = false;
        SeparatedEndsBlock = 0;
        SeparatedEndsDraw = 0;
        WarmthShouldNotStayEnabled = false;
        FireComponentBurnsThisTurn = 0;
        ManualFireComponentsCompletedThisCombat = 0;
        PendingBringHomeEnergy = 0;
        PendingBringHomeDraw = 0;
        YellowNextTurnEnergyGrantedThisTurn = 0;
        PendingYellowCostReduction = 0;
        RedConsumeDamage = RedConsumeBaseDamage;
        PreserveHighestFireColor = 0;
        PreserveHighestAtTurnStart = 0;
        PreserveHighestRewriteEnergyEnabled = false;
        PreserveHighestRewriteEnergyUsedThisTurn = false;
        MixedConclusionEnabled = false;
        MixedConclusionUsedThisTurn = false;
        FullRefillPreserveEnabled = false;
        FullRefillPreserveUsedThisTurn = false;
        ThirteenthListeningEnabled = false;
        ThirteenthCoverUsedThisTurn = false;
        ThirteenthRewriteUsedThisTurn = false;
        SealedLightOrange = 0;
        SealedBrightYellow = 0;
        SealedRed = 0;
        SealedBlackRed = 0;
        OrangeConsumePairProgress = 0;
        YellowConsumePairProgress = 0;
        RedConsumePairProgress = 0;
        HasLastConsumedFireColorThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return;

        PainKeeperUsedThisTurn = 0;
        FireComponentBurnsThisTurn = 0;
        YellowNextTurnEnergyGrantedThisTurn = 0;
        PendingYellowCostReduction = 0;
        PreserveHighestRewriteEnergyUsedThisTurn = false;
        MixedConclusionUsedThisTurn = false;
        FullRefillPreserveUsedThisTurn = false;
        ThirteenthCoverUsedThisTurn = false;
        ThirteenthRewriteUsedThisTurn = false;
        HasLastConsumedFireColorThisTurn = false;

        if (PreserveHighestAtTurnStart > 0)
            PreserveHighestFireColor += PreserveHighestAtTurnStart;

        foreach (var (card, block) in _glassReturnCards.ToArray())
        {
            if (!card.HasBeenRemovedFromState)
            {
                await CardPileCmd.Add(card, PileType.Hand);
                card.EnergyCost.SetThisTurnOrUntilPlayed(0, reduceOnly: true);
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

        await ResolveFireColorTurnStartRewards(choiceContext);
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
        if (PendingYellowCostReduction <= 0 || !CanApplyPendingYellowCostReduction(card))
            return false;

        modifiedCost = Math.Max(0m, originalCost - PendingYellowCostReduction);
        return modifiedCost != originalCost;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (PendingYellowCostReduction > 0
            && cardPlay.IsFirstInSeries
            && !cardPlay.IsAutoPlay
            && CanApplyPendingYellowCostReduction(cardPlay.Card))
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
        if (BurnedApologyEnabled)
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
            await PlayerCmd.GainEnergy(UnneededGoodChildPendingEnergy, Owner);

        UnneededGoodChildPendingEnergy = 0;
        UnneededGoodChildPendingCount = 0;
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (result.TotalDamage <= 0)
            return Task.CompletedTask;

        if (!IsYalisalinCardDamage(target, dealer, cardSource))
            return Task.CompletedTask;

        if (TryAddFireColor(target, 1, cardSource))
            Flash();

        return Task.CompletedTask;
    }

    public async Task AfterFireComponentBurned(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context)
    {
        if (context.BurnedCard != null)
            FireComponentBurnsThisTurn++;

        if (context.BurnedCard is { } burned)
        {
            await ResolveDazzlingTolerance(choiceContext, context, burned);
            await ResolveDontLookAtMe(choiceContext, burned);
        }
    }

    public Task AfterFireComponentResolved(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context)
    {
        if (WarmthShouldNotStayEnabled)
            YalisalinFireComponentRules.TryAddFireComponent(
                YalisalinFireComponentRules.RandomCardWithoutFireComponent(Owner));

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
            case CardType.Attack when context.Target is { } target && target.Side != Owner.Creature.Side:
                await PowerCmd.Apply<YlsmPower>(choiceContext, target, 1, Owner.Creature, context.SourceCard, false);
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

    private async Task ResolveDazzlingTolerance(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context,
        CardModel burned)
    {
        if (DazzlingToleranceStacks <= 0)
            return;

        var count = IsCurse(burned) ? 3 : 1;
        var damage = DazzlingToleranceStacks;
        var block = DazzlingToleranceStacks * 2;
        for (var i = 0; i < count; i++)
        {
            if (Owner.Creature.CombatState is { } combatState)
            {
                await DamageCmd.Attack(damage)
                    .FromCard(context.SourceCard, context.CardPlay)
                    .TargetingRandomOpponents(combatState)
                    .Execute(choiceContext);
            }

            await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, context.CardPlay);
        }
    }

    private async Task ResolveDontLookAtMe(PlayerChoiceContext choiceContext, CardModel burned)
    {
        if (!_dontLookAtMeCards.Remove(burned, out var gainBlock))
            return;

        await CardPileCmd.Draw(choiceContext, 1, Owner);
        if (gainBlock)
            await CreatureCmd.GainBlock(Owner.Creature, 6, ValueProp.Move, cardPlay: null);
    }

    private static bool IsCurse(CardModel card)
    {
        return card.Type == CardType.Curse || card.Rarity == CardRarity.Curse;
    }

    public bool TryAddFireColor(Creature target, int amount = 1, CardModel? source = null)
    {
        if (!CanTrack(target) || amount <= 0)
            return false;

        _currentFireColorTarget = target;
        var gauge = GetOrCreateGauge(target);
        var changed = false;
        for (var i = 0; i < amount; i++)
        {
            var wasFull = gauge.IsFull;
            changed |= gauge.AddLightOrange(ref _conversionSequence);
            if (!wasFull && gauge.IsFull)
                AfterTargetFireColorFilledToFull();
        }

        return changed;
    }

    public bool TryConvertFireColor(Creature target, CardModel? source = null)
    {
        if (!CanTrack(target))
            return false;

        _currentFireColorTarget = target;
        var changed = GetOrCreateGauge(target).TryPromoteOnce(IsCarbonizationUnlocked, ref _conversionSequence);
        if (changed)
            Flash();

        return changed;
    }

    public bool TryStrongConvertFireColor(Creature target, CardModel? source = null)
    {
        if (!CanTrack(target))
            return false;

        _currentFireColorTarget = target;
        var changed = GetOrCreateGauge(target).TryStrongPromoteOnce(IsCarbonizationUnlocked, ref _conversionSequence);
        if (changed)
            Flash();

        return changed;
    }

    public bool TryDowngradeFireColor(
        Creature target,
        out YalisalinFireColor originalColor,
        CardModel? source = null)
    {
        originalColor = default;
        if (!_gauges.TryGetValue(target, out var gauge))
            return false;

        _currentFireColorTarget = target;
        var changed = gauge.TryDowngradeEarliest(out originalColor);
        if (changed)
            Flash();

        return changed;
    }

    public bool TryMoveLastFireColorToFront(
        Creature target,
        out YalisalinFireColor movedColor,
        CardModel? source = null)
    {
        movedColor = default;
        if (!_gauges.TryGetValue(target, out var gauge))
            return false;

        _currentFireColorTarget = target;
        var changed = gauge.TryMoveLastToFront(out movedColor);
        if (changed)
            Flash();

        return changed;
    }

    public async Task<IReadOnlyList<YalisalinFireColorSegment>> ConsumeFireColor(
        PlayerChoiceContext choiceContext,
        Creature target,
        int amount,
        CardModel? source = null)
    {
        var result = await ConsumeFireColorDetailed(choiceContext, target, amount, source);
        return result.Consumed;
    }

    public async Task<YalisalinFireColorConsumeResult> ConsumeFireColorDetailed(
        PlayerChoiceContext choiceContext,
        Creature target,
        int amount,
        CardModel? source = null)
    {
        if (!_gauges.TryGetValue(target, out var gauge) || amount <= 0)
            return YalisalinFireColorConsumeResult.Empty;

        _currentFireColorTarget = target;
        var result = gauge.Consume(amount, PreserveHighestFireColor);
        if (result.PreservedHighest.Count > 0)
        {
            PreserveHighestFireColor = Math.Max(0, PreserveHighestFireColor - result.PreservedHighest.Count);
            await ResolvePreservedHighestFireColor(choiceContext, result.PreservedHighest, source);
        }

        if (result.Consumed.Count > 0)
        {
            await ResolveFireColorConsumedRewards(choiceContext, result.Consumed, source);
            Flash();
        }

        return result;
    }

    public async Task ResolveExtraFireColorReward(
        PlayerChoiceContext choiceContext,
        YalisalinFireColor color,
        CardModel? source = null)
    {
        await ResolveSingleFireColorConsumedReward(choiceContext, color, source, countPairs: false, recordConsumption: false);
        Flash();
    }

    public IReadOnlyList<YalisalinFireColorSegment> GetFireColorSegments(Creature target)
    {
        return _gauges.TryGetValue(target, out var gauge) ? gauge.Segments : [];
    }

    private async Task ResolveFireColorTurnStartRewards(PlayerChoiceContext choiceContext)
    {
        var target = GetCurrentFireColorRewardTarget();
        if (target == null)
            return;

        var colors = GetFireColorSegments(target)
            .Select(segment => segment.Color)
            .ToHashSet();
        if (colors.Count == 0)
            return;

        var resolvedAny = false;
        if (colors.Contains(YalisalinFireColor.LightOrange))
            resolvedAny |= await TryChooseFireColorCardToHand(choiceContext);

        if (colors.Contains(YalisalinFireColor.BrightYellow))
        {
            await PlayerCmd.GainEnergy(1, Owner);
            resolvedAny = true;
        }

        if (colors.Contains(YalisalinFireColor.Red))
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
            await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
            resolvedAny = true;
        }

        if (resolvedAny)
            Flash();
    }

    private async Task<bool> TryChooseFireColorCardToHand(PlayerChoiceContext choiceContext)
    {
        var options = new[] { PileType.Draw, PileType.Discard }
            .SelectMany(pileType => pileType.GetPile(Owner).Cards)
            .Where(card => !card.HasBeenRemovedFromState)
            .Distinct()
            .ToList();

        if (options.Count == 0)
            return false;

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            options,
            Owner,
            new CardSelectorPrefs(
                new LocString("relics", $"{Id.Entry}.fireColor.selectionScreenPrompt"),
                0,
                1)
            {
                Cancelable = true
            })).FirstOrDefault();

        if (selected == null)
            return false;

        await CardPileCmd.Add(selected, PileType.Hand);
        return true;
    }

    private Creature? GetCurrentFireColorRewardTarget()
    {
        if (_currentFireColorTarget != null
            && CanTrack(_currentFireColorTarget)
            && GetFireColorSegments(_currentFireColorTarget).Count > 0)
            return _currentFireColorTarget;

        return _gauges
            .Where(pair => CanTrack(pair.Key) && pair.Value.Count > 0)
            .Select(pair => pair.Key)
            .FirstOrDefault();
    }

    private async Task ResolveFireColorConsumedRewards(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<YalisalinFireColorSegment> consumed,
        CardModel? source)
    {
        foreach (var segment in consumed.OrderBy(segment => segment.Order))
            await ResolveSingleFireColorConsumedReward(
                choiceContext,
                segment.Color,
                source,
                countPairs: true,
                recordConsumption: true);
    }

    private async Task ResolveSingleFireColorConsumedReward(
        PlayerChoiceContext choiceContext,
        YalisalinFireColor color,
        CardModel? source,
        bool countPairs,
        bool recordConsumption)
    {
        if (recordConsumption)
            await RecordActualFireColorConsumption(choiceContext, color, source);

        switch (color)
        {
            case YalisalinFireColor.LightOrange:
                await CreatureCmd.GainBlock(Owner.Creature, OrangeConsumeBlock, ValueProp.Move, cardPlay: null);
                if (countPairs && AdvanceOrangePairCounter())
                    await CardPileCmd.Draw(choiceContext, 1, Owner);
                break;
            case YalisalinFireColor.BrightYellow:
                PendingYellowCostReduction++;
                await ApplyYellowNextTurnEnergy(choiceContext, 1, source);
                if (countPairs && AdvanceYellowPairCounter())
                    await PlayerCmd.GainEnergy(1, Owner);
                break;
            case YalisalinFireColor.Red:
                await ResolveSingleRedFireColorConsumedReward(choiceContext, source, countPairs);
                break;
            case YalisalinFireColor.BlackRed:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(color), color, null);
        }
    }

    private async Task RecordActualFireColorConsumption(
        PlayerChoiceContext choiceContext,
        YalisalinFireColor color,
        CardModel? source)
    {
        if (MixedConclusionEnabled
            && !MixedConclusionUsedThisTurn
            && HasLastConsumedFireColorThisTurn
            && LastConsumedFireColorThisTurn != color)
        {
            MixedConclusionUsedThisTurn = true;
            GrantSealedFire(color);
            await CardPileCmd.Draw(choiceContext, 1, Owner);
        }

        LastConsumedFireColorThisTurn = color;
        HasLastConsumedFireColorThisTurn = true;
    }

    private async Task ResolvePreservedHighestFireColor(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<YalisalinFireColorSegment> preserved,
        CardModel? source)
    {
        foreach (var segment in preserved)
        {
            if (PreserveHighestRewriteEnergyEnabled && !PreserveHighestRewriteEnergyUsedThisTurn)
            {
                PreserveHighestRewriteEnergyUsedThisTurn = true;
                await PlayerCmd.GainEnergy(1, Owner);
            }

            if (ThirteenthListeningEnabled && !ThirteenthRewriteUsedThisTurn)
            {
                ThirteenthRewriteUsedThisTurn = true;
                GrantSealedFire(segment.Color);
            }
        }
    }

    private void AfterTargetFireColorFilledToFull()
    {
        if (!FullRefillPreserveEnabled || FullRefillPreserveUsedThisTurn)
            return;

        FullRefillPreserveUsedThisTurn = true;
        GainPreserveHighestFireColor(1);
    }

    private bool AdvanceOrangePairCounter()
    {
        OrangeConsumePairProgress++;
        if (OrangeConsumePairProgress < 2)
            return false;

        OrangeConsumePairProgress -= 2;
        return true;
    }

    private bool AdvanceYellowPairCounter()
    {
        YellowConsumePairProgress++;
        if (YellowConsumePairProgress < 2)
            return false;

        YellowConsumePairProgress -= 2;
        return true;
    }

    private bool AdvanceRedPairCounter()
    {
        RedConsumePairProgress++;
        if (RedConsumePairProgress < 2)
            return false;

        RedConsumePairProgress -= 2;
        return true;
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

    private async Task ResolveSingleRedFireColorConsumedReward(
        PlayerChoiceContext choiceContext,
        CardModel? source,
        bool countPairs)
    {
        var damage = RedConsumeDamage;
        var damagedEnemies = await DealRedFireColorDamage(choiceContext, damage, source);
        foreach (var enemy in damagedEnemies.Where(static enemy => enemy.IsAlive).Distinct())
            await PowerCmd.Apply<YlsmPower>(choiceContext, enemy, damage, Owner.Creature, source, false);

        RedConsumeDamage++;

        if (countPairs && AdvanceRedPairCounter())
        {
            await DealRedFireColorDamage(choiceContext, damage, source);
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, source, false);
        }
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

    private bool CanApplyPendingYellowCostReduction(CardModel card)
    {
        if (card.Owner != Owner || card.EnergyCost.CostsX)
            return false;

        return card.Pile?.Type is PileType.Hand or PileType.Play;
    }

    private bool TryGetSealedFireColorToCopy(out YalisalinFireColor color)
    {
        if (SealedBlackRed > 0)
        {
            color = YalisalinFireColor.BlackRed;
            return true;
        }

        if (SealedRed > 0)
        {
            color = YalisalinFireColor.Red;
            return true;
        }

        if (SealedBrightYellow > 0)
        {
            color = YalisalinFireColor.BrightYellow;
            return true;
        }

        if (SealedLightOrange > 0)
        {
            color = YalisalinFireColor.LightOrange;
            return true;
        }

        color = default;
        return false;
    }

    private bool TrySpendSealedFire(YalisalinFireColor color)
    {
        switch (color)
        {
            case YalisalinFireColor.LightOrange when SealedLightOrange > 0:
                SealedLightOrange--;
                return true;
            case YalisalinFireColor.BrightYellow when SealedBrightYellow > 0:
                SealedBrightYellow--;
                return true;
            case YalisalinFireColor.Red when SealedRed > 0:
                SealedRed--;
                return true;
            case YalisalinFireColor.BlackRed when SealedBlackRed > 0:
                SealedBlackRed--;
                return true;
            default:
                return false;
        }
    }

    private bool IsYalisalinCardDamage(
        Creature target,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner.Creature)
            return false;

        if (target.Side == Owner.Creature.Side)
            return false;

        return cardSource?.Owner == Owner;
    }

    private bool CanTrack(Creature target)
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
    Red,
    BlackRed
}

public readonly record struct YalisalinFireColorSegment(YalisalinFireColor Color, long Order)
{
    public Color DisplayColor => Color switch
    {
        YalisalinFireColor.LightOrange => new Color("#c4631c"),
        YalisalinFireColor.BrightYellow => new Color("#7f1d1d"),
        YalisalinFireColor.Red => new Color("#dc143c"),
        YalisalinFireColor.BlackRed => new Color("#2a0707"),
        _ => Colors.White
    };
}

public readonly record struct YalisalinFireColorConsumeResult(
    IReadOnlyList<YalisalinFireColorSegment> Consumed,
    IReadOnlyList<YalisalinFireColorSegment> PreservedHighest)
{
    public static YalisalinFireColorConsumeResult Empty { get; } = new([], []);
}

internal readonly record struct BringHomePendingCard(int Energy, int Draw);

internal sealed class YalisalinFireColorGauge
{
    private readonly List<YalisalinFireColorSegment> _segments = [];

    public int Count => _segments.Count;
    public bool IsFull => _segments.Count >= YalisalinsHairpin.MaxSegments;
    public IReadOnlyList<YalisalinFireColorSegment> Segments => OrderedSegments().ToArray();

    public bool AddLightOrange(ref long conversionSequence)
    {
        if (IsFull)
            return false;

        _segments.Add(new YalisalinFireColorSegment(
            YalisalinFireColor.LightOrange,
            ++conversionSequence));

        return true;
    }

    public bool TryPromoteOnce(bool canCarbonize, ref long conversionSequence)
    {
        if (!IsFull)
            return false;

        var lowestColor = _segments.Min(segment => segment.Color);
        return TryPromoteColor(lowestColor, canCarbonize, ref conversionSequence);
    }

    public bool TryStrongPromoteOnce(bool canCarbonize, ref long conversionSequence)
    {
        if (_segments.Count == 0)
            return false;

        var highestColor = _segments.Max(segment => segment.Color);
        return TryPromoteColor(highestColor, canCarbonize, ref conversionSequence);
    }

    public bool TryGetEarliestColor(out YalisalinFireColor color)
    {
        var earliest = OrderedSegments().FirstOrDefault();
        color = earliest.Color;
        return _segments.Count > 0;
    }

    public bool TryGetHighestColor(out YalisalinFireColor color)
    {
        color = default;
        if (_segments.Count == 0)
            return false;

        color = _segments.Max(segment => segment.Color);
        return true;
    }

    public bool TryDowngradeEarliest(out YalisalinFireColor originalColor)
    {
        originalColor = default;
        var earliest = OrderedSegments().FirstOrDefault();
        if (_segments.Count == 0 || earliest.Color == YalisalinFireColor.LightOrange)
            return false;

        var index = _segments.IndexOf(earliest);
        if (index < 0)
            return false;

        originalColor = earliest.Color;
        _segments[index] = new YalisalinFireColorSegment(GetPreviousColor(earliest.Color), earliest.Order);
        return true;
    }

    public bool TryMoveLastToFront(out YalisalinFireColor movedColor)
    {
        movedColor = default;
        var ordered = OrderedSegments().ToArray();
        if (ordered.Length <= 1)
            return false;

        var latest = ordered[^1];
        var index = _segments.IndexOf(latest);
        if (index < 0)
            return false;

        movedColor = latest.Color;
        _segments[index] = new YalisalinFireColorSegment(latest.Color, ordered[0].Order - 1);
        return true;
    }

    public void InsertColor(YalisalinFireColor color, int slotIndex, ref long conversionSequence)
    {
        var ordered = OrderedSegments().ToList();
        var index = Math.Clamp(slotIndex, 0, ordered.Count);
        ordered.Insert(index, new YalisalinFireColorSegment(color, 0));

        if (ordered.Count > YalisalinsHairpin.MaxSegments)
            ordered.RemoveAt(ordered.Count - 1);

        _segments.Clear();
        foreach (var segment in ordered)
            _segments.Add(new YalisalinFireColorSegment(segment.Color, ++conversionSequence));
    }

    public YalisalinFireColorConsumeResult Consume(int amount, int preserveHighestLayers)
    {
        List<YalisalinFireColorSegment> consumed = [];
        List<YalisalinFireColorSegment> preserved = [];

        for (var i = 0; i < amount && _segments.Count > 0; i++)
        {
            var ordered = OrderedSegments().ToArray();
            var next = ordered[0];
            var highestColor = ordered.Max(segment => segment.Color);
            if (preserveHighestLayers > preserved.Count
                && next.Color == highestColor
                && TryConsumeTwoLowerColors(highestColor, consumed))
            {
                preserved.Add(next);
                continue;
            }

            _segments.Remove(next);
            consumed.Add(next);
        }

        return new YalisalinFireColorConsumeResult(consumed, preserved);
    }

    private bool TryPromoteColor(YalisalinFireColor color, bool canCarbonize, ref long conversionSequence)
    {
        var nextColor = GetNextColor(color, canCarbonize);
        if (nextColor == null)
            return false;

        var changed = false;
        for (var i = 0; i < _segments.Count; i++)
        {
            if (_segments[i].Color != color)
                continue;

            _segments[i] = new YalisalinFireColorSegment(nextColor.Value, ++conversionSequence);
            changed = true;
        }

        return changed;
    }

    private bool TryConsumeTwoLowerColors(
        YalisalinFireColor highestColor,
        List<YalisalinFireColorSegment> consumed)
    {
        var lower = OrderedSegments()
            .Where(segment => segment.Color < highestColor)
            .OrderBy(segment => segment.Color)
            .ThenBy(segment => segment.Order)
            .Take(2)
            .ToArray();

        if (lower.Length < 2)
            return false;

        foreach (var segment in lower)
        {
            _segments.Remove(segment);
            consumed.Add(segment);
        }

        return true;
    }

    private IEnumerable<YalisalinFireColorSegment> OrderedSegments()
    {
        return _segments.OrderBy(segment => segment.Order);
    }

    private static YalisalinFireColor GetPreviousColor(YalisalinFireColor color)
    {
        return color switch
        {
            YalisalinFireColor.BrightYellow => YalisalinFireColor.LightOrange,
            YalisalinFireColor.Red => YalisalinFireColor.BrightYellow,
            YalisalinFireColor.BlackRed => YalisalinFireColor.Red,
            _ => color
        };
    }

    private static YalisalinFireColor? GetNextColor(YalisalinFireColor color, bool canCarbonize)
    {
        return color switch
        {
            YalisalinFireColor.LightOrange => YalisalinFireColor.BrightYellow,
            YalisalinFireColor.BrightYellow => YalisalinFireColor.Red,
            YalisalinFireColor.Red when canCarbonize => YalisalinFireColor.BlackRed,
            _ => null
        };
    }
}
