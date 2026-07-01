using ManosabaLin.Characters.Common.Components.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MinionLib.Component.Core;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Sherrylin.Components;

public sealed partial class RetainCounterComponent : KeywordLikeComponent
{
    public static IHoverTip[] Tip => GetHoverTip<RetainCounterComponent>();

    public override IEnumerable<IHoverTip> HoverTips => Tip;

    private int _counter = 1;
    private readonly Dictionary<string, decimal> _originalValues = new();
    private bool _stored;

    public int Counter => _counter;

    protected override void OnAttach()
    {
        base.OnAttach();
        if (Card?.Pile != null)
        {
            Card.GiveSingleTurnRetain();
            StoreOriginalValues();
        }
    }

    // 打出时计数清零
    public override Task OnPlayPostfix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        _counter = 0;
        return Task.CompletedTask;
    }

    // 回合结束：保持临时保留，确保原始值已存
    public override Task BeforeSideTurnEndPostfix(
        PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants, ComponentContext componentContext)
    {
        if (Card?.Owner?.Creature is { } creature && side == creature.Side)
        {
            Card.GiveSingleTurnRetain();
            StoreOriginalValues();
        }
        return Task.CompletedTask;
    }

    // 回合开始：加计数，变更数值
    public override async Task AfterPlayerTurnStartEarlyPostfix(
        PlayerChoiceContext choiceContext, Player player, ComponentContext componentContext)
    {
        if (Card?.Owner != player) return;
        if (Card.Pile?.Type != PileType.Hand) return;

        _counter++;

        if (!_stored) return;
        foreach (var entry in Card.DynamicVars)
        {
            if (_originalValues.TryGetValue(entry.Key, out var original) && original > 0)
            {
                entry.Value.BaseValue = original * _counter;
            }
        }
    }

    private void StoreOriginalValues()
    {
        if (_stored || Card == null) return;
        _stored = true;
        _originalValues.Clear();
        foreach (var entry in Card.DynamicVars)
        {
            _originalValues[entry.Key] = entry.Value.BaseValue;
        }
    }
}
