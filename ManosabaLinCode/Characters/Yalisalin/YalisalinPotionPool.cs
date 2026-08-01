using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Yalisalin;

public class YalisalinPotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "yalisalin";
    public override Color LabOutlineColor => Yalisalin.Color;
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
