using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace ManosabaLin.Characters.Sherrylin;

public class SherrylinCardPool : TypeListCardPoolModel
{
    private const string CharacterIdLower = "sherrylin";

    public override string Title => Sherrylin.CharacterId;
    public override string EnergyColorName => CharacterIdLower;

    public override string BigEnergyIconPath => "characters/Sherrylin/sherrylin_energy.png".ImagePath();
    public override string TextEnergyIconPath => "characters/Sherrylin/sherrylin_energy_small.png".ImagePath();

    private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateHsvShaderMaterial(0.55f, 0.6f, 1.2f);
    public override Material? PoolFrameMaterial => _poolFrameMaterial;

    public override Color DeckEntryCardColor => Sherrylin.Color;
    public override bool IsColorless => false;
}
