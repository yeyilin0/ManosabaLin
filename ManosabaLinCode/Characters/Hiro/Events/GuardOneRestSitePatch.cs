using System.Linq;
using HarmonyLib;
using ManosabaLin.Characters.Hiro.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ManosabaLin;
using ManosabaLin.Characters.Hiro.Monsters;

namespace ManosabaLin.Characters.Hiro.Events;

internal static class GuardOneEventState
{
    private static bool _hasTriggered;

    internal static bool ShouldTrigger => !_hasTriggered;

    internal static void MarkTriggered() => _hasTriggered = true;

    internal static void Reset() => _hasTriggered = false;

    internal static bool IsFirstRestSite(IRunState runState)
    {
        int restSiteCount = 0;
        foreach (var mapPoint in runState.MapPointHistory)
        {
            foreach (var entry in mapPoint)
            {
                if (entry.Rooms.Any(r => r.RoomType == RoomType.RestSite))
                {
                    restSiteCount++;
                    break;
                }
            }
        }
        return restSiteCount <= 1;
    }

    /// <summary>
    /// 调试命令：直接触发 GuardOne 事件（用于测试 Boss 遭遇）
    /// 在游戏控制台中调用：GuardOneEventState.TriggerDebug()
    /// </summary>
    public static void TriggerDebug()
    {
        _hasTriggered = false; // 重置状态，允许再次触发
        MainFile.Logger.Info("[Debug] GuardOne 事件已重置，下次进入火堆将触发");
    }

    /// <summary>
    /// 调试命令：直接设置 Boss 遭遇（跳过事件）
    /// 在游戏控制台中调用：GuardOneEventState.ForceBossEncounter()
    /// </summary>
    public static void ForceBossEncounter()
    {
        try
        {
            var runState = RunManager.Instance?.State;
            if (runState == null)
            {
                MainFile.Logger.Info("[Debug] 无法获取 RunState");
                return;
            }

            var act = runState.Acts[runState.CurrentActIndex];
            var encounter = ModelDb.Get<GuardOneEncounter>();
            if (encounter == null)
            {
                MainFile.Logger.Info("[Debug] GuardOneEncounter 未找到");
                return;
            }

            act.SetBossEncounter(encounter);
            MainFile.Logger.Info("[Debug] Boss 遭遇已设置为 GuardOne");
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Info($"[Debug] 设置 Boss 遭遇失败: {ex.Message}");
        }
    }
}

/// <summary>
///     在第一层第一次进入火堆时，将 RestSiteRoom 替换为 GuardOneEvent 事件房间
///     Patch CreateRoom：当 roomType == RestSite 且满足条件时，返回 EventRoom
/// </summary>
[HarmonyPatch(typeof(RunManager), "CreateRoom")]
internal static class GuardOneCreateRoomPatch
{
    private static bool Prefix(ref AbstractRoom __result, RoomType roomType)
    {
        if (roomType != RoomType.RestSite) return true; // 不拦截，继续原方法
        if (!GuardOneEventState.ShouldTrigger) return true;

        var runState = RunManager.Instance.State;
        if (runState == null) return true;

        if (!GuardOneEventState.IsFirstRestSite(runState)) return true;

        // 条件满足：替换为 GuardOneEvent 事件房间
        GuardOneEventState.MarkTriggered();

        var guardOneEvent = ModelDb.Get<GuardOneEvent>();
        if (guardOneEvent == null) return true;

        __result = new EventRoom(guardOneEvent);
        return false; // 跳过原方法
    }
}

/// <summary>
///     调试用：每次创建房间后，强制设置 Boss 遭遇为 GuardOne
///     测试完成后删除此补丁
/// </summary>
[HarmonyPatch(typeof(RunManager), "CreateRoom")]
internal static class GuardOneDebugBossPatch
{
    private static void Postfix(ref AbstractRoom __result, RoomType roomType)
    {
        if (roomType != RoomType.Boss) return;

        try
        {
            var runState = RunManager.Instance?.State;
            if (runState == null) return;

            var act = runState.Acts[runState.CurrentActIndex];
            var encounter = ModelDb.Get<GuardOneEncounter>();
            if (encounter == null) return;

            act.SetBossEncounter(encounter);
            MainFile.Logger.Info("[Debug] Boss 遭遇已强制设置为 GuardOne");
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Info($"[Debug] 设置 Boss 遭遇失败: {ex.Message}");
        }
    }
}
