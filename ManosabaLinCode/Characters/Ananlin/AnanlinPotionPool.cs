using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Ananlin;

public class AnanlinPotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "ananlin";
    public override Color LabOutlineColor => Ananlin.Color;
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
