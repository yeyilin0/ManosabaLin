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
    }
}
