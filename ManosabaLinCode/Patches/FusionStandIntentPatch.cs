using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Runs;
using ManosabaLin.Characters.Common.Powers;

namespace ManosabaLin.Patches;

public static class FusionStandIntentPatch
{
    private static readonly FieldInfo? OnPerformField = AccessTools.Field(typeof(MoveState), "_onPerform");
    private static readonly Dictionary<string, MonsterModel> CanonicalCache = [];
    private static readonly Dictionary<MonsterModel, int> TurnCounts = [];
    private static readonly HashSet<MonsterModel> TraitsApplied = [];

    [ThreadStatic] private static bool _isApplyingFusionMove;

    [HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.RollMove), typeof(IEnumerable<Creature>))]
    [HarmonyPostfix]
    public static void Postfix(MonsterModel __instance, IEnumerable<Creature> targets)
    {
        if (_isApplyingFusionMove) return;
        if (!FusionStandManager.IsActiveForCurrentCombat()) return;
        if (__instance.Creature.GetPower<FusionStandPower>() == null) return;

        var mainMove = __instance.NextMove;
        if (mainMove == null || mainMove.Id.EndsWith("_FUSION_STAND", StringComparison.Ordinal))
            return;

        FusionStandManager.EnsurePartner(__instance);
        if (!FusionStandManager.PartnerMonsterIds.TryGetValue(__instance, out var partnerId))
            return;

        try
        {
            var canonical = GetCanonicalMonster(partnerId);
            if (canonical == null) return;

            var partner = (MonsterModel)canonical.ToMutable();
            partner.Creature = __instance.Creature;
            partner.RunRng = __instance.RunRng;

            if (TraitsApplied.Add(__instance))
                _ = partner.AfterAddedToRoom();

            var moveStateMachine = AccessTools.Method(typeof(MonsterModel), "GenerateMoveStateMachine")
                ?.Invoke(partner, null) as MonsterMoveStateMachine;
            if (moveStateMachine == null) return;

            var partnerMoves = moveStateMachine.States.Values
                .OfType<MoveState>()
                .Where(state => state.Id != "UNSET_MOVE" && state.Intents is { Count: > 0 })
                .ToList();
            if (partnerMoves.Count == 0) return;

            var turn = TurnCounts.TryGetValue(__instance, out var currentTurn) ? currentTurn : 0;
            TurnCounts[__instance] = turn + 1;

            var runSeed = RunManager.Instance?.DebugOnlyGetState()?.Rng.StringSeed ?? "0";
            var monsterId = ((AbstractModel)__instance).Id.Entry;
            var seed = StringHelper.GetDeterministicHashCode($"{runSeed}_{monsterId}_FUSION_STAND_T{turn}");
            var partnerMove = partnerMoves[new Random(seed).Next(partnerMoves.Count)];

            var combinedIntents = new List<AbstractIntent>();
            if (mainMove.Intents != null)
                combinedIntents.AddRange(mainMove.Intents);
            combinedIntents.AddRange(partnerMove.Intents);

            var mainPerform = OnPerformField?.GetValue(mainMove) as Func<IReadOnlyList<Creature>, Task>;
            var partnerPerform = OnPerformField?.GetValue(partnerMove) as Func<IReadOnlyList<Creature>, Task>;

            async Task FusedPerform(IReadOnlyList<Creature> moveTargets)
            {
                if (mainPerform != null)
                    await mainPerform(moveTargets);

                if (partnerPerform != null)
                {
                    await Task.Delay(200);
                    await partnerPerform(moveTargets);
                }
            }

            var fusedMove = new MoveState(
                mainMove.Id + "_FUSION_STAND",
                FusedPerform,
                combinedIntents.ToArray());
            fusedMove.FollowUpState = mainMove.FollowUpState;
            fusedMove.MustPerformOnceBeforeTransitioning = mainMove.MustPerformOnceBeforeTransitioning;

            _isApplyingFusionMove = true;
            try
            {
                __instance.SetMoveImmediate(fusedMove, forceTransition: true);
            }
            finally
            {
                _isApplyingFusionMove = false;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[FusionStand] intent failed: {ex.Message}");
        }
    }

    private static MonsterModel? GetCanonicalMonster(string partnerId)
    {
        if (CanonicalCache.TryGetValue(partnerId, out var cached))
            return cached;

        var slug = StringHelper.Slugify(partnerId);
        var canonical = ModelDb.Monsters.FirstOrDefault(model =>
            ((AbstractModel)model).Id.Entry.Equals(slug, StringComparison.OrdinalIgnoreCase));
        if (canonical != null)
            CanonicalCache[partnerId] = canonical;

        return canonical;
    }
}
