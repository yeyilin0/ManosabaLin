using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Emalin;

public class EmalinRelicPool:TypeListRelicPoolModel
{
    public override string EnergyColorName => "emalin";

    // 遗物实验室描边颜色沿用角色主题色。
    public override Color LabOutlineColor => Emalin.Color;

    // 指定大号能量图标的资源路径。
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();

    // 指定文本行内使用的小号能量图标资源路径。
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}