using HarmonyLib;
using ManosabaLin.Characters.Hiro.Monsters;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using System.Linq;

namespace ManosabaLin.Characters.Hiro.Events;

[HarmonyPatch(typeof(EventModel), nameof(EventModel.IsAllowed))]
internal static class WitchoftheIslandPatch
{
    private static void Postfix(EventModel __instance, RunState runState, ref bool __result)
    {
        if (__result == false) return;
        if (__instance is not WitchoftheIsland) return;

        // 只有在第一幕完成过 GuardOneEncounter 才能出现
        var guardOneId = ModelDb.Encounter<GuardOneEncounter>().Id;
        var hasGuardOne = runState.MapPointHistory.Count > 0 &&
                          runState.MapPointHistory[0]
                              .Any(entry => entry.Rooms
                                  .Any(r => r.ModelId == guardOneId));

        if (!hasGuardOne)
            __result = false;
    }
}
