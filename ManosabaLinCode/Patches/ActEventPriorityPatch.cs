using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using Rng = MegaCrit.Sts2.Core.Random.Rng;

namespace ManosabaLin.Patches;

[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
internal static class ActEventPriorityPatch
{
    private const float PromotionChance = 0.65f;
    private static readonly string EventIdPrefix = $"{MainFile.Slug}_EVENT_";
    private static readonly AccessTools.FieldRef<ActModel, RoomSet> RoomsRef =
        AccessTools.FieldRefAccess<ActModel, RoomSet>("_rooms");

    private static void Postfix(ActModel __instance, Rng rng, UnlockState unlockState, bool isMultiplayer = false)
    {
        try
        {
            if (rng.NextFloat() >= PromotionChance)
                return;

            var events = RoomsRef(__instance).events;
            if (events.Count <= 1)
                return;

            var manosabaEvents = events
                .Where(e => e.Id.Entry.StartsWith(EventIdPrefix, StringComparison.Ordinal))
                .ToList();

            var promotedEvent = rng.NextItem(manosabaEvents);
            if (promotedEvent == null)
                return;

            var currentIndex = events.IndexOf(promotedEvent);
            if (currentIndex <= 0)
                return;

            events.RemoveAt(currentIndex);
            events.Insert(0, promotedEvent);
            MainFile.Logger.Debug($"[ActEventPriority] Promoted {promotedEvent.Id.Entry} in {__instance.Id.Entry}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[ActEventPriority] Failed to promote ManosabaLin event: {ex.Message}");
        }
    }
}
