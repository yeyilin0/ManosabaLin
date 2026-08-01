using Godot;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace ManosabaLin.Characters.Sherrylin;

[RegisterCharacter]
public class Sherrylin : ManosabaCharacterTemplate<SherrylinCardPool, SherrylinRelicPool, SherrylinPotionPool>
{
    public const string CharacterId = "Sherrylin";

    public static readonly Color Color = new("33ccff");

    public override Color NameColor => Color;
    public override Color EnergyLabelOutlineColor => new(0.2f, 0.8f, 1f);
    public override Color MapDrawingColor => new(0.2f, 0.8f, 1f);
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 75;
    public override int StartingGold => 99;

    public override CharacterAssetProfile AssetProfile => new(
        new CharacterSceneAssetSet(
            null,
            "sherrylin_energy_counter.tscn".CharacterScenePath(CharacterId),
            "sherrylin_merchant.tscn".CharacterScenePath(CharacterId),
            "sherrylin_rest_site.tscn".CharacterScenePath(CharacterId)),
        new CharacterUiAssetSet(
            IconTexturePath: "sherrylin_map.png".CharacterImgPath(CharacterId),
            IconOutlineTexturePath: "sherrylin_map.png".CharacterImgPath(CharacterId),
            IconPath: "sherrylin_icon.tscn".CharacterScenePath(CharacterId),
            CharacterSelectBgPath: "sherrylin_bg.tscn".CharacterScenePath(CharacterId),
            CharacterSelectIconPath: "sherrylin_char_select.png".CharacterImgPath(CharacterId),
            MapMarkerPath: "sherrylin_map.png".CharacterImgPath(CharacterId)),
        Multiplayer: new CharacterMultiplayerAssetSet(
            "sherrylin_arm_pointing.png".CharacterImgPath(CharacterId),
            "sherrylin_arm_rock.png".CharacterImgPath(CharacterId),
            "sherrylin_arm_paper.png".CharacterImgPath(CharacterId),
            "sherrylin_arm_scissors.png".CharacterImgPath(CharacterId)));

    public override string? PlaceholderCharacterId => "ironclad";
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;
    public override bool RequiresEpochAndTimeline => false;

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        var visuals = RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            "sherrylin.tscn".CharacterScenePath(CharacterId));
        return visuals;
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
