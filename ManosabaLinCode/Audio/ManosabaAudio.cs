using System.Reflection;
using STS2RitsuLib.Audio;

namespace ManosabaLin.Audio;

public static class ManosabaAudio
{
    private const string AudioRoot = "audio";

    private static bool _missingWarned;
    private static bool _playWarned;

    public static bool TryPlayOneShot(string audioRelativePath, float volume = 1f)
    {
        if (string.IsNullOrWhiteSpace(audioRelativePath))
            return false;

        var absolutePath = ResolveAudioFilePath(audioRelativePath);
        if (!File.Exists(absolutePath))
        {
            WarnOnce(ref _missingWarned, $"[Audio] Loose sound file missing: {absolutePath}");
            return false;
        }

        try
        {
            var result = GameAudioService.Shared.PlayOneShot(
                AudioSource.File(absolutePath),
                new AudioPlaybackOptions
                {
                    Volume = volume,
                    DebugName = audioRelativePath
                });

            if (result.Succeeded)
                return true;

            WarnOnce(ref _playWarned,
                $"[Audio] Loose sound playback failed: {absolutePath} ({result.Status}: {result.Message})");
            return false;
        }
        catch (Exception ex)
        {
            WarnOnce(ref _playWarned, $"[Audio] Loose sound playback failed: {absolutePath} ({ex.Message})");
            return false;
        }
    }

    private static string ResolveAudioFilePath(string audioRelativePath)
    {
        var normalizedRelativePath = audioRelativePath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return string.IsNullOrWhiteSpace(assemblyDirectory)
            ? Path.Combine(AudioRoot, normalizedRelativePath)
            : Path.Combine(assemblyDirectory, AudioRoot, normalizedRelativePath);
    }

    private static void WarnOnce(ref bool warned, string message)
    {
        if (warned)
            return;

        warned = true;
        MainFile.Logger.Warn(message);
    }
}