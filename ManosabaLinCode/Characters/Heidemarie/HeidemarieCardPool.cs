using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace ManosabaLin.Characters.Heidemarie;

public class HeidemarieCardPool : TypeListCardPoolModel
{
    private const string CharacterIdLower = "heidemarie";

    public override string Title => Heidemarie.CharacterId;
    public override string EnergyColorName => CharacterIdLower;
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateHsvShaderMaterial(0.47f, 0.42f, 1.25f);

    public override Material? PoolFrameMaterial => _poolFrameMaterial;
    public override Color DeckEntryCardColor => Heidemarie.Color;
    public override bool IsColorless => false;
}
