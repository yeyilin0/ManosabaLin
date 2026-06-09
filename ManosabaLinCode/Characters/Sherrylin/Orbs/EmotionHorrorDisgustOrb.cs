using Godot;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 骇厌球体（厌恶+惊讶）：受到伤害时对敌方全体造成能被能力增幅的伤害并抽1，下回合开始随机对敌人造成1点伤害次数等于手牌数。
/// </summary>
[RegisterOrb]
public sealed class EmotionHorrorDisgustOrb : EmotionOrb
{
    protected override Color GetOrbColor() => new(0.5f, 0.8f, 0.5f);
    protected override string GetOrbName() => "emotion_horror_disgust_orb";
}
