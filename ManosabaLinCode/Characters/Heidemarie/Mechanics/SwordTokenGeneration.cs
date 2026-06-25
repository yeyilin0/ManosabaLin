using MegaCrit.Sts2.Core.Entities.Cards;

namespace ManosabaLin.Characters.Heidemarie.Mechanics;

public enum SwordTokenKind
{
    Aurora,
    Crimson
}

public interface ISwordGraveCard;

public interface IAuroraSwordCard : ISwordGraveCard;

public interface ICrimsonSwordCard : ISwordGraveCard;

public interface ISwordGenerationListener
{
    Task OnSwordGenerationRequested(PlayerChoiceContext choiceContext, SwordGenerationRequest request)
    {
        return Task.CompletedTask;
    }

    Task OnSwordGenerationSucceeded(
        PlayerChoiceContext choiceContext,
        SwordGenerationRequest request,
        SwordGenerationResult result)
    {
        return Task.CompletedTask;
    }
}

public sealed record SwordGenerationBatch(SwordTokenKind Kind, int Count);

public sealed class SwordGenerationRequest
{
    private readonly List<SwordGenerationBatch> _batches;

    public Player Owner { get; }
    public Player Creator { get; }
    public SwordTokenKind OriginalKind { get; }
    public int OriginalCount { get; }
    public PileType TargetPile { get; }
    public CardPilePosition Position { get; }
    public AbstractModel? Source { get; }
    public Action<CardModel>? ConfigureGeneratedCard { get; }

    public IReadOnlyList<SwordGenerationBatch> Batches => _batches;

    internal SwordGenerationRequest(
        Player owner,
        Player creator,
        SwordTokenKind originalKind,
        int count,
        PileType targetPile,
        CardPilePosition position,
        AbstractModel? source,
        Action<CardModel>? configureGeneratedCard)
    {
        Owner = owner;
        Creator = creator;
        OriginalKind = originalKind;
        OriginalCount = count;
        TargetPile = targetPile;
        Position = position;
        Source = source;
        ConfigureGeneratedCard = configureGeneratedCard;
        _batches = count > 0 ? [new SwordGenerationBatch(originalKind, count)] : [];
    }

    public void ReplaceWith(SwordTokenKind kind, int count)
    {
        _batches.Clear();
        Add(kind, count);
    }

    public void ReplaceWith(IEnumerable<SwordGenerationBatch> batches)
    {
        var replacement = batches.ToArray();
        _batches.Clear();
        foreach (var batch in replacement)
            Add(batch.Kind, batch.Count);
    }

    public void Add(SwordTokenKind kind, int count)
    {
        if (count <= 0)
            return;

        _batches.Add(new SwordGenerationBatch(kind, count));
    }
}

public sealed record SwordGeneratedCardResult(SwordTokenKind Kind, CardPileAddResult AddResult)
{
    public bool Success => AddResult.success;
    public CardModel Card => AddResult.cardAdded;
    public bool EnteredHand => Success && Card.Pile?.Type == PileType.Hand;
}

public sealed class SwordGenerationResult
{
    public SwordGenerationRequest Request { get; }
    public IReadOnlyList<SwordGeneratedCardResult> Cards { get; }

    public int SuccessCount => Cards.Count(static r => r.Success);

    public SwordGenerationResult(
        SwordGenerationRequest request,
        IReadOnlyList<SwordGeneratedCardResult> cards)
    {
        Request = request;
        Cards = cards;
    }

    public int SuccessCountFor(SwordTokenKind kind)
    {
        return Cards.Count(r => r.Kind == kind && r.Success);
    }

    public int EnteredHandCountFor(SwordTokenKind kind)
    {
        return Cards.Count(r => r.Kind == kind && r.EnteredHand);
    }
}

public static class SwordTokenGeneration
{
    public delegate CardModel SwordTokenFactory(ICombatState combatState, Player owner);

    private static readonly Dictionary<SwordTokenKind, SwordTokenFactory> Factories = [];

    public static void RegisterTokenFactory(SwordTokenKind kind, SwordTokenFactory factory)
    {
        Factories[kind] = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public static void RegisterTokenCard<TCard>(SwordTokenKind kind)
        where TCard : CardModel
    {
        RegisterTokenFactory(kind, static (combatState, owner) => combatState.CreateCard<TCard>(owner));
    }

    public static CardModel CreateToken(SwordTokenKind kind, ICombatState combatState, Player owner)
    {
        if (!Factories.TryGetValue(kind, out var factory))
            throw new InvalidOperationException(
                $"{kind} sword token factory has not been registered. Register the token card before generating it.");

        var card = factory(combatState, owner);
        if (card.Owner != owner)
            throw new InvalidOperationException(
                $"{kind} sword token factory returned a card with the wrong owner.");

        return card;
    }

    public static Task<SwordGenerationResult> AddAuroraSwordsToHand(
        PlayerChoiceContext choiceContext,
        Player owner,
        int count,
        AbstractModel? source = null,
        Action<CardModel>? configureGeneratedCard = null)
    {
        return AddSwordsToHand(
            choiceContext,
            owner,
            SwordTokenKind.Aurora,
            count,
            source,
            configureGeneratedCard);
    }

    public static Task<SwordGenerationResult> AddCrimsonSwordsToHand(
        PlayerChoiceContext choiceContext,
        Player owner,
        int count,
        AbstractModel? source = null,
        Action<CardModel>? configureGeneratedCard = null)
    {
        return AddSwordsToHand(
            choiceContext,
            owner,
            SwordTokenKind.Crimson,
            count,
            source,
            configureGeneratedCard);
    }

    public static Task<SwordGenerationResult> AddSwordsToHand(
        PlayerChoiceContext choiceContext,
        Player owner,
        SwordTokenKind kind,
        int count,
        AbstractModel? source = null,
        Action<CardModel>? configureGeneratedCard = null)
    {
        return AddSwordsToCombat(
            choiceContext,
            owner,
            creator: owner,
            kind,
            count,
            PileType.Hand,
            CardPilePosition.Bottom,
            source,
            configureGeneratedCard);
    }

    public static async Task<SwordGenerationResult> AddSwordsToCombat(
        PlayerChoiceContext choiceContext,
        Player owner,
        Player creator,
        SwordTokenKind kind,
        int count,
        PileType targetPile,
        CardPilePosition position = CardPilePosition.Bottom,
        AbstractModel? source = null,
        Action<CardModel>? configureGeneratedCard = null)
    {
        if (!targetPile.IsCombatPile())
            throw new InvalidOperationException("Sword tokens can only be generated into combat piles.");

        var combatState = owner.Creature.CombatState;
        var request = new SwordGenerationRequest(
            owner,
            creator,
            kind,
            count,
            targetPile,
            position,
            source,
            configureGeneratedCard);

        if (combatState == null || count <= 0)
            return new SwordGenerationResult(request, []);

        await DispatchRequested(choiceContext, combatState, request);

        var generated = new List<(SwordTokenKind Kind, CardModel Card)>();
        foreach (var batch in request.Batches)
        {
            for (var i = 0; i < batch.Count; i++)
            {
                var card = CreateToken(batch.Kind, combatState, owner);
                request.ConfigureGeneratedCard?.Invoke(card);
                generated.Add((batch.Kind, card));
            }
        }

        if (generated.Count == 0)
            return new SwordGenerationResult(request, []);

        var addResults = await CardPileCmd.AddGeneratedCardsToCombat(
            generated.Select(static g => g.Card),
            request.TargetPile,
            request.Creator,
            request.Position);

        var cardResults = generated.Zip(
                addResults,
                static (generatedCard, addResult) => new SwordGeneratedCardResult(generatedCard.Kind, addResult))
            .ToArray();

        var result = new SwordGenerationResult(request, cardResults);
        if (result.SuccessCount > 0)
            await DispatchSucceeded(choiceContext, combatState, request, result);

        return result;
    }

    private static async Task DispatchRequested(
        PlayerChoiceContext choiceContext,
        ICombatState combatState,
        SwordGenerationRequest request)
    {
        foreach (var listener in OrderedListeners(combatState))
            await Dispatch(choiceContext, listener, l => l.OnSwordGenerationRequested(choiceContext, request));
    }

    private static async Task DispatchSucceeded(
        PlayerChoiceContext choiceContext,
        ICombatState combatState,
        SwordGenerationRequest request,
        SwordGenerationResult result)
    {
        foreach (var listener in OrderedListeners(combatState))
            await Dispatch(choiceContext, listener, l => l.OnSwordGenerationSucceeded(choiceContext, request, result));
    }

    private static IEnumerable<ISwordGenerationListener> OrderedListeners(ICombatState combatState)
    {
        return combatState.IterateHookListeners().OfType<ISwordGenerationListener>().ToArray();
    }

    private static async Task Dispatch(
        PlayerChoiceContext choiceContext,
        ISwordGenerationListener listener,
        Func<ISwordGenerationListener, Task> dispatch)
    {
        var listenerModel = listener as AbstractModel;
        if (listenerModel != null)
            choiceContext.PushModel(listenerModel);

        try
        {
            await dispatch(listener);
            listenerModel?.InvokeExecutionFinished();
        }
        finally
        {
            if (listenerModel != null)
                choiceContext.PopModel(listenerModel);
        }
    }
}
