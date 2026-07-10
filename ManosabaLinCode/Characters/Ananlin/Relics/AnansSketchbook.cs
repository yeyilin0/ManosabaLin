using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Relics;

[RegisterRelic(typeof(AnanlinRelicPool))]
[RegisterCharacterStarterRelic(typeof(Ananlin))]
public sealed class AnansSketchbook : ManosabaRelicTemplate
{
    private const int MaxRecordedPools = 3;
    private const int AncientRewardChoices = 3;

    [SavedProperty] public string RecordedPool1 { get; set; } = string.Empty;
    [SavedProperty] public string RecordedPool2 { get; set; } = string.Empty;
    [SavedProperty] public string RecordedPool3 { get; set; } = string.Empty;

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => RecordedPoolEntries.Count > 0;
    public override int DisplayAmount => RecordedPoolEntries.Count;

    internal IReadOnlyList<string> RecordedPoolEntries =>
        new[] { RecordedPool1, RecordedPool2, RecordedPool3 }
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .Take(MaxRecordedPools)
            .ToArray();

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        await PowerCmd.Apply<SilentPower>(choiceContext, Owner.Creature, 1, Owner.Creature, null);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner) return;

        if (cardPlay.Card.Type == CardType.Skill)
        {
            await PowerCmd.Apply<SilentPower>(choiceContext, Owner.Creature, 2, Owner.Creature, cardPlay.Card);
            return;
        }

        if (cardPlay.Card.Type != CardType.Attack) return;

        var silence = Owner.Creature.GetPower<SilentPower>();
        if (silence is null || silence.Amount <= 0) return;

        await PowerCmd.ModifyAmount(choiceContext, silence, -1, Owner.Creature, cardPlay.Card);
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, 1, Owner.Creature, cardPlay.Card);
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner) return false;
        if (room is not EventRoom { CanonicalEvent: AncientEventModel }) return false;

        var reward = CreateAncientRecordReward();
        if (reward is null) return false;

        rewards.Add(reward);
        return true;
    }

    public override Task AfterRewardTaken(Player player, Reward reward)
    {
        if (player != Owner) return Task.CompletedTask;
        if (AnansSketchbookRewardTracker.TryRecordSelectedPool(reward, this))
            InvokeDisplayAmountChanged();

        return Task.CompletedTask;
    }

    internal async Task TriggerSilenceRewrite(PlayerChoiceContext choiceContext)
    {
        Flash();
        await AnanlinSilenceIntentManager.Trigger(choiceContext, Owner);
    }

    internal async Task UseBlankPage(PlayerChoiceContext choiceContext, CardModel source)
    {
        var recordedPools = GetRecordedPools().ToArray();
        if (recordedPools.Length == 0) return;

        var rng = Owner.RunState.Rng.CombatCardGeneration;
        if (recordedPools.Length < MaxRecordedPools)
        {
            var pool = rng.NextItem(recordedPools);
            var card = RollCombatCardFromPool(pool, rng);
            if (card is not null)
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
            return;
        }

        var options = recordedPools
            .Select(pool => RollCombatCardFromPool(pool, rng))
            .OfType<CardModel>()
            .ToList();
        if (options.Count == 0) return;

        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            options,
            Owner,
            new CardSelectorPrefs(source.SelectionScreenPrompt, 1, 1));

        foreach (var card in selected)
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
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

    private CardReward? CreateAncientRecordReward()
    {
        var ananlinPoolId = ModelDb.GetId(typeof(AnanlinCardPool));
        var existing = RecordedPoolEntries.ToHashSet();
        var rng = Owner.RunState.Rng.UpFront;

        var candidates = Owner.UnlockState.CharacterCardPools
            .Where(pool => pool.Id != ananlinPoolId && !existing.Contains(pool.Id.Entry))
            .Select(pool => (pool, cards: GetRecordableCards(pool).ToArray()))
            .Where(static entry => entry.cards.Length > 0)
            .OrderBy(_ => rng.NextFloat())
            .Take(AncientRewardChoices)
            .Select(entry => (entry.pool, card: Owner.RunState.CreateCard(rng.NextItem(entry.cards), Owner)))
            .ToArray();

        if (candidates.Length == 0) return null;

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

    private IEnumerable<CardPoolModel> GetRecordedPools()
    {
        foreach (var entry in RecordedPoolEntries)
            if (ModelDb.GetByIdOrNull<CardPoolModel>(new ModelId("CARD_POOL", entry)) is { } pool)
                yield return pool;
    }

    private CardModel? RollCombatCardFromPool(CardPoolModel pool, MegaCrit.Sts2.Core.Random.Rng rng)
    {
        var cards = GetRecordableCards(pool).ToArray();
        if (cards.Length == 0) return null;
        return Owner.Creature.CombatState.CreateCard(rng.NextItem(cards), Owner);
    }

    private IEnumerable<CardModel> GetRecordableCards(CardPoolModel pool)
    {
        return pool
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(IsRecordableCard);
    }

    private static bool IsRecordableCard(CardModel card)
    {
        return card.Rarity != CardRarity.Basic
            && card.Rarity != CardRarity.Ancient
            && card.Rarity != CardRarity.Event
            && card.Rarity != CardRarity.Token
            && card.CanBeGeneratedInCombat;
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
