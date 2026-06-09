using Godot;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 怅然球体（快乐+悲伤）：每打出攻击牌+1临时减力量+回复队友2血，每打出技能+2盾，3张技能回1能量，能力牌全体+1能量。
/// </summary>
[RegisterOrb]
public sealed class EmotionMelancholyOrb : EmotionOrb
{
    protected override Color GetOrbColor() => new(0.8f, 0.6f, 0.2f);
    protected override string GetOrbName() => "emotion_melancholy_orb";
}
