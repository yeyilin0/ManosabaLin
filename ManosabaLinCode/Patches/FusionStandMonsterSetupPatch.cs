using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Patches;

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.SetUpForCombat))]
public static class FusionStandMonsterSetupPatch
{
    [HarmonyPostfix]
    public static void Postfix(MonsterModel __instance)
    {
        var creature = Traverse.Create(__instance).Field<Creature?>("_creature").Value;
        if (creature == null) return;

        var combat = creature.CombatState;
        if (combat != null)
            FusionStandManager.ClearForNewCombat(combat.GetHashCode());
    }
}
