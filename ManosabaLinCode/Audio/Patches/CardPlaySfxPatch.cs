using HarmonyLib;
using ManosabaLin.Audio.Services;

namespace ManosabaLin.Audio.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class CardPlaySfxPatch
{
    [HarmonyPrefix]
    public static bool Prefix(CardModel __instance)
    {
        CardAudioService.PlayCardPlaySfx(__instance);
        return true;
    }
}
