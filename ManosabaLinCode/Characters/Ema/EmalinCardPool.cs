using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace ManosabaLin.Characters.Emalin;

// 定义艾玛角色卡牌池的标题、能量图标和卡背配色参数。
public class EmalinCardPool : TypeListCardPoolModel
{
    private const string CharacterIdLower = "emalin";

    // 卡池标题使用角色 Id，这里不是玩家看到的本地化显示名。
    public override string Title => Emalin.CharacterId;
    public override string EnergyColorName => CharacterIdLower;

    // 指定大号能量图标的资源路径。
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();

    // 指定文本行内使用的小号能量图标资源路径。
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
    public override Material? PoolFrameMaterial => MaterialUtils.CreateHsvShaderMaterial(1f, 0.6f, 0.8f);

    // 如果不想通过 HSV 染色，也可以保持这些值为 1，并自行提供一张自定义卡框。
    /*public override Texture2D CustomFrame(CustomCardModel card)
    {
        // 这里会尝试加载 CharMod/images/cards/frame.png 作为自定义卡框。
        return PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());
    }*/

    // 定义牌组列表中小卡牌图标使用的颜色。
    public override Color DeckEntryCardColor => Emalin.Color;

    // 设为 false 表示这不是无色卡池，而是角色专属卡池。
    public override bool IsColorless => false;
}