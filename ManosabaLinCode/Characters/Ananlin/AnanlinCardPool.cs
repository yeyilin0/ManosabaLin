using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace ManosabaLin.Characters.Ananlin;

public class AnanlinCardPool : TypeListCardPoolModel, IModColorfulPhilosophersCardPool
{
    private const string CharacterIdLower = "ananlin";

    public override string Title => Ananlin.CharacterId;
    public override string EnergyColorName => CharacterIdLower;

    public override string BigEnergyIconPath => "characters/Ananlin/ananlin_energy.png".ImagePath();
    public override string TextEnergyIconPath => "characters/Ananlin/ananlin_energy_small.png".ImagePath();

    private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateHsvShaderMaterial(0.68f, 0.55f, 1.05f);
    public override Material? PoolFrameMaterial => _poolFrameMaterial;

    public override Color DeckEntryCardColor => Ananlin.Color;
    public override bool IsColorless => false;
}
