using Godot;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 凄惶球体（悲伤+恐惧）：无法打出攻击牌和能力牌，获得格挡时随机给予友方等量格挡并可保留至下回合，回合结束回复护盾量四分之一的血量。
/// </summary>
[RegisterOrb]
public sealed class EmotionDesolateOrb : EmotionOrb
{
    protected override Color GetOrbColor() => new(0.3f, 0.3f, 0.8f);
    protected override string GetOrbName() => "emotion_desolate_orb";
}
