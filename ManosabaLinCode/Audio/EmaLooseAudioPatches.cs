using HarmonyLib;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace ManosabaLin.Audio;

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
internal static class EmaLooseAudioPatches
{
    private static void Postfix(CharacterModel characterModel)
    {
        if (!IsEma(characterModel))
            return;

        ManosabaAudio.TryPlayOneShot("0101Adv04_Ema001.wav".CharacterAudioPath());
    }

    private static bool IsEma(CharacterModel? characterModel)
    {
        var entry = characterModel?.Id.Entry;
        return string.Equals(entry, ModelDb.GetEntry(typeof(Emalin)), StringComparison.OrdinalIgnoreCase);
    }
}
