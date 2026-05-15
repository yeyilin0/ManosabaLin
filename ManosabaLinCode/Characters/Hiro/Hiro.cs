using Godot;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace ManosabaLin.Characters.Hiro;

// 定义当前模板角色。
[RegisterCharacter]
public class Hiro : ModCharacterTemplate<HiroCardPool, HiroRelicPool, HiroPotionPool>
{
	// 定义角色 Id，供标题、资源和其他注册逻辑复用。
	public const string CharacterId = "Hiro";


	// 定义角色主色调，这里使用白色作为模板占位颜色。
	public static readonly Color Color = new("B72222");

	// 角色名称显示时使用上面定义的主题色。
	public override Color NameColor => Color;

	// 能量图标轮廓颜色
	public override Color EnergyLabelOutlineColor => new(0.1f, 0.1f, 1f);

	public override Color MapDrawingColor => new (0.8f, 0.4f, 0.4f);

	// 模板角色默认使用中性性别。
	public override CharacterGender Gender => CharacterGender.Feminine;

	// 模板角色的初始生命值设置为 70。
	public override int StartingHp => 70;
	public override int StartingGold => 99;

	public override CharacterAssetProfile AssetProfile => new(
		new CharacterSceneAssetSet(
			null,
			"nikaido_hiro_energy_counter.tscn".CharacterScenePath(CharacterId),
			"nikaido_hiro_merchant.tscn".CharacterScenePath(CharacterId),
			"nikaido_hiro_rest_site.tscn".CharacterScenePath(CharacterId)),
		new CharacterUiAssetSet(
			"hiro_map.png".CharacterImgPath(CharacterId),
			null,
			"nikaido_hiro_icon.tscn".CharacterScenePath(CharacterId),
			"hiro_bg.tscn".CharacterScenePath(CharacterId),
			"hiro_char_select.png".CharacterImgPath(CharacterId),
			null,
			null,
			"hiro_map.png".CharacterImgPath(CharacterId)),
		
		Multiplayer: new CharacterMultiplayerAssetSet(
			"nikaido_hiro_arm_pointing.png".CharacterImgPath(CharacterId),
			"nikaido_hiro_arm_rock.png".CharacterImgPath(CharacterId),
			"nikaido_hiro_arm_paper.png".CharacterImgPath(CharacterId),
			"nikaido_hiro_arm_scissors.png".CharacterImgPath(CharacterId)));

	public override string? PlaceholderCharacterId => "ironclad";
	public override float AttackAnimDelay => 0.15f;
	public override float CastAnimDelay => 0.25f;
	public override bool RequiresEpochAndTimeline => false;

	protected override NCreatureVisuals? TryCreateCreatureVisuals()
	{
		var visuals = RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
			"nikaido_hiro.tscn".CharacterScenePath(CharacterId));
		return visuals;
	}

	// 攻击建筑师的攻击特效列表
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
