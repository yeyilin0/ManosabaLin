using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Reflection;

namespace ManosabaLin.Audio;

/// <summary>
/// 卡牌打出时自动播放音效的补丁
/// Patch OnPlayWrapper 而非 AutoPlay，这样手动出牌和自动出牌都会触发
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class CardPlayAudioPatch
{
    private static void Postfix(CardModel __instance)
    {
        var card = __instance;
        if (card == null) return;

        // 播放卡牌专属音效
        var hasCustomSound = ManosabaAudioService.TryPlayCardSound(card);

        // 攻击牌且没有专属音效时，播放随机语音
        if (card.Type == CardType.Attack && !hasCustomSound)
            ManosabaAudioService.TryPlayAttackVoice(card);
    }
}
