namespace ManosabaLin.Characters.Ananlin.Cards;

public abstract class AnanlinNonRandomCardTemplate(
    int energyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = false)
    : ManosabaCardTemplate(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
}
