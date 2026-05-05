using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Common;

public abstract class ManosabaPotionTemplate : ModPotionTemplate
{
    public override PotionAssetProfile AssetProfile
    {
        get
        {
            var fileName = $"{GetType().Name.ToLowerInvariant()}.png";
            var image = fileName.PotionImagePath();
            var resolvedImage = ResourceLoader.Exists(image) ? image : "power.png".PotionImagePath();

            var outline = fileName.PotionImagePath();
            var resolvedOutline = ResourceLoader.Exists(outline) ? outline : "power.png".PotionImagePath();

            return new PotionAssetProfile(resolvedImage, resolvedOutline);
        }
    }
}