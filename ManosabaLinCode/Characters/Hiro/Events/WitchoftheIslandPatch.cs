// using HarmonyLib;
// using ManosabaLin.Characters.Hiro.Monsters;
// using MegaCrit.Sts2.Core.Events;
// using MegaCrit.Sts2.Core.Models;
// using MegaCrit.Sts2.Core.Runs;
// using System.Linq;
//
// namespace ManosabaLin.Characters.Hiro.Events;
//
// [HarmonyPatch(typeof(EventModel), nameof(EventModel.IsAllowed))]
// internal static class WitchoftheIslandPatch
// {
//     private static void Postfix(EventModel __instance, IRunState runState, ref bool __result)
//     {
//         // 先打日志看看到底有没有触发
//         MainFile.Logger.Info($"[WitchoftheIslandPatch] IsAllowed called: {__instance.GetType().Name}, result={__result}");
//
//         if (!__result) return;
//         if (__instance is not WitchoftheIsland) return;
//
//         MainFile.Logger.Info($"[WitchoftheIslandPatch] WitchoftheIsland detected, checking conditions...");
//
//         var guardOneId = ModelDb.Encounter<GuardOneEncounter>().Id;
//         MainFile.Logger.Info($"[WitchoftheIslandPatch] GuardOne ID: {guardOneId}");
//         MainFile.Logger.Info($"[WitchoftheIslandPatch] CurrentActIndex: {runState.CurrentActIndex}");
//
//         // 打印所有幕的历史
//         for (int i = 0; i < runState.MapPointHistory.Count; i++)
//         {
//             var history = runState.MapPointHistory[i];
//             MainFile.Logger.Info($"[WitchoftheIslandPatch] Act {i}: {history.Count} map points");
//             foreach (var entry in history)
//             {
//                 foreach (var room in entry.Rooms)
//                 {
//                     MainFile.Logger.Info($"[WitchoftheIslandPatch]   Room: {room.ModelId}");
//                 }
//             }
//         }
//
//         var actOneHistory = runState.MapPointHistory.FirstOrDefault();
//         var hasGuardOneInActOne = actOneHistory != null
//                                   && actOneHistory.Any(entry =>
//                                       entry.Rooms.Any(r => r.ModelId == guardOneId));
//
//         MainFile.Logger.Info($"[WitchoftheIslandPatch] hasGuardOneInActOne={hasGuardOneInActOne}, isActThree={runState.CurrentActIndex == 2}");
//
//         if (!hasGuardOneInActOne || runState.CurrentActIndex != 2)
//         {
//             MainFile.Logger.Info($"[WitchoftheIslandPatch] BLOCKING WitchoftheIsland");
//             __result = false;
//         }
//     }
// }