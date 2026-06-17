using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MinionLib.Component.Core;
using System.Collections.Generic;
using System.Linq;
using MinionLib.Component;

namespace ManosabaLin.Characters.Sherrylin.Components;

public sealed partial class EmptyHouseComponent : CardComponent
{
    private readonly List<CardModel> _markedCards = new();

    public IReadOnlyList<CardModel> MarkedCards => _markedCards;

    public void ClearMarkedCards() => _markedCards.Clear();

    public override async Task BeforeSideTurnEndPostfix(
        PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants, ComponentContext componentContext)
    {
        if (Card?.Owner?.Creature is not { } creature || side != creature.Side) return;
        if (Card.Pile?.Type != PileType.Hand) return;

        var player = Card.Owner;
        var exhaustPile = PileType.Exhaust.GetPile(player);
        if (exhaustPile.Cards.Count == 0) return;

        var rng = player.RunState.Rng.CombatCardSelection;
        var target = exhaustPile.Cards[rng.NextInt(exhaustPile.Cards.Count)];

        if (!_markedCards.Contains(target))
            _markedCards.Add(target);
    }
}