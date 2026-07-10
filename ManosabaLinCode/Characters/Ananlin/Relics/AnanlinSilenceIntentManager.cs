using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ManosabaLin.Characters.Ananlin.Relics;

internal static class AnanlinSilenceIntentManager
{
    private const string BuffMoveId = "MANOSABA_LIN_ANANLIN_SILENT_BUFF_MOVE";
    private const int BuffBlock = 8;

    private static readonly FieldInfo? OnPerformField = AccessTools.Field(typeof(MoveState), "_onPerform");
    private static readonly Dictionary<Creature, MoveState> PendingBuffMoves = [];
    private static readonly Dictionary<Creature, HashSet<string>> UsedMovesByPhase = [];
    private static readonly Dictionary<Creature, string> PhaseKeys = [];
    private static readonly Dictionary<MoveState, MoveState> BaseMovesByReplacement = [];

    [ThreadStatic] private static bool _isApplyingReplacement;

    internal static async Task Trigger(PlayerChoiceContext choiceContext, Player owner)
    {
        var combatState = owner.Creature.CombatState;
        if (combatState is null) return;

        var enemies = combatState.Enemies
            .Where(static c => c.IsAlive && c.Monster?.NextMove is not null)
            .ToArray();

        foreach (var enemy in enemies)
        {
            var monster = enemy.Monster;
            var currentMove = ResolveBaseMove(monster.NextMove);
            if (currentMove is null || !CanForceNow(monster, currentMove)) continue;

            MarkForced(monster, currentMove);
            monster.SetMoveImmediate(currentMove, forceTransition: true);
            await monster.PerformMove();
        }

        foreach (var enemy in enemies.Where(static e => e.IsAlive))
            await CreatureCmd.Stun(enemy);

        var selectedBuff = await ChoosePlayerBuffIntent(choiceContext, owner);
        if (selectedBuff is null) return;

        foreach (var enemy in enemies.Where(static e => e.IsAlive))
            PendingBuffMoves[enemy] = selectedBuff;
    }

    internal static bool TryApplyPendingBuffMove(MonsterModel monster)
    {
        if (_isApplyingReplacement) return false;
        var creature = monster.Creature;
        if (!PendingBuffMoves.Remove(creature, out var buffMove)) return false;

        var baseMove = ResolveBaseMove(monster.NextMove);
        if (baseMove is null) return false;

        var replacement = CloneMoveWithPerform(
            buffMove,
            async targets =>
            {
                await buffMove.PerformMove(targets);
                monster.SetMoveImmediate(baseMove, forceTransition: true);
            },
            $"{buffMove.StateId}_{baseMove.StateId}");

        BaseMovesByReplacement[replacement] = baseMove;

        _isApplyingReplacement = true;
        try
        {
            monster.SetMoveImmediate(replacement, forceTransition: true);
        }
        finally
        {
            _isApplyingReplacement = false;
        }

        return true;
    }

    internal static void ClearForNewCombat()
    {
        PendingBuffMoves.Clear();
        UsedMovesByPhase.Clear();
        PhaseKeys.Clear();
        BaseMovesByReplacement.Clear();
    }

    private static Task<MoveState?> ChoosePlayerBuffIntent(PlayerChoiceContext choiceContext, Player owner)
    {
        return Task.FromResult<MoveState?>(CreateBlockBuffMove(owner));
    }

    private static MoveState CreateBlockBuffMove(Player owner)
    {
        return new MoveState(
            BuffMoveId,
            async _ => await CreatureCmd.GainBlock(owner.Creature, BuffBlock, ValueProp.Move, null),
            new DefendIntent());
    }

    private static bool CanForceNow(MonsterModel monster, MoveState move)
    {
        var phaseKey = GetPhaseKey(monster);
        if (!PhaseKeys.TryGetValue(monster.Creature, out var knownPhase) || knownPhase != phaseKey)
        {
            PhaseKeys[monster.Creature] = phaseKey;
            UsedMovesByPhase[monster.Creature] = [];
        }

        var used = UsedMovesByPhase[monster.Creature];
        var phaseMoves = GetPhaseMoveIds(monster);
        if (phaseMoves.Count > 0 && used.Count >= phaseMoves.Count)
            used.Clear();

        return !used.Contains(move.StateId);
    }

    private static void MarkForced(MonsterModel monster, MoveState move)
    {
        if (!UsedMovesByPhase.TryGetValue(monster.Creature, out var used))
            UsedMovesByPhase[monster.Creature] = used = [];

        used.Add(move.StateId);
    }

    private static string GetPhaseKey(MonsterModel monster)
    {
        var moveIds = GetPhaseMoveIds(monster);
        return $"{monster.Id.Entry}:{string.Join("|", moveIds)}";
    }

    private static HashSet<string> GetPhaseMoveIds(MonsterModel monster)
    {
        var machine = monster.MoveStateMachine;
        if (machine is null) return [];

        return machine.States.Values
            .OfType<MoveState>()
            .Where(static m => m.IsMove && m.ShouldAppearInLogs && m.StateId != MonsterModel.stunnedMoveId)
            .Select(static m => m.StateId)
            .ToHashSet();
    }

    private static MoveState? ResolveBaseMove(MoveState? move)
    {
        if (move is null) return null;
        return BaseMovesByReplacement.GetValueOrDefault(move) ?? move;
    }

    private static MoveState CloneMoveWithPerform(
        MoveState source,
        Func<IReadOnlyList<Creature>, Task> perform,
        string suffix)
    {
        var clone = new MoveState(
            $"{source.StateId}_{suffix}",
            perform,
            source.Intents.ToArray())
        {
            FollowUpState = source.FollowUpState,
            FollowUpStateId = source.FollowUpStateId,
            MustPerformOnceBeforeTransitioning = source.MustPerformOnceBeforeTransitioning
        };

        if (OnPerformField?.GetValue(source) is not null)
            return clone;

        return clone;
    }
}
