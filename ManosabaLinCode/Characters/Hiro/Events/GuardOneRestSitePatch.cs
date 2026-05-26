using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ManosabaLin.Characters.Hiro.Events;

/// <summary>
///     在第一次进入火堆时，将 RestSiteRoom 替换为 GuardOneEvent 事件房间
///     Patch CreateRoom：当 roomType == RestSite 且未进入过火堆时，返回 EventRoom
/// </summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.CreateRoom))]
internal static class GuardOneCreateRoomPatch
{
    private static bool Prefix(ref AbstractRoom __result, RoomType roomType)
    {
        if (roomType != RoomType.RestSite) return true;

        var runState = RunManager.Instance.State;
        if (runState == null) return true;

        if (runState.MapPointHistory.Any(l => l.Any(entry => entry.MapPointType == MapPointType.RestSite)))
            return true;

        var guardOneEvent = ModelDb.Get<GuardOneEvent>();

        __result = new EventRoom(guardOneEvent);
        return false;
    }
}
