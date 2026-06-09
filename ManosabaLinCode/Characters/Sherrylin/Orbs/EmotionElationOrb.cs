using Godot;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 雀跃球体（快乐+惊讶）：获得等于能量上限的能量但减少下回合能量，每抽5张卡减少1层减少下回合能量，清零后可重复获得。
/// </summary>
[RegisterOrb]
public sealed class EmotionElationOrb : EmotionOrb
{
    protected override Color GetOrbColor() => new(1f, 0.9f, 0.3f);
    protected override string GetOrbName() => "emotion_elation_orb";
}
