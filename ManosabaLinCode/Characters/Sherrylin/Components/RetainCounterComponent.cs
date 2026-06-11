using ManosabaLin.Characters.Common.Components.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Component.Core;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Sherrylin.Components;

public sealed partial class RetainCounterComponent : KeywordLikeComponent
{
    private int _counter = 1;
    private readonly Dictionary<string, decimal> _originalValues = new();

    public int Counter => _counter;

    protected override void OnAttach()
    {
        base.OnAttach();
        if (Card != null)
        {
            Card.GiveSingleTurnRetain();
            StoreOriginalValues();
        }
    }

    public override async Task AfterPlayerTurnStartEarlyPostfix(
        PlayerChoiceContext choiceContext, Player player, ComponentContext componentContext)
    {
        if (Card?.Owner != player) return;

        _counter++;
        UpdateCardValues();
    }

    public override Task BeforeSideTurnEndPostfix(
        PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants, ComponentContext componentContext)
    {
        if (Card?.Owner?.Creature is { } creature && side == creature.Side)
            Card.GiveSingleTurnRetain();
        return Task.CompletedTask;
    }

    private void StoreOriginalValues()
    {
        if (Card == null) return;
        _originalValues.Clear();

        foreach (var varEntry in Card.DynamicVars)
        {
            _originalValues[varEntry.Key] = varEntry.Value.BaseValue;
        }
    }

    private void UpdateCardValues()
    {
        if (Card == null) return;

        foreach (var varEntry in Card.DynamicVars)
        {
            if (_originalValues.TryGetValue(varEntry.Key, out var original))
            {
                varEntry.Value.BaseValue = original * _counter;
            }
        }
    }
}