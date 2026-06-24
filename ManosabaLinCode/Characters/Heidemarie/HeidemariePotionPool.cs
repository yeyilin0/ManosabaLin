using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Heidemarie;

public class HeidemariePotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "heidemarie";
    public override Color LabOutlineColor => Heidemarie.Color;
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
