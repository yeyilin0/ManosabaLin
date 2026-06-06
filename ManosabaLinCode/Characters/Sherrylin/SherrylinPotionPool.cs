using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Sherrylin;

public class SherrylinPotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "sherrylin";
    public override Color LabOutlineColor => Sherrylin.Color;
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
