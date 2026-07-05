// GuardThreeCombatSingleton.cs
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Cards.Transforms;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Ema.Afflictions;
using ManosabaLin.Characters.Hiro.Powers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Hiro.Monsters;

[RegisterSingleton]
public sealed class GuardThreeCombatSingleton : SingletonModel
{
    private const int MaxHpLossPerPlayer = 50;
    private const int IntelEndTurnThreshold = 5;
    private static bool _transformListenerRegistered;
    private bool _isProcessingResurrection;
    private readonly Dictionary<ulong, int> _intelGainedThisTurn = new();
    private bool _intelEndTurnTriggered;

    public static GuardThreeCombatSingleton? Instance { get; private set; }
    public decimal PreviousMaxHp { get; private set; }

    public GuardThreeCombatSingleton()
    {
        Instance = this;
        ModHelper.SubscribeForCombatStateHooks(Id.Entry, CombatSubModels);
        RegisterTransformListener();
    }

    public override bool ShouldReceiveCombatHooks => true;

    private IEnumerable<AbstractModel> CombatSubModels(CombatState _)
    {
        return [this];
    }

    public override bool ShouldDie(Creature creature)
    {
        return true;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        var justice = creature.GetPower<UncontrolledJusticePower>();
        if (justice == null) return;

        await HandlePhaseOneResurrection(creature, justice);
    }

    // 情报强制结束回合
    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        _intelGainedThisTurn.Clear();
        _intelEndTurnTriggered = false;
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is not ThirteenWaterPlayerIntelPower) return Task.CompletedTask;
        if (amount <= 0) return Task.CompletedTask;

        var player = power.Owner?.Player;
        if (player == null) return Task.CompletedTask;

        if (!_intelGainedThisTurn.ContainsKey(player.NetId))
            _intelGainedThisTurn[player.NetId] = 0;
        _intelGainedThisTurn[player.NetId] += (int)amount;

        if (_intelEndTurnTriggered) return Task.CompletedTask;
        if (_intelGainedThisTurn[player.NetId] < IntelEndTurnThreshold) return Task.CompletedTask;

        _intelEndTurnTriggered = true;
        foreach (var p in player.Creature.CombatState.Players.Where(p => p.Creature.IsAlive))
            PlayerCmd.EndTurn(p, false);

        return Task.CompletedTask;
    }

    public static async Task HandlePhaseOneResurrection(Creature creature, UncontrolledJusticePower justice)
    {
        var instance = Instance;
        if (instance == null) return;

        if (creature.Monster is not GuardThreeMonster monster) return;

        if (justice.Owner != creature) return;

        if (instance._isProcessingResurrection) return;
        instance._isProcessingResurrection = true;

        if (monster.MoveStateMachine?.States.TryGetValue("PHASE2_ATTACK", out var move) == true &&
            move is MoveState moveState)
        {
            monster.SetMoveImmediate(moveState, forceTransition: true);
        }

        int playerCount = creature.CombatState.Players.Count();
        int hpLoss = MaxHpLossPerPlayer * playerCount;
        var newMaxHp = monster.MaxInitialHp - hpLoss;
        creature.MaxHp = (int)Math.Min(newMaxHp, 999999999M);
        creature.CurrentHp = Math.Min(creature.CurrentHp, creature.MaxHp);
        await CreatureCmd.SetCurrentHp(creature, creature.MaxHp);
        instance.PreviousMaxHp = newMaxHp;

        foreach (var player in creature.CombatState.Players)
        {
            foreach (var power in player.Creature.Powers.ToList())
            {
                if (power.Type == PowerType.Debuff)
                    await PowerCmd.Remove(power);
            }

            foreach (var card in CombatCards(player).Where(c => c.Affliction is ErosionAffliction).ToList())
                CardCmd.ClearAffliction(card);
        }

        var intelPower = await PowerCmd.Apply<ThirteenWaterIntelPower>(
            new ThrowingPlayerChoiceContext(), creature, 1, creature, null);
        intelPower?.InitializePreviousMaxHp(newMaxHp);

        if (creature.GetPower<FusionStandPower>() == null)
        {
            await PowerCmd.Apply<FusionStandPower>(
                new ThrowingPlayerChoiceContext(), creature, 1, creature, null);
        }

        await PowerCmd.Remove(justice);
        instance._isProcessingResurrection = false;

        await monster.EnterPhaseTwo();
    }

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Affliction is not ErosionAffliction) return;
        if (CombatManager.Instance.IsEnding) return;

        await CardPileCmd.Add(card, PileType.Hand);
    }

    private static void RegisterTransformListener()
    {
        if (_transformListenerRegistered) return;
        _transformListenerRegistered = true;

        ModCardTransformRegistry.For("ManosabaLin").Register(
            "GuardThree_ErosionPreventTransform",
            async context =>
            {
                if (context.Original.Affliction is not ErosionAffliction) return;
                if (context.Original.CombatState == null) return;

                context.Replacement.RemoveFromCurrentPile();

                if (context.Replacement.Affliction is ErosionAffliction)
                    CardCmd.ClearAffliction(context.Replacement);

                await CardPileCmd.Add(context.Original, PileType.Hand);

                if (context.Original.Affliction == null)
                {
                    var erosion = (AfflictionModel)ModelDb.Get(typeof(ErosionAffliction)).MutableClone();
                    await CardCmd.Afflict(erosion, context.Original, 1m);
                }
            });
    }

    private static IEnumerable<CardModel> CombatCards(Player player)
    {
        return new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust }
            .SelectMany(pile => pile.GetPile(player).Cards);
    }
}
