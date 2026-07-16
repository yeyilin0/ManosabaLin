using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Relics;

[RegisterRelic(typeof(AnanlinRelicPool))]
[RegisterCharacterStarterRelic(typeof(Ananlin), Order = -10)]
public sealed class AnansSketchbook : ManosabaRelicTemplate
{
    internal const int MaxRecordedPools = 3;
    private const int AncientRewardChoices = 3;
    private CardModel? _trackedCard;
    private decimal _trackedDamage;
    private decimal _trackedBlock;

    [SavedProperty] public string RecordedPool1 { get; set; } = string.Empty;
    [SavedProperty] public string RecordedPool2 { get; set; } = string.Empty;
    [SavedProperty] public string RecordedPool3 { get; set; } = string.Empty;
    [SavedProperty] public int LastRecordRewardActIndex { get; set; } = -1;
    [SavedProperty] public bool PeaceLostThisTurn { get; set; }
    [SavedProperty] public int PendingNonAttackRepeatChargesThisTurn { get; set; }

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
        await OfferFirstCombatRecordReward();
        await AddSilence(choiceContext, 1, null);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner) return;

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
            PendingNonAttackRepeatChargesThisTurn = 0;

        return Task.CompletedTask;
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

    public override async Task AfterRewardTaken(Player player, Reward reward)
    {
        if (player != Owner) return;

        if (AnansSketchbookRewardTracker.TryRecordSelectedPool(reward, this))
            InvokeDisplayAmountChanged();
    }

    internal async Task TriggerSilenceRewrite(PlayerChoiceContext choiceContext)
    {
        Flash();
        await AnanlinSilenceIntentManager.Trigger(choiceContext, Owner);
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
        var genrePower = Owner.Creature.GetPower<AnanlinSpecifiedGenrePower>();
        var preferredType = genrePower?.PreferredType;

        var baseOptionCount = recordedPools.Length < MaxRecordedPools ? 1 : MaxRecordedPools;
        var optionCount = Math.Max(1, baseOptionCount + (indexPower?.Amount ?? 0));
        var options = RollCombatCardsFromRecordedPools((int)optionCount, preferredType, rng);
        if (options.Count == 0 && preferredType is not null)
            options = RollCombatCardsFromRecordedPools((int)optionCount, null, rng);
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

        if (genrePower is not null)
        {
            if (genrePower.Amount <= 1)
                await PowerCmd.Remove(genrePower);
            else
                await PowerCmd.ModifyAmount(choiceContext, genrePower, -1, Owner.Creature, source);
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

        return true;
    }

    internal bool TryRecordPoolWithFeedback(CardPoolModel pool)
    {
        if (!TryRecordPool(pool)) return false;

        Flash();
        InvokeDisplayAmountChanged();
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

    private CardReward? CreateAncientRecordReward()
    {
        if (HasFullRecordedPools) return null;

        var ananlinPoolId = ModelDb.GetId(typeof(AnanlinCardPool));
        var existing = RecordedPoolEntries.ToHashSet();
        var rng = Owner.RunState.Rng.UpFront;

        var rewardOptions = Owner.UnlockState.CharacterCardPools
            .Where(pool => pool.Id != ananlinPoolId && !existing.Contains(pool.Id.Entry))
            .Select(pool => (pool, cards: GetRecordableCards(pool).ToArray()))
            .Where(static entry => entry.cards.Length > 0)
            .OrderBy(_ => rng.NextFloat())
            .Take(AncientRewardChoices)
            .ToArray();

        var candidates = new List<(CardPoolModel pool, CardModel card)>();
        foreach (var (pool, cards) in rewardOptions)
        {
            var canonical = rng.NextItem(cards);
            if (canonical is not null)
                candidates.Add((pool, Owner.RunState.CreateCard(canonical, Owner)));
        }

        if (candidates.Count == 0) return null;

        var rewardCards = candidates.Select(static entry => entry.card).ToArray();
        var rewardPools = candidates.Select(static entry => entry.pool).ToArray();
        var options = CardCreationOptions
            .ForNonCombatWithUniformOdds(rewardPools, c => IsRecordableCard(c))
            .WithFlags(CardCreationFlags.NoRarityModification | CardCreationFlags.NoCardPoolModifications);

        var reward = new CardReward(rewardCards, CardCreationSource.Other, Owner, options)
        {
            CanSkip = true,
            CanReroll = false
        };

        AnansSketchbookRewardTracker.Track(reward, candidates.ToDictionary(static e => e.card.Id.Entry, static e => e.pool.Id.Entry));
        return reward;
    }

    private async Task OfferFirstCombatRecordReward()
    {
        if (HasFullRecordedPools) return;

        var actIndex = RunManager.Instance.State?.CurrentActIndex ?? 0;
        if (LastRecordRewardActIndex == actIndex) return;

        LastRecordRewardActIndex = actIndex;
        var reward = CreateAncientRecordReward();
        if (reward is null) return;

        Flash();
        await RewardsCmd.OfferCustom(Owner, [reward]);
    }

    private IEnumerable<CardPoolModel> GetRecordedPools()
    {
        foreach (var entry in RecordedPoolEntries)
            if (ModelDb.GetByIdOrNull<CardPoolModel>(new ModelId("CARD_POOL", entry)) is { } pool)
                yield return pool;
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

internal static class AnansSketchbookRewardTracker
{
    private static readonly Dictionary<Reward, Dictionary<string, string>> PoolsByReward = [];

    internal static void Track(CardReward reward, Dictionary<string, string> poolsByCardId)
    {
        PoolsByReward[reward] = poolsByCardId;
    }

    internal static bool TryRecordSelectedPool(Reward reward, AnansSketchbook sketchbook)
    {
        if (!PoolsByReward.Remove(reward, out var poolsByCardId)) return false;
        if (reward is not CardReward cardReward) return false;

        var remaining = cardReward.Cards.Select(static c => c.Id.Entry).ToHashSet();
        var selectedCardId = poolsByCardId.Keys.FirstOrDefault(id => !remaining.Contains(id));
        if (selectedCardId is null) return false;

        var poolEntry = poolsByCardId[selectedCardId];
        var pool = ModelDb.GetByIdOrNull<CardPoolModel>(new ModelId("CARD_POOL", poolEntry));
        return pool is not null && sketchbook.TryRecordPool(pool);
    }
}
