using Godot;
using ManosabaLin.Characters.Hiro;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace ManosabaLin.Characters.Yalisalin;

[RegisterCharacter]
public class Yalisalin : ManosabaCharacterTemplate<YalisalinCardPool, YalisalinRelicPool, YalisalinPotionPool>
{
    public const string CharacterId = "Yalisalin";

    public static readonly Color Color = new("ff0000");

    public override Color NameColor => Color;
    public override Color EnergyLabelOutlineColor => new(1f, 0f, 0f);
    public override Color MapDrawingColor => new(1f, 0f, 0f);
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 75;
    public override int StartingGold => 99;

    public override CharacterAssetProfile AssetProfile => new(
        new CharacterSceneAssetSet(
            "yalisalin_idle.tscn".CharacterScenePath(CharacterId),
            "yalisalin_energy_counter.tscn".CharacterScenePath(CharacterId),
            "yalisalin_merchant.tscn".CharacterScenePath(CharacterId),
            "yalisalin_rest_site.tscn".CharacterScenePath(CharacterId)),
        new CharacterUiAssetSet(
            IconTexturePath: "yalisalin_map.png".CharacterImgPath(CharacterId),
            IconOutlineTexturePath: "yalisalin_map.png".CharacterImgPath(CharacterId),
            IconPath: "yalisalin_icon.tscn".CharacterScenePath(CharacterId),
            CharacterSelectBgPath: "yalisalin_bg.tscn".CharacterScenePath(CharacterId),
            CharacterSelectIconPath: "yalisalin_char_select.png".CharacterImgPath(CharacterId),
            MapMarkerPath: "yalisalin_map.png".CharacterImgPath(CharacterId)),
        Multiplayer: new CharacterMultiplayerAssetSet(
            "yalisalin_arm_pointing.png".CharacterImgPath(CharacterId),
            "yalisalin_arm_rock.png".CharacterImgPath(CharacterId),
            "yalisalin_arm_paper.png".CharacterImgPath(CharacterId),
            "yalisalin_arm_scissors.png".CharacterImgPath(CharacterId)),
        Audio: new CharacterAudioAssetSet(
            CharacterSelectSfx: $"event:/{ModId}/sfx/characters/sherrylin/select"));

    public override string? PlaceholderCharacterId => "ironclad";
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;
    public override bool RequiresEpochAndTimeline => false;

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return WithAnimationAudio(RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            "yalisalin_idle.tscn".CharacterScenePath(CharacterId)));
    }

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
