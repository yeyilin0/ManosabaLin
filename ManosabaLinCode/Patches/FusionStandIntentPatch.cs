using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using ManosabaLin.Characters.Common.Powers;

namespace ManosabaLin.Patches;

[HarmonyPatch]
public static class FusionStandIntentPatch
{
    private static readonly FieldInfo? OnPerformField = AccessTools.Field(typeof(MoveState), "_onPerform");
    private static readonly Dictionary<MoveState, MoveState> BaseMovesByFusedMove = [];

    [ThreadStatic] private static bool _isApplyingFusionMove;

    [HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.RollMove), typeof(IEnumerable<Creature>))]
    [HarmonyPostfix]
    public static void Postfix(MonsterModel __instance, IEnumerable<Creature> targets)
    {
        TryApplyFusionMove(__instance);
    }

    internal static bool TryApplyFusionMove(MonsterModel monster)
    {
        if (_isApplyingFusionMove) return false;
        if (!FusionStandManager.IsActiveForCurrentCombat()) return false;
        if (monster.Creature.GetPower<FusionStandPower>() == null) return false;

        var mainMove = ResolveBaseMove(monster.NextMove);
        if (mainMove == null)
            return false;

        if (!FusionStandManager.EnsureStand(monster))
            return false;

        try
        {
            var standMove = FusionStandManager.PickStandMove(monster);
            if (standMove == null) return false;

            var combinedIntents = new List<AbstractIntent>();
            if (mainMove.Intents != null)
                combinedIntents.AddRange(mainMove.Intents);
            combinedIntents.AddRange(standMove.Intents);

            var mainPerform = OnPerformField?.GetValue(mainMove) as Func<IReadOnlyList<Creature>, Task>;
            var standPerform = OnPerformField?.GetValue(standMove) as Func<IReadOnlyList<Creature>, Task>;

            async Task FusedPerform(IReadOnlyList<Creature> moveTargets)
            {
                if (mainPerform != null)
                    await mainPerform(moveTargets);

                if (standPerform != null)
                {
                    await Task.Delay(200);
                    await standPerform(moveTargets);
                }
            }

            var fusedMove = new MoveState(
                mainMove.Id + FusionStandManager.FusedMoveSuffix,
                FusedPerform,
                combinedIntents.ToArray())
            {
                FollowUpState = mainMove.FollowUpState,
                FollowUpStateId = mainMove.FollowUpStateId,
                MustPerformOnceBeforeTransitioning = mainMove.MustPerformOnceBeforeTransitioning
            };
            BaseMovesByFusedMove[fusedMove] = mainMove;

            _isApplyingFusionMove = true;
            try
            {
                monster.SetMoveImmediate(fusedMove, forceTransition: true);
            }
            finally
            {
                _isApplyingFusionMove = false;
            }

            MainFile.Logger.Info($"[FusionStand] {mainMove.Id} gained stand move {standMove.Id}");
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[FusionStand] intent failed: {ex.Message}");
            return false;
        }
    }

    internal static void ClearForNewCombat()
    {
        BaseMovesByFusedMove.Clear();
    }

    private static MoveState? ResolveBaseMove(MoveState? move)
    {
        if (move == null)
            return null;

        if (!move.Id.EndsWith(FusionStandManager.FusedMoveSuffix, StringComparison.Ordinal))
            return move;

        return BaseMovesByFusedMove.GetValueOrDefault(move);
    }
}
