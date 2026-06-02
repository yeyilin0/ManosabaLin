using STS2RitsuLib.Audio;

namespace ManosabaLin.Audio.Services;

public static class FmodHelper
{
    public static bool IsEventExists(string eventPath)
    {
        return FmodStudioServer.TryCheckEventPath(eventPath) ?? false;
    }
}
