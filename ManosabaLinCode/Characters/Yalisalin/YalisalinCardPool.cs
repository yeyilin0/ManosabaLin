using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace ManosabaLin.Characters.Yalisalin;

public class YalisalinCardPool : TypeListCardPoolModel, IModColorfulPhilosophersCardPool
{
    private const string CharacterIdLower = "yalisalin";

    public override string Title => Yalisalin.CharacterId;
    public override string EnergyColorName => CharacterIdLower;

    public override string BigEnergyIconPath => "characters/Yalisalin/yalisalin_energy.png".ImagePath();
    public override string TextEnergyIconPath => "characters/Yalisalin/yalisalin_energy_small.png".ImagePath();

    private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateHsvShaderMaterial(0.00f, 1.00f, 1.00f);
    public override Material? PoolFrameMaterial => _poolFrameMaterial;

    public override Color DeckEntryCardColor => Yalisalin.Color;
    public override bool IsColorless => false;
}
