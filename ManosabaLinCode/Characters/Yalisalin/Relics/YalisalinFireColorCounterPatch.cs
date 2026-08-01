using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ManosabaLin.Characters.Yalisalin.Relics;

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class YalisalinFireColorCounterPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        try
        {
            var target = __instance.Entity;
            if (target is not { IsMonster: true, CombatState: { } combatState })
                return;

            var viewer = ResolveLocalYalisalinOwner(combatState);
            if (viewer == null)
                return;

            var nodeName = $"YalisalinFireColorCounter_{viewer.NetId}";
            if (__instance.GetNodeOrNull<YalisalinFireColorCounter>(nodeName) != null)
                return;

            var counter = new YalisalinFireColorCounter
            {
                Name = nodeName
            };
            __instance.AddChild(counter);
            counter.SetContext(viewer, target);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[YalisalinFireColor] counter setup failed: {ex.Message}");
        }
    }

    private static Player? ResolveLocalYalisalinOwner(ICombatState combatState)
    {
        try
        {
            var local = LocalContext.GetMe(combatState);
            if (IsYalisalinWithHairpin(local))
                return local;
        }
        catch
        {
            // Local context may not be established in some debug/test combat setup paths.
        }

        var yalisalins = combatState.Players
            .Where(IsYalisalinWithHairpin)
            .ToArray();

        return yalisalins.Length == 1 ? yalisalins[0] : null;
    }

    private static bool IsYalisalinWithHairpin(Player? player)
    {
        return player?.Character is Yalisalin
               && YalisalinFireColorSystem.TryGetHairpin(player, out _);
    }
}
