using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Common;

public abstract class ManosabaRelicTemplate : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile
    {
        get
        {
            var slug = GetType().Name.ToLowerInvariant();
            var fileName = $"{slug}.png";

            var icon = fileName.RelicImagePath();
            var resolvedIcon = ResourceLoader.Exists(icon) ? icon : "relic.png".RelicImagePath();

            var outline = $"{slug}_outline.png".RelicImagePath();
            var resolvedOutline = ResourceLoader.Exists(outline) ? outline : "relic_outline.png".RelicImagePath();

            var big = fileName.BigRelicImagePath();
            var resolvedBig = ResourceLoader.Exists(big) ? big : "relic.png".BigRelicImagePath();

            return new RelicAssetProfile(resolvedIcon, resolvedOutline, resolvedBig);
        }
    }
}