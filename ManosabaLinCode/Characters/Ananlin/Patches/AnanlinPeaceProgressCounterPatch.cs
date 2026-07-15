using Godot;
using HarmonyLib;
using ManosabaLin.Characters.Ananlin.Nodes;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ManosabaLin.Characters.Ananlin.Patches;

[HarmonyPatch(typeof(NEnergyCounter), nameof(NEnergyCounter._Ready))]
public static class AnanlinPeaceProgressCounterPatch
{
    [HarmonyPostfix]
    public static void Postfix(NEnergyCounter __instance, Player ____player)
    {
        try
        {
            if (____player.Character is not ManosabaLin.Characters.Ananlin.Ananlin)
                return;

            __instance
                .GetNodeOrNull<AnanlinPeaceProgressCounter>("%AnanlinPeaceProgress")
                ?.SetContext(____player);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[AnanlinPeaceProgress] setup failed: {ex.Message}");
        }
    }
}
