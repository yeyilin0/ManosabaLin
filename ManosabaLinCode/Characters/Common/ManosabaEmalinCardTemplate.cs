using Godot;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Common;

public abstract class ManosabaEmalinCardTemplate(
    int energyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ManosabaCardTemplate(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public override CardAssetProfile AssetProfile
    {
        get
        {
            var slug = GetType().Name.ToLowerInvariant();
            var fileName = $"{slug}.png";

            var big = fileName.BigCardsImagePath();
            var small = fileName.CardsImagePath();

            var portrait = ResourceLoader.Exists(big)
                ? big
                : ResourceLoader.Exists(small)
                    ? small
                    : "card.png".CardsImagePath();

            return new CardAssetProfile(portrait, null);
        }
    }
}