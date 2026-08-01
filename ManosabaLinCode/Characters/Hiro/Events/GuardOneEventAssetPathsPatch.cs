using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ManosabaLin.Characters.Hiro.Events;

[HarmonyPatch(typeof(EventModel), nameof(EventModel.GetAssetPaths), typeof(IRunState))]
internal static class GuardOneEventAssetPathsPatch
{
    private static readonly string DefaultInitialPortraitPath =
        ImageHelper.GetImagePath("events/manosaba_lin_event_guard_one_event.png");

    [HarmonyPostfix]
    private static void RemoveMissingDefaultPortrait(EventModel __instance, ref IEnumerable<string> __result)
    {
        if (__instance is not GuardOneEvent) return;

        __result = __result.Where(path => !string.Equals(path, DefaultInitialPortraitPath, StringComparison.Ordinal));
    }
}
