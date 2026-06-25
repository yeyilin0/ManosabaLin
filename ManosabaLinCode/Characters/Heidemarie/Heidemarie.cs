using Godot;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Heidemarie;

[RegisterCharacter]
public class Heidemarie : ManosabaCharacterTemplate<HeidemarieCardPool, HeidemarieRelicPool, HeidemariePotionPool>
{
    public const string CharacterId = "Heidemarie";

    public static readonly Color Color = new("66d9cc");

    public override Color NameColor => Color;
    public override Color EnergyLabelOutlineColor => new(0.4f, 0.85f, 0.8f);
    public override Color MapDrawingColor => new(0.4f, 0.85f, 0.8f);
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 75;
    public override int StartingGold => 99;

    public override string? PlaceholderCharacterId => "ironclad";
    public override string CharacterSelectSfx => "event:/sfx/characters/ironclad/ironclad_select";
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;
    public override bool RequiresEpochAndTimeline => false;

    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_blunt",
            "vfx/vfx_heavy_blunt",
            "vfx/vfx_attack_slash",
            "vfx/vfx_bloody_impact",
            "vfx/vfx_rock_shatter"
        ];
    }
}
