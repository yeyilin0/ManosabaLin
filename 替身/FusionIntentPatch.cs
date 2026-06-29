using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FusionMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace FusionMod.Patches;

/// <summary>
/// 融合意图：每次RollMove创建Mutable搭档，提取真实MoveState，
/// 直接执行其_onPerform（通用解法，支持塞牌/buff/格挡等所有效果）
/// </summary>
public static class FusionIntentPatch
{
    static readonly System.Reflection.FieldInfo? _onPerformF = AccessTools.Field(typeof(MoveState), "_onPerform");
    static readonly Dictionary<string, MonsterModel> _canonCache = new();
    static readonly Dictionary<MonsterModel, int> _turnCnt = new();
    static readonly HashSet<MonsterModel> _traitsApplied = new();
    [ThreadStatic] static bool _inFusion;

    [HarmonyPatch(typeof(MonsterModel), "RollMove", typeof(IEnumerable<Creature>))]
    [HarmonyPostfix]
    public static void Postfix(MonsterModel __instance, IEnumerable<Creature> targets)
    {
        if (_inFusion) return;
        if (!FusionManager.IsFusionModeActive) return;
        if (!FusionManager.IsCurrentNodeFusion()) return;

        MoveState mainMove = __instance.NextMove;
        if (mainMove == null || mainMove.Id.EndsWith("_FU")) return;

        string? pid = GetPartnerId(__instance);
        if (pid == null) return;

        try
        {
            // 拿或缓存Canonical
            if (!_canonCache.TryGetValue(pid, out var canonical))
            {
                string slug = StringHelper.Slugify(pid);
                foreach (var cm in ModelDb.Monsters)
                    if (((AbstractModel)cm).Id.Entry.Equals(slug, StringComparison.OrdinalIgnoreCase))
                    { canonical = cm; _canonCache[pid] = cm; break; }
                if (canonical == null) return;
            }

            // ★ 每次新建Mutable（让_onPerform里的this.Creature可用）
            var partner = (MonsterModel)canonical.ToMutable();
            partner.Creature = __instance.Creature; // 搭档效果通过主怪生物结算
            partner.RunRng = __instance.RunRng;
            // 首次融合时应用搭档的开局自带Buff（如外骨骼虫的9层覆甲）
            if (!_traitsApplied.Contains(__instance))
            {
                _traitsApplied.Add(__instance);
                _ = partner.AfterAddedToRoom(); // 异步触发，不阻塞
            }
            var sm = AccessTools.Method(typeof(MonsterModel), "GenerateMoveStateMachine")
                     ?.Invoke(partner, null) as MonsterMoveStateMachine;
            if (sm == null) return;

            // 收集所有MoveState
            var dict = AccessTools.Property(typeof(MonsterMoveStateMachine), "States")
                       ?.GetValue(sm) as Dictionary<string, MonsterState>;
            if (dict == null) return;

            var moves = new List<MoveState>();
            foreach (var kv in dict)
                if (kv.Value is MoveState ms && ms.Intents != null && ms.Intents.Count > 0 && ms.Id != "UNSET_MOVE")
                    moves.Add(ms);
            if (moves.Count == 0) return;

            // 确定性选择（基于run seed + 怪物ID + 回合数，多人同步）
            var turn = _turnCnt.TryGetValue(__instance, out var t) ? t : 0;
            _turnCnt[__instance] = turn + 1;
            string runSeed = MegaCrit.Sts2.Core.Runs.RunManager.Instance?.DebugOnlyGetState()?.Rng.StringSeed ?? "0";
            int seed = StringHelper.GetDeterministicHashCode($"{runSeed}_{((AbstractModel)__instance).Id.Entry}_FUSION_T{turn}");
            var rng = new Random(seed);
            var pState = moves[rng.Next(moves.Count)];
            var partnerIntents = pState.Intents;
            var partnerPerform = _onPerformF?.GetValue(pState) as Func<IReadOnlyList<Creature>, Task>;

            // 100%显示，100%执行——完全一致
            var combined = new List<AbstractIntent>();
            if (mainMove.Intents != null) combined.AddRange(mainMove.Intents);
            combined.AddRange(partnerIntents ?? Array.Empty<AbstractIntent>());

            var mainPerform = _onPerformF?.GetValue(mainMove) as Func<IReadOnlyList<Creature>, Task>;

            Func<IReadOnlyList<Creature>, Task> fused = async (t) =>
            {
                if (mainPerform != null) await mainPerform(t);
                if (partnerPerform != null)
                {
                    await Task.Delay(200);
                    await partnerPerform(t);
                }
            };

            var fusedMove = new MoveState(mainMove.Id + "_FU", fused, combined.ToArray());
            fusedMove.FollowUpState = mainMove.FollowUpState;
            fusedMove.MustPerformOnceBeforeTransitioning = mainMove.MustPerformOnceBeforeTransitioning;

            _inFusion = true;
            try { __instance.SetMoveImmediate(fusedMove, forceTransition: true); }
            catch { }
            _inFusion = false;
        }
        catch (Exception ex) { FusionModMain.Logger?.Warn($"意图异常: {ex.Message}", 3); }
    }

    static string? GetPartnerId(MonsterModel m)
    {
        foreach (var kv in FusionMonsterSetupPatch.PartnerNames) if (kv.Key == m) return kv.Value;
        return null;
    }
}
