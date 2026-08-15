using ManosabaLin.Characters.Ananlin.Capabilities;
using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Relics;

[RegisterRelic(typeof(AnanlinRelicPool))]
[RegisterCharacterStarterRelic(typeof(Ananlin), Order = -10)]
[RegisterTouchOfOrobasRefinement(typeof(BlessedObject))]
public class AnansSketchbook : ManosabaRelicTemplate
{
    internal const int MaxRecordedPools = 3;
    private const int AncientRewardChoices = 3;
    private const string SketchbookLocEntry = "MANOSABA_LIN_RELIC_ANANS_SKETCHBOOK";
    private CardModel? _trackedCard;
    private decimal _trackedDamage;
    private decimal _trackedBlock;

    [SavedProperty] public string RecordedPool1 { get; set; } = string.Empty;
    [SavedProperty] public string RecordedPool2 { get; set; } = string.Empty;
    [SavedProperty] public string RecordedPool3 { get; set; } = string.Empty;
    [SavedProperty] public int LastRecordRewardActIndex { get; set; } = -1;
    [SavedProperty] public bool PeaceLostThisTurn { get; set; }
    [SavedProperty] public int PendingNonAttackRepeatChargesThisTurn { get; set; }

    /// <summary>上一张带安心组件的卡打出后，等待匹配的类型（本回合内有效）。</summary>
    private CardType? _pendingReassuranceMatchType;

    internal int AttacksPlayedThisTurn { get; private set; }
    internal int SkillsPlayedThisTurn { get; private set; }
    internal CardType? LastPlayedCardType { get; private set; }
    internal decimal LastPlayedCardDamage { get; private set; }
    internal decimal LastPlayedCardBlock { get; private set; }
    internal int RecordedPoolCount => RecordedPoolEntries.Count;
    internal bool HasRecordedPools => RecordedPoolCount > 0;
    internal bool HasFullRecordedPools => RecordedPoolCount >= MaxRecordedPools;

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => RecordedPoolEntries.Count > 0;
    public override int DisplayAmount => RecordedPoolEntries.Count;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return new HoverTip(
                new LocString("relics", $"{SketchbookLocEntry}.recordedPools.title"),
                GetRecordedPoolsHoverTipDescription());
        }
    }

    internal IReadOnlyList<string> RecordedPoolEntries =>
        new[] { RecordedPool1, RecordedPool2, RecordedPool3 }
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .Take(MaxRecordedPools)
            .ToArray();

    public override Task BeforeCombatStart()
    {
        AnanlinSilenceIntentManager.ClearForNewCombat();
        PeaceLostThisTurn = false;
        PendingNonAttackRepeatChargesThisTurn = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;

        AttacksPlayedThisTurn = 0;
        SkillsPlayedThisTurn = 0;
        PeaceLostThisTurn = false;
        _pendingReassuranceMatchType = null;
        await OfferFirstCombatRecordReward(choiceContext);
        await AddSilence(choiceContext, 1, null);
        AssignReassuranceMark();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner) return;

        // 安心组件：若上一张打出的是带组件的卡，检查本张类型是否匹配
        if (_pendingReassuranceMatchType is { } pendingType)
        {
            _pendingReassuranceMatchType = null;
            if (cardPlay.Card.Type == pendingType)
            {
                await cardPlay.Card.GainPeaceOfMind(choiceContext);
            }
        }

        // 若本次打出的卡带安心组件：记录待匹配类型，并重新给予一张手牌安心组件
        if (cardPlay.Card.TryGetCapability<AnanlinReassuranceMarkCapability>(out _))
        {
            _pendingReassuranceMatchType = cardPlay.Card.Type;
            AssignReassuranceMark();
        }

        RecordFinishedCard(cardPlay.Card);

        if (cardPlay.Card.Type == CardType.Skill)
        {
            SkillsPlayedThisTurn++;
            await AddSilence(choiceContext, 2, cardPlay.Card);
            return;
        }

        if (cardPlay.Card.Type != CardType.Attack) return;

        AttacksPlayedThisTurn++;
        var spent = await SpendSilence(choiceContext, 1, cardPlay.Card);
        if (spent <= 0) return;

        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, 1, Owner.Creature, cardPlay.Card);
    }

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner.Creature))
        {
            PendingNonAttackRepeatChargesThisTurn = 0;
            _pendingReassuranceMatchType = null;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 随机给予一张手牌【安心组件】：打出后，若下一张打出的牌与其类型相同，获得 1 层【安心】。
    /// 若手牌已有一张带组件的卡，则不重复给予。
    /// </summary>
    private void AssignReassuranceMark()
    {
        var hand = PileType.Hand.GetPile(Owner).Cards;
        if (hand.Count == 0) return;
        if (hand.Any(card => card.TryGetCapability<AnanlinReassuranceMarkCapability>(out _))) return;

        var candidates = hand
            .Where(AnanlinCardHelpers.IsPlayableCombatCard)
            .ToArray();
        if (candidates.Length == 0) return;

        var target = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
        target.GetOrCreateCapability<AnanlinReassuranceMarkCapability>();
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (power is AnanlinPeaceOfMindPower && power.Owner == Owner.Creature && amount < 0)
            PeaceLostThisTurn = true;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner != Owner || card.Type == CardType.Attack)
            return playCount;

        return PendingNonAttackRepeatChargesThisTurn > 0
            ? playCount + PendingNonAttackRepeatChargesThisTurn
            : playCount;
    }

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        if (PendingNonAttackRepeatChargesThisTurn > 0)
            PendingNonAttackRepeatChargesThisTurn = 0;

        Flash();
        return Task.CompletedTask;
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer != Owner.Creature) return Task.CompletedTask;
        if (cardSource?.Owner != Owner) return Task.CompletedTask;

        TrackCard(cardSource);
        _trackedDamage += result.TotalDamage;
        return Task.CompletedTask;
    }

    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != Owner.Creature) return Task.CompletedTask;
        if (cardSource?.Owner != Owner) return Task.CompletedTask;

        TrackCard(cardSource);
        _trackedBlock += amount;
        return Task.CompletedTask;
    }

    public override async Task AfterRemoved()
    {
        await base.AfterRemoved();
        AnansSketchbookRefinementMemory.TryStore(this);
    }

    internal virtual async Task TriggerSilenceRewrite(PlayerChoiceContext choiceContext)
    {
        await TriggerSilenceRewriteAndGetTargets(choiceContext);
    }

    internal virtual async Task<IReadOnlyList<Creature>> TriggerSilenceRewriteAndGetTargets(PlayerChoiceContext choiceContext)
    {
        Flash();
        return await AnanlinSilenceIntentManager.TriggerAndGetTargets(choiceContext, Owner);
    }

    internal bool CanTriggerSilenceRewrite()
    {
        return AnanlinSilenceIntentManager.CanTrigger(Owner);
    }

    internal async Task<IReadOnlyList<CardModel>> UseBlankPage(PlayerChoiceContext choiceContext, CardModel source)
    {
        var recordedPools = GetRecordedPools().ToArray();
        if (recordedPools.Length == 0) return [];

        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var indexPower = Owner.Creature.GetPower<AnanlinIndexPagePower>();
        var baseOptionCount = recordedPools.Length < MaxRecordedPools ? 1 : MaxRecordedPools;
        var optionCount = Math.Max(1, baseOptionCount + (indexPower?.Amount ?? 0));
        var options = RollCombatCardsFromRecordedPools((int)optionCount, null, rng);
        if (options.Count == 0) return [];

        if (source.IsUpgraded)
            foreach (var option in options)
                CardCmd.Upgrade(option);

        IReadOnlyList<CardModel> selected;
        if (options.Count == 1)
        {
            selected = options;
        }
        else
        {
            selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                options,
                Owner,
                new CardSelectorPrefs(source.SelectionScreenPrompt, 1, 1))).ToArray();
        }

        var added = new List<CardModel>();
        foreach (var card in selected)
        {
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
            added.Add(card);
        }

        if (added.Count > 0 && Owner.Creature.GetPower<AnanlinTracingPower>() is { } tracing)
            await tracing.CopyGeneratedCards(choiceContext, added, this);

        if (added.Count > 0)
            await AnanlinBadEnding.TryAddRandomCurseFromPage(choiceContext, Owner, source);

        return added;
    }

    internal async Task<IReadOnlyList<CardModel>> UseMarginPage(PlayerChoiceContext choiceContext, CardModel source)
    {
        var recordedPools = GetRecordedPools().ToArray();
        if (recordedPools.Length == 0) return [];

        var options = RollMarginPageOptions(recordedPools, Owner.RunState.Rng.CombatCardGeneration);
        if (options.Count == 0) return [];

        if (source.IsUpgraded)
            foreach (var option in options)
                CardCmd.Upgrade(option);

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            options,
            Owner,
            new CardSelectorPrefs(source.SelectionScreenPrompt, 0, 1))).ToArray();

        var added = new List<CardModel>();
        foreach (var card in selected)
        {
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
            added.Add(card);
        }

        if (added.Count > 0 && Owner.Creature.GetPower<AnanlinTracingPower>() is { } tracing)
            await tracing.CopyGeneratedCards(choiceContext, added, this);

        if (added.Count > 0)
            await AnanlinBadEnding.TryAddRandomCurseFromPage(choiceContext, Owner, source);

        return added;
    }

    internal Task AddSilence(PlayerChoiceContext choiceContext, int amount, CardModel? source)
    {
        return amount <= 0
            ? Task.CompletedTask
            : PowerCmd.Apply<SilentPower>(choiceContext, Owner.Creature, amount, Owner.Creature, source);
    }

    internal async Task<int> SpendSilence(PlayerChoiceContext choiceContext, int amount, CardModel? source)
    {
        if (amount <= 0) return 0;

        var silence = Owner.Creature.GetPower<SilentPower>();
        if (silence is null || silence.Amount <= 0) return 0;

        var spent = Math.Min(amount, (int)silence.Amount);
        await PowerCmd.ModifyAmount(choiceContext, silence, -spent, Owner.Creature, source);
        return spent;
    }

    internal int CurrentSilence => (int)(Owner.Creature.GetPower<SilentPower>()?.Amount ?? 0);

    internal void QueueNonAttackRepeatThisTurn(int extraPlays = 1)
    {
        PendingNonAttackRepeatChargesThisTurn += Math.Max(0, extraPlays);
    }

    internal bool TryForgetRecordedAttack(Creature target)
    {
        if (!AnanlinSilenceIntentManager.TryForgetRecordedAttack(target))
            return false;

        Flash();
        return true;
    }

    internal bool HasAnyEnemyAttackIntent()
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState is null) return false;

        return combatState.Enemies
            .Where(static enemy => enemy.IsAlive)
            .Any(static enemy => IsAttackIntent(enemy.Monster?.NextMove));
    }

    internal bool IsAttackIntent(Creature? creature)
    {
        return creature?.Monster is { NextMove: { } move } && IsAttackIntent(move);
    }

    internal decimal GetRoomEchoValue(CardType cardType)
    {
        return cardType switch
        {
            CardType.Attack => Math.Floor(LastPlayedCardDamage / 2m),
            CardType.Skill => LastPlayedCardBlock,
            _ => 0m
        };
    }

    internal bool IsFromRecordedPool(CardModel card)
    {
        return RecordedPoolEntries.Contains(card.Pool.Id.Entry);
    }

    internal bool IsPoolRecorded(CardPoolModel pool)
    {
        return RecordedPoolEntries.Contains(pool.Id.Entry);
    }

    internal IReadOnlyList<CardPoolModel> GetRecordedCardPools()
    {
        return GetRecordedPools().ToArray();
    }

    internal IEnumerable<CardModel> GetRecordableCardsFromPool(CardPoolModel pool, CardType? requiredType = null)
    {
        return GetRecordableCards(pool, requiredType);
    }

    internal static bool CanSketchbookGenerate(CardModel card)
    {
        return IsRecordableCard(card);
    }

    internal CardModel? RollCombatCardFromRecordedPools(CardType? requiredType = null)
    {
        var pools = GetRecordedPools().ToArray();
        if (pools.Length == 0) return null;

        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var candidates = pools
            .SelectMany(pool => GetRecordableCards(pool, requiredType))
            .ToArray();

        if (candidates.Length == 0 || Owner.Creature.CombatState is not { } combatState) return null;

        var canonical = rng.NextItem(candidates);
        return canonical is null ? null : combatState.CreateCard(canonical, Owner);
    }

    internal IReadOnlyList<CardModel> RollOneFromEachRecordedPool()
    {
        var rng = Owner.RunState.Rng.CombatCardGeneration;
        return GetRecordedPools()
            .Select(pool => RollCombatCardFromPool(pool, rng))
            .OfType<CardModel>()
            .ToArray();
    }

    internal CardModel? RollDifferentCardFromSamePool(CardModel source, CardType requiredType)
    {
        if (!IsFromRecordedPool(source)) return null;

        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var cards = GetRecordableCards(source.Pool, requiredType)
            .Where(card => card.Id != source.Id)
            .ToArray();
        if (cards.Length == 0) return null;

        if (Owner.Creature.CombatState is not { } combatState) return null;

        var canonical = rng.NextItem(cards);
        return canonical is null ? null : combatState.CreateCard(canonical, Owner);
    }

    internal CardModel? RollHigherRarityCardFromOtherRecordedPool(CardModel source)
    {
        var sourceRarityRank = GetRecordableRarityRank(source.Rarity);
        if (sourceRarityRank is null) return null;

        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var cards = GetRecordedPools()
            .Where(pool => pool.Id != source.Pool.Id)
            .SelectMany(pool => GetRecordableCards(pool, source.Type))
            .Where(card => GetRecordableRarityRank(card.Rarity) > sourceRarityRank)
            .ToArray();
        if (cards.Length == 0 || Owner.Creature.CombatState is not { } combatState) return null;

        var canonical = rng.NextItem(cards);
        return canonical is null ? null : combatState.CreateCard(canonical, Owner);
    }

    internal void CopyUpgradeLevel(CardModel source, CardModel target)
    {
        for (var i = 0; i < source.CurrentUpgradeLevel; i++)
            CardCmd.Upgrade(target);
    }

    internal void CopyVisibleAdditions(CardModel source, CardModel target)
    {
        CopyUpgradeLevel(source, target);

        foreach (var keyword in source.GetKeywordsWithSources(KeywordSources.Local))
            if (!target.Keywords.Contains(keyword))
                target.AddKeyword(keyword);

        if (source.Enchantment is not null && target.Enchantment is null)
            CardCmd.Enchant((EnchantmentModel)source.Enchantment.ClonePreservingMutability(), target, source.Enchantment.Amount);
    }

    internal bool TryRecordPool(CardPoolModel pool)
    {
        var entry = pool.Id.Entry;
        if (RecordedPoolEntries.Contains(entry)) return false;

        if (string.IsNullOrWhiteSpace(RecordedPool1)) RecordedPool1 = entry;
        else if (string.IsNullOrWhiteSpace(RecordedPool2)) RecordedPool2 = entry;
        else if (string.IsNullOrWhiteSpace(RecordedPool3)) RecordedPool3 = entry;
        else return false;

        InvokeDisplayAmountChanged();
        return true;
    }

    internal bool TryRecordPoolWithFeedback(CardPoolModel pool)
    {
        if (!TryRecordPool(pool)) return false;

        Flash();
        return true;
    }

    internal bool TryForgetRecordedPool(CardPoolModel pool)
    {
        var entry = pool.Id.Entry;
        var removed = false;

        if (RecordedPool1 == entry)
        {
            RecordedPool1 = string.Empty;
            removed = true;
        }

        if (RecordedPool2 == entry)
        {
            RecordedPool2 = string.Empty;
            removed = true;
        }

        if (RecordedPool3 == entry)
        {
            RecordedPool3 = string.Empty;
            removed = true;
        }

        if (!removed) return false;

        Flash();
        InvokeDisplayAmountChanged();
        return true;
    }

    private IReadOnlyList<(CardPoolModel Pool, CardModel Card)> CreateAncientRecordChoices()
    {
        if (HasFullRecordedPools) return [];

        var ananlinPoolId = ModelDb.GetId(typeof(AnanlinCardPool));
        var existing = RecordedPoolEntries.ToHashSet();
        var rng = Owner.PlayerRng.Rewards;

        var rewardOptions = Owner.UnlockState.CharacterCardPools
            .Where(pool => pool.Id != ananlinPoolId && !existing.Contains(pool.Id.Entry))
            .Select(pool => (pool, cards: GetRecordableCards(pool).ToArray()))
            .Where(static entry => entry.cards.Length > 0)
            .OrderBy(_ => rng.NextFloat())
            .Take(AncientRewardChoices)
            .ToArray();

        var candidates = new List<(CardPoolModel Pool, CardModel Card)>();
        foreach (var (pool, cards) in rewardOptions)
        {
            var canonical = rng.NextItem(cards);
            if (canonical is not null)
                candidates.Add((pool, Owner.RunState.CreateCard(canonical, Owner)));
        }

        return candidates;
    }

    private async Task OfferFirstCombatRecordReward(PlayerChoiceContext choiceContext)
    {
        if (HasFullRecordedPools) return;

        var actIndex = RunManager.Instance.State?.CurrentActIndex ?? 0;
        if (LastRecordRewardActIndex == actIndex) return;

        LastRecordRewardActIndex = actIndex;
        var choices = CreateAncientRecordChoices();
        if (choices.Count == 0) return;

        Flash();
        var selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choices.Select(static choice => choice.Card).ToArray(),
            Owner,
            canSkip: true);

        if (selected is null) return;

        var selectedPool = choices
            .Where(choice => choice.Card == selected)
            .Select(static choice => choice.Pool)
            .FirstOrDefault();
        if (selectedPool is not null)
            TryRecordPoolWithFeedback(selectedPool);
    }

    private IEnumerable<CardPoolModel> GetRecordedPools()
    {
        foreach (var entry in RecordedPoolEntries)
            if (ModelDb.GetByIdOrNull<CardPoolModel>(new ModelId("CARD_POOL", entry)) is { } pool)
                yield return pool;
    }

    private string GetRecordedPoolsHoverTipDescription()
    {
        var description = new LocString("relics", $"{SketchbookLocEntry}.recordedPools.description");
        description.Add("Count", RecordedPoolCount);
        description.Add("Max", MaxRecordedPools);

        var poolNames = GetRecordedPools()
            .Select(GetCardPoolDisplayName)
            .ToArray();

        if (poolNames.Length == 0)
            return description.GetFormattedText()
                + "\n"
                + new LocString("relics", $"{SketchbookLocEntry}.recordedPools.empty").GetFormattedText();

        return description.GetFormattedText()
            + "\n"
            + string.Join("\n", poolNames.Select(static name => $"- {name}"));
    }

    private static string GetCardPoolDisplayName(CardPoolModel pool)
    {
        var character = ModelDb.AllCharacters.FirstOrDefault(character => character.CardPool.Id == pool.Id);
        if (character is not null)
            return character.Title.GetFormattedText();

        if (LocString.Exists("characters", $"{pool.Title}.title"))
            return new LocString("characters", $"{pool.Title}.title").GetFormattedText();

        return pool.Title;
    }

    private static bool IsAttackIntent(MoveState? move)
    {
        return move?.Intents.Any(static intent => intent is AttackIntent) == true;
    }

    private void TrackCard(CardModel card)
    {
        if (_trackedCard == card) return;

        _trackedCard = card;
        _trackedDamage = 0m;
        _trackedBlock = 0m;
    }

    private void RecordFinishedCard(CardModel card)
    {
        if (_trackedCard != card)
        {
            _trackedCard = card;
            _trackedDamage = 0m;
            _trackedBlock = 0m;
        }

        LastPlayedCardType = card.Type;
        LastPlayedCardDamage = _trackedDamage;
        LastPlayedCardBlock = _trackedBlock;
        _trackedCard = null;
        _trackedDamage = 0m;
        _trackedBlock = 0m;
    }

    private List<CardModel> RollCombatCardsFromRecordedPools(
        int count,
        CardType? requiredType,
        MegaCrit.Sts2.Core.Random.Rng rng)
    {
        var pools = GetRecordedPools().ToArray();
        var options = new List<CardModel>();
        var usedIds = new HashSet<ModelId>();
        var attempts = Math.Max(count * 6, 12);
        if (pools.Length == 0) return options;

        while (options.Count < count && attempts-- > 0)
        {
            var pool = rng.NextItem(pools);
            if (pool is null) break;

            var card = RollCombatCardFromPool(pool, rng, requiredType, usedIds);
            if (card is null) continue;

            usedIds.Add(card.Id);
            options.Add(card);
        }

        return options;
    }

    private IReadOnlyList<CardModel> RollMarginPageOptions(
        IReadOnlyList<CardPoolModel> pools,
        MegaCrit.Sts2.Core.Random.Rng rng)
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var selected = new List<CardModel>();
            var usedIds = new HashSet<ModelId>();
            var usedRarities = new HashSet<CardRarity>();
            var rarityOrder = new[] { CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare }
                .OrderBy(_ => rng.NextFloat())
                .ToArray();
            var requiredPools = pools.Count switch
            {
                1 => [pools[0], pools[0], pools[0]],
                2 => new[] { pools[0], pools[1], rng.NextItem(pools) ?? pools[0] },
                _ => pools.Take(MaxRecordedPools).OrderBy(_ => rng.NextFloat()).ToArray()
            };

            foreach (var pool in requiredPools)
            {
                var card = RollMarginPageOption(pool, rarityOrder, usedRarities, usedIds, rng);
                if (card is null) break;

                selected.Add(card);
                usedIds.Add(card.Id);
                usedRarities.Add(card.Rarity);
            }

            if (selected.Count == 3)
                return selected;
        }

        return [];
    }

    private CardModel? RollMarginPageOption(
        CardPoolModel pool,
        IReadOnlyList<CardRarity> rarityOrder,
        ISet<CardRarity> usedRarities,
        HashSet<ModelId> usedIds,
        MegaCrit.Sts2.Core.Random.Rng rng)
    {
        foreach (var rarity in rarityOrder.Where(rarity => !usedRarities.Contains(rarity)))
        {
            var cards = GetRecordableCards(pool)
                .Where(card => card.Rarity == rarity && !usedIds.Contains(card.Id))
                .ToArray();
            if (cards.Length == 0) continue;

            var canonical = rng.NextItem(cards);
            if (canonical is null || Owner.Creature.CombatState is not { } combatState) return null;

            return combatState.CreateCard(canonical, Owner);
        }

        return null;
    }

    private CardModel? RollCombatCardFromPool(
        CardPoolModel pool,
        MegaCrit.Sts2.Core.Random.Rng rng,
        CardType? requiredType = null,
        HashSet<ModelId>? excludedIds = null)
    {
        var cards = GetRecordableCards(pool, requiredType)
            .Where(card => excludedIds?.Contains(card.Id) != true)
            .ToArray();
        if (cards.Length == 0 || Owner.Creature.CombatState is not { } combatState) return null;

        var canonical = rng.NextItem(cards);
        return canonical is null ? null : combatState.CreateCard(canonical, Owner);
    }

    private IEnumerable<CardModel> GetRecordableCards(CardPoolModel pool, CardType? requiredType = null)
    {
        return pool
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(IsRecordableCard)
            .Where(card => requiredType is null || card.Type == requiredType);
    }

    private static bool IsRecordableCard(CardModel card)
    {
        return card.Rarity != CardRarity.Basic
            && card.Rarity != CardRarity.Ancient
            && card.Rarity != CardRarity.Event
            && card.Rarity != CardRarity.Token
            && card.CanBeGeneratedInCombat;
    }

    private static int? GetRecordableRarityRank(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common => 0,
            CardRarity.Uncommon => 1,
            CardRarity.Rare => 2,
            _ => null
        };
    }
}

internal static class AnansSketchbookRefinementMemory
{
    private static readonly Dictionary<Player, Snapshot> PendingSnapshots = [];

    internal static void TryStore(AnansSketchbook sketchbook)
    {
        if (sketchbook.GetType() != typeof(AnansSketchbook)) return;
        if (!IsTouchOfOrobasUpgradeToBlessedObject(sketchbook)) return;

        PendingSnapshots[sketchbook.Owner] = Snapshot.From(sketchbook);
    }

    internal static bool TryRestore(BlessedObject blessedObject)
    {
        if (!PendingSnapshots.Remove(blessedObject.Owner, out var snapshot)) return false;

        snapshot.ApplyTo(blessedObject);
        return true;
    }

    private static bool IsTouchOfOrobasUpgradeToBlessedObject(AnansSketchbook sketchbook)
    {
        var blessedObjectId = ModelDb.GetId(typeof(BlessedObject));
        return sketchbook.Owner.Relics
            .OfType<TouchOfOrobas>()
            .Any(orobas =>
                (orobas.StarterRelic is null || orobas.StarterRelic == sketchbook.Id)
                && (orobas.UpgradedRelic is null || orobas.UpgradedRelic == blessedObjectId));
    }

    private readonly record struct Snapshot(
        string RecordedPool1,
        string RecordedPool2,
        string RecordedPool3,
        int LastRecordRewardActIndex,
        bool PeaceLostThisTurn,
        int PendingNonAttackRepeatChargesThisTurn)
    {
        internal static Snapshot From(AnansSketchbook sketchbook)
        {
            return new Snapshot(
                sketchbook.RecordedPool1,
                sketchbook.RecordedPool2,
                sketchbook.RecordedPool3,
                sketchbook.LastRecordRewardActIndex,
                sketchbook.PeaceLostThisTurn,
                sketchbook.PendingNonAttackRepeatChargesThisTurn);
        }

        internal void ApplyTo(AnansSketchbook sketchbook)
        {
            sketchbook.RecordedPool1 = RecordedPool1;
            sketchbook.RecordedPool2 = RecordedPool2;
            sketchbook.RecordedPool3 = RecordedPool3;
            sketchbook.LastRecordRewardActIndex = LastRecordRewardActIndex;
            sketchbook.PeaceLostThisTurn = PeaceLostThisTurn;
            sketchbook.PendingNonAttackRepeatChargesThisTurn = PendingNonAttackRepeatChargesThisTurn;
        }
    }
}
