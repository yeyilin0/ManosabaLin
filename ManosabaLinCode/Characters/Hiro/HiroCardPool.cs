using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace ManosabaLin.Characters.Hiro;

// 定义希罗角色卡牌池的标题、能量图标和卡背配色参数。
public class HiroCardPool : TypeListCardPoolModel
{
	private const string CharacterIdLower = "hiro";

	// 卡池标题使用角色 Id，这里不是玩家看到的本地化显示名。
	public override string Title => Hiro.CharacterId;
	public override string EnergyColorName => CharacterIdLower;

	// 指定大号能量图标的资源路径。
	public override string BigEnergyIconPath => "images/characters/Hiro/nikaido_hiro_energy.png".ImagePath();

	// 指定文本行内使用的小号能量图标资源路径。
	public override string TextEnergyIconPath => "images/characters/Hiro/nikaido_hiro_energy.png".ImagePath();
	  private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateRgbShaderMaterial(0.8f, 0.4f, 0.4f);
	public override Material? PoolFrameMaterial => _poolFrameMaterial;
	

	// 如果不想通过 HSV 染色，也可以保持这些值为 1，并自行提供一张自定义卡框。
	/*public override Texture2D CustomFrame(CustomCardModel card)
	{
		// 这里会尝试加载 CharMod/images/cards/frame.png 作为自定义卡框。
		return PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());
	}*/

	// 定义牌组列表中小卡牌图标使用的颜色。
	public override Color DeckEntryCardColor => Hiro.Color;

	// 设为 false 表示这不是无色卡池，而是角色专属卡池。
	public override bool IsColorless => false;
}
