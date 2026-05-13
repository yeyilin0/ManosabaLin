using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Emalin;

// 定义希罗角色药水池的描边颜色和能量图标资源。
public class EmalinPotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "emalin";

    // 药水实验室描边颜色沿用角色主题色。
    public override Color LabOutlineColor => Emalin.Color;

    // 指定大号能量图标的资源路径。
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();

    // 指定文本行内使用的小号能量图标资源路径。
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}