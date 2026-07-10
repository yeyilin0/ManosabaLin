using HarmonyLib;
using ManosabaLin.Characters.Ananlin.Relics;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ManosabaLin.Characters.Ananlin.Patches;

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.RollMove), typeof(IEnumerable<Creature>))]
public static class AnanlinSilenceIntentPatch
{
    [HarmonyPostfix]
    public static void Postfix(MonsterModel __instance)
    {
        AnanlinSilenceIntentManager.TryApplyPendingBuffMove(__instance);
    }
}
