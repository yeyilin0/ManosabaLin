using HarmonyLib;
using ManosabaLin.Characters.Hiro;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace ManosabaLin.Audio;

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
internal static class HiroLooseAudioPatches
{
    private static void Postfix(CharacterModel characterModel)
    {
        if (!IsHiro(characterModel))
            return;

        ManosabaAudio.TryPlayOneShot("0101Adv04_Hiro005.wav".CharacterAudioPath());
    }

    private static bool IsHiro(CharacterModel? characterModel)
    {
        var entry = characterModel?.Id.Entry;
        return string.Equals(entry, ModelDb.GetEntry(typeof(Hiro)), StringComparison.OrdinalIgnoreCase);
    }
}