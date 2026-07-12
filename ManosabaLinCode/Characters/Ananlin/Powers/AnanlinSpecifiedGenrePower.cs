using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinSpecifiedGenrePower : ManosabaPowerTemplate
{
    [SavedProperty] public CardType PreferredType { get; set; } = CardType.Skill;
    [SavedProperty] public bool UpgradeGenerated { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}
