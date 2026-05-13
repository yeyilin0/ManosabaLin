using System;
using ManosabaLin.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Common;

public abstract class ManosabaAncientEventTemplate : ModAncientEventTemplate
{
    public override EventAssetProfile AssetProfile
    {
        get
        {
            var slug = GetType().Name.ToLowerInvariant();
            return new EventAssetProfile(
                BackgroundScenePath: $"{slug}.tscn".EventBackgroundScenePath()
            );
        }
    }

    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile
    {
        get
        {
            var slug = GetType().Name.ToLowerInvariant();
            var mapIconPath = $"{slug}.png".AncientMapIconPath();
            var mapIconOutlinePath = $"{slug}_outline.png".AncientMapIconPath();

            var runHistoryPath = $"{slug}.png".RunHistoryIconPath();
            var runHistoryOutlinePath = $"{slug}_outline.png".RunHistoryIconPath();

            return new AncientEventPresentationAssetProfile(
                MapIconPath: mapIconPath,
                MapIconOutlinePath: mapIconOutlinePath,
                RunHistoryIconPath: runHistoryPath,
                RunHistoryIconOutlinePath: runHistoryOutlinePath
            );
        }
    }
}
