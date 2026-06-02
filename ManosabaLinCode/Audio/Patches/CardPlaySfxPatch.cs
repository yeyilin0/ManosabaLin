using HarmonyLib;
using ManosabaLin.Audio.Services;

namespace ManosabaLin.Audio.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class CardPlaySfxPatch
{
    [HarmonyPrefix]
    public static bool Prefix(CardModel __instance)
    {
        CardAudioService.PlayCardSfx(__instance);
        return true;
    }
}
