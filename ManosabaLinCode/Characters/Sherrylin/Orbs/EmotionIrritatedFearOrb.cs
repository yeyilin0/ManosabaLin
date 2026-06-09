using Godot;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 恼惧球体（愤怒+恐惧）：无法打出攻击牌，获得格挡时对随机敌人造成双倍格挡伤害，回合结束给予随机队友13伤害并使其获得等量护盾。
/// </summary>
[RegisterOrb]
public sealed class EmotionIrritatedFearOrb : EmotionOrb
{
    protected override Color GetOrbColor() => new(0.9f, 0.2f, 0.5f);
    protected override string GetOrbName() => "emotion_irritated_fear_orb";
}
