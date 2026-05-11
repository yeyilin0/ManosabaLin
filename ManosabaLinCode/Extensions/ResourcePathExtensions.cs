using MegaCrit.Sts2.Core.Models;
using static ManosabaLin.Utils.ResourcePathHelper;

namespace ManosabaLin.Extensions;

public static class ResourcePathExtensions
{
    private static readonly int EncounterPrefixLength = (MainFile.Slug + "_ENCOUNTER_").Length;

    extension(EncounterModel encounter)
    {
        public string BackgroungScenePath
        {
            get
            {
                var id = encounter.Id.Entry[EncounterPrefixLength..].ToLowerInvariant();
                return BuildResPath("scenes", "backgrounds", id, id + ".tscn");
            }
        }

        public string BackgroundLayersDirectoryPath =>
            BuildResPath("scenes", "backgrounds",
                encounter.Id.Entry[EncounterPrefixLength..].ToLowerInvariant(), "layers");

        public string RunHistoryIconPath =>
            BuildResPath("images", "ui", "run_history",
                encounter.Id.Entry[EncounterPrefixLength..].ToLowerInvariant() + ".png");

        public string RunHistoryIconOutlinePath =>
            BuildResPath("images", "ui", "run_history",
                encounter.Id.Entry[EncounterPrefixLength..].ToLowerInvariant() + "_outline.png");

        public string BossMapNodePath =>
            BuildResPath("images", "map", "placeholder",
                encounter.Id.Entry[EncounterPrefixLength..].ToLowerInvariant() + "_icon");
    }
}
