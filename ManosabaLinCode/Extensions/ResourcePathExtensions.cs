using MegaCrit.Sts2.Core.Models;
using static ManosabaLin.Utils.ResourcePathHelper;

namespace ManosabaLin.Extensions;

public static class ResourcePathExtensions
{
    extension(EncounterModel encounter)
    {
        public string BackgroungScenePath
        {
            get
            {
                var id = encounter.Id.Entry.ToLowerInvariant();
                return BuildResPath("scenes", "backgrounds", id, id + ".tscn");
            }
        }

        public string BackgroundLayersDirectoryPath =>
            BuildResPath("scenes", "backgrounds", encounter.Id.Entry.ToLowerInvariant(), "layers");

        public string RunHistoryIconPath =>
            BuildResPath("images", "ui", "run_history", encounter.Id.Entry.ToLowerInvariant() + ".png");

        public string RunHistoryIconOutlinePath =>
            BuildResPath("images", "ui", "run_history", encounter.Id.Entry.ToLowerInvariant() + "_outline.png");

        public string BossMapNodePath =>
            BuildResPath("images", "map", "placeholder", encounter.Id.Entry.ToLowerInvariant() + "_icon");
    }
}
