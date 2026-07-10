using Godot;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace ManosabaLin.Characters.Ananlin;

[RegisterCharacter]
public class Ananlin : ManosabaCharacterTemplate<AnanlinCardPool, AnanlinRelicPool, AnanlinPotionPool>
{
    public const string CharacterId = "Ananlin";

    public static readonly Color Color = new("6666cc");

    public override Color NameColor => Color;
    public override Color EnergyLabelOutlineColor => new(0.4f, 0.4f, 0.8f);
    public override Color MapDrawingColor => new(0.4f, 0.4f, 0.8f);
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 75;
    public override int StartingGold => 99;

    public override CharacterAssetProfile AssetProfile => new(
        new CharacterSceneAssetSet(
            null,
            "ananlin_energy_counter.tscn".CharacterScenePath(CharacterId),
            "ananlin_merchant.tscn".CharacterScenePath(CharacterId),
            "ananlin_rest_site.tscn".CharacterScenePath(CharacterId)),
        new CharacterUiAssetSet(
            "ananlin_map.png".CharacterImgPath(CharacterId),
            null,
            "ananlin_icon.tscn".CharacterScenePath(CharacterId),
            "ananlin_bg.tscn".CharacterScenePath(CharacterId),
            "ananlin_char_select.png".CharacterImgPath(CharacterId),
            null,
            null,
            "ananlin_map.png".CharacterImgPath(CharacterId)),
        Multiplayer: new CharacterMultiplayerAssetSet(
            "ananlin_arm_pointing.png".CharacterImgPath(CharacterId),
            "ananlin_arm_rock.png".CharacterImgPath(CharacterId),
            "ananlin_arm_paper.png".CharacterImgPath(CharacterId),
            "ananlin_arm_scissors.png".CharacterImgPath(CharacterId)));

    public override string? PlaceholderCharacterId => "ironclad";
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;
    public override bool RequiresEpochAndTimeline => false;

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            "ananlin.tscn".CharacterScenePath(CharacterId));
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
