using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using System;
using System.Collections.Generic;
using ManosabaLin.Characters.Ema.Cards;

namespace ManosabaLin.Audio;

/// <summary>
/// 统一管理卡牌打出音效的服务类
/// </summary>
public static class ManosabaAudioService
{
    // ===== 攻击语音池（随机播放） =====
    private static readonly string[] HiroAttackVoicePool =
    [
        "hiro_attack_1.wav",
        "hiro_attack_2.wav",
        "hiro_attack_3.wav"
    ];

    private static readonly string[] EmalinAttackVoicePool =
    [
        "emalin_attack_1.wav",
        "emalin_attack_2.wav",
        "emalin_attack_3.wav"
    ];

    // ===== 卡牌音效映射 =====
    private static readonly Dictionary<Type, (string path, float volume)> CardSoundMap = new()
    {
        // 希罗基础
        [typeof(HiroAttack)]       = ("hiro_attack.wav", 0.8f),
        [typeof(HiroDefend)]       = ("hiro_defend.wav", 0.8f),

        // 艾玛基础
        [typeof(EmalinAttack)]     = ("emalin_attack.wav", 0.8f),
        [typeof(EmalinDefend)]     = ("emalin_defend.wav", 0.8f),
        [typeof(Emamonv)]          = ("emamonv.wav", 0.8f),

        // 希罗特殊卡
        [typeof(CardTwelve)]       = ("card_twelve.wav", 0.8f),
        [typeof(CardThirteen)]     = ("card_thirteen.wav", 0.8f),
        [typeof(Powerthreethreecard)] = ("power_three_three.wav", 0.8f),

        // 艾玛特殊卡
        [typeof(Emawichpower)]     = ("emawichpower.wav", 0.9f),
        [typeof(EmaWitchKillerCard)] = ("emawitchkiller.wav", 0.9f),
        [typeof(Lamort)]           = ("lamort.wav", 0.8f),
    };

    // ===== 主题音效映射 =====
    private static readonly Dictionary<Type, (string path, float volume)> ThemeSoundMap = new()
    {
        [typeof(Hirodeath)]        = ("hirodeath_theme.mp3", 1.0f),
        [typeof(HiroBadEnding)]    = ("hiro_bad_ending_theme.mp3", 1.0f),
        [typeof(HiroWith)]         = ("hiro_with_theme.mp3", 1.0f),
        [typeof(HappyEnding)]     = ("happy_ending_theme.mp3", 1.0f),
        [typeof(Justice)]          = ("justice_theme.mp3", 1.0f),
        [typeof(Save)]             = ("save_theme.mp3", 1.0f),
        [typeof(TheEnd)]           = ("the_end_theme.mp3", 1.0f),
        [typeof(DeathRewind)]      = ("death_rewind_theme.mp3", 1.0f),
        [typeof(Emadeath)]         = ("ema_death_theme.mp3", 1.0f),
    };

    // ===== 默认音效 =====
    private const string DefaultAttackSound = "hiro_attack.wav";
    private const string DefaultSkillSound = "emamonv.wav";
    private const float DefaultVolume = 0.8f;

    private static readonly Random Rng = new();

    /// <summary>
    /// 播放卡牌音效（自动根据卡牌类型匹配）
    /// 返回 true 表示找到了专属音效并播放，false 表示使用默认音效
    /// </summary>
    public static bool TryPlayCardSound(CardModel card)
    {
        if (card == null) return false;

        var cardType = card.GetType();

        // 先查主题音效
        if (ThemeSoundMap.TryGetValue(cardType, out var theme))
        {
            ManosabaAudio.TryPlayOneShot(theme.path.BgmAudioPath(), theme.volume);
            return true; // 有专属音效
        }

        // 再查卡牌音效
        if (CardSoundMap.TryGetValue(cardType, out var sfx))
        {
            ManosabaAudio.TryPlayOneShot(sfx.path.CardsAudioPath(), sfx.volume);
            return true; // 有专属音效
        }

        // 默认音效（根据卡牌类型）
        var defaultPath = card.Type switch
        {
            CardType.Attack => DefaultAttackSound,
            _ => DefaultSkillSound
        };
        ManosabaAudio.TryPlayOneShot(defaultPath.CardsAudioPath(), DefaultVolume);
        return false; // 没有专属音效，使用默认
    }

    /// <summary>
    /// 播放指定卡牌音效
    /// </summary>
    public static bool TryPlayCardSound(string soundFile, float volume = 0.8f)
    {
        return ManosabaAudio.TryPlayOneShot(soundFile.CardsAudioPath(), volume);
    }

    /// <summary>
    /// 播放主题音效
    /// </summary>
    public static bool TryPlayTheme(string soundFile, float volume = 1.0f)
    {
        return ManosabaAudio.TryPlayOneShot(soundFile.BgmAudioPath(), volume);
    }

    /// <summary>
    /// 播放角色语音
    /// </summary>
    public static bool TryPlayCharacterVoice(string soundFile, float volume = 1.0f)
    {
        return ManosabaAudio.TryPlayOneShot(soundFile.CharacterAudioPath(), volume);
    }

    /// <summary>
    /// 随机播放希罗攻击语音
    /// </summary>
    public static bool TryPlayHiroAttackVoice(float volume = 0.8f)
    {
        var index = Rng.Next(HiroAttackVoicePool.Length);
        return ManosabaAudio.TryPlayOneShot(HiroAttackVoicePool[index].CardsAudioPath(), volume);
    }

    /// <summary>
    /// 随机播放艾玛攻击语音
    /// </summary>
    public static bool TryPlayEmalinAttackVoice(float volume = 0.8f)
    {
        var index = Rng.Next(EmalinAttackVoicePool.Length);
        return ManosabaAudio.TryPlayOneShot(EmalinAttackVoicePool[index].CardsAudioPath(), volume);
    }

    /// <summary>
    /// 随机播放攻击语音（根据角色自动选择）
    /// </summary>
    public static bool TryPlayAttackVoice(CardModel card, float volume = 0.8f)
    {
        if (card?.Owner?.Character == null) return false;

        var characterId = card.Owner.Character.Id.Entry ?? "";
        if (characterId.Contains("Hiro", StringComparison.OrdinalIgnoreCase))
            return TryPlayHiroAttackVoice(volume);
        else if (characterId.Contains("Emalin", StringComparison.OrdinalIgnoreCase))
            return TryPlayEmalinAttackVoice(volume);

        return TryPlayHiroAttackVoice(volume);
    }

    /// <summary>
    /// 注册新的卡牌音效（用于扩展）
    /// </summary>
    public static void RegisterCardSound<T>(string soundFile, float volume = 0.8f) where T : CardModel
    {
        CardSoundMap[typeof(T)] = (soundFile, volume);
    }

    /// <summary>
    /// 注册新的主题音效（用于扩展）
    /// </summary>
    public static void RegisterThemeSound<T>(string soundFile, float volume = 1.0f) where T : CardModel
    {
        ThemeSoundMap[typeof(T)] = (soundFile, volume);
    }
}
