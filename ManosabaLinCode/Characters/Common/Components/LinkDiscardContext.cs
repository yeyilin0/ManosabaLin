using System.Threading;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Common.Components;

public static class LinkDiscardContext
{
    private static readonly AsyncLocal<Scope?> CurrentScope = new();

    public static IDisposable Begin(IEnumerable<CardModel> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        var scopeCards = new HashSet<CardModel>(ReferenceEqualityComparer.Instance);
        foreach (var card in cards)
            scopeCards.Add(card);

        if (scopeCards.Count == 0)
            return EmptyScope.Instance;

        var scope = new Scope(scopeCards, CurrentScope.Value);
        CurrentScope.Value = scope;
        return new ScopeHandle(scope);
    }

    public static bool IsActiveFor(CardModel? card)
    {
        return Contains(card);
    }

    public static bool Contains(CardModel? card)
    {
        if (card == null)
            return false;

        for (var scope = CurrentScope.Value; scope != null; scope = scope.Parent)
        {
            if (scope.Cards.Contains(card))
                return true;
        }

        return false;
    }

    private sealed class Scope(IReadOnlySet<CardModel> cards, Scope? parent)
    {
        public IReadOnlySet<CardModel> Cards { get; } = cards;
        public Scope? Parent { get; } = parent;
    }

    private sealed class ScopeHandle(Scope scope) : IDisposable
    {
        private Scope? _scope = scope;

        public void Dispose()
        {
            var scope = _scope;
            if (scope == null)
                return;

            if (ReferenceEquals(CurrentScope.Value, scope))
                CurrentScope.Value = scope.Parent;

            _scope = null;
        }
    }

    private sealed class EmptyScope : IDisposable
    {
        public static readonly EmptyScope Instance = new();

        public void Dispose()
        {
        }
    }
}
