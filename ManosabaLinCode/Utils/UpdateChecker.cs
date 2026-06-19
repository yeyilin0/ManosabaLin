using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ManosabaLin.Utils;

public static class UpdateChecker
{
    private const string GitHubApiUrl = "https://api.github.com/repos/yeyilin0/ManosabaLin/releases/latest";
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;

    private static readonly System.Net.Http.HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static bool _hasCheckedThisSession;
    private static readonly object Lock = new();
    private static string? _cachedVersion;

    public static string LatestVersion { get; private set; } = string.Empty;
    public static string ReleaseUrl { get; private set; } = string.Empty;
    public static string CurrentVersion => GetCurrentVersion();

    static UpdateChecker()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "ManosabaLin-UpdateChecker");
        HttpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    private static string GetCurrentVersion()
    {
        if (_cachedVersion != null)
            return _cachedVersion;

        string? version = null;

        try
        {
            version = GetVersionFromModManager();
            if (!string.IsNullOrEmpty(version))
                MainFile.Logger.Debug($"Got version from ModManager: {version}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Debug($"Failed to get version from ModManager: {ex.Message}");
        }

        if (string.IsNullOrEmpty(version))
        {
            try
            {
                version = GetVersionFromManifest();
                if (!string.IsNullOrEmpty(version))
                    MainFile.Logger.Debug($"Got version from manifest: {version}");
            }
            catch (Exception ex)
            {
                MainFile.Logger.Debug($"Failed to read version from manifest: {ex.Message}");
            }
        }

        if (string.IsNullOrEmpty(version))
        {
            MainFile.Logger.Warn("Failed to get version from all sources, using default v0.0.0");
            return "v0.0.0";
        }

        if (!version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            version = "v" + version;

        _cachedVersion = version;
        return version;
    }

    private static string? GetVersionFromModManager()
    {
        var modManagerType = Type.GetType("MegaCrit.Sts2.Core.Modding.ModManager, sts2");
        if (modManagerType == null) return null;

        var modsProperty = modManagerType.GetProperty("Mods");
        if (modsProperty == null) return null;

        var mods = modsProperty.GetValue(null) as System.Collections.IEnumerable;
        if (mods == null) return null;

        foreach (var mod in mods)
        {
            var manifestField = mod.GetType().GetField("manifest");
            if (manifestField == null) continue;

            var manifest = manifestField.GetValue(mod);
            if (manifest == null) continue;

            var idField = manifest.GetType().GetField("id");
            var versionField = manifest.GetType().GetField("version");

            if (idField == null || versionField == null) continue;

            var id = idField.GetValue(manifest) as string;
            if (id != MainFile.ModId) continue;

            var version = versionField.GetValue(manifest) as string;
            if (!string.IsNullOrEmpty(version))
                return version;
        }

        return null;
    }

    private static string? GetVersionFromManifest()
    {
        var manifestPath = FindManifestPath();
        if (string.IsNullOrEmpty(manifestPath) || !File.Exists(manifestPath))
            return null;

        var jsonContent = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<ModManifestData>(jsonContent);
        return manifest?.Version;
    }

    private static string? FindManifestPath()
    {
        var path = TryGetManifestPathFromModManager();
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            return path;

        path = TryGetManifestPathFromExecutable();
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            return path;

        path = TryGetManifestPathFromAssembly();
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            return path;

        return null;
    }

    private static string? TryGetManifestPathFromModManager()
    {
        try
        {
            var modManagerType = Type.GetType("MegaCrit.Sts2.Core.Modding.ModManager, sts2");
            if (modManagerType == null) return null;

            var modsProperty = modManagerType.GetProperty("Mods");
            if (modsProperty == null) return null;

            var mods = modsProperty.GetValue(null) as System.Collections.IEnumerable;
            if (mods == null) return null;

            foreach (var mod in mods)
            {
                var manifestField = mod.GetType().GetField("manifest");
                if (manifestField == null) continue;

                var manifest = manifestField.GetValue(mod);
                if (manifest == null) continue;

                var idField = manifest.GetType().GetField("id");
                if (idField == null) continue;

                var id = idField.GetValue(manifest) as string;
                if (id != MainFile.ModId) continue;

                var pathField = manifest.GetType().GetField("path");
                if (pathField != null)
                {
                    var modPath = pathField.GetValue(manifest) as string;
                    if (!string.IsNullOrEmpty(modPath))
                        return Path.Combine(modPath, $"{MainFile.ModId}.json");
                }

                var directoryField = manifest.GetType().GetField("directory");
                if (directoryField != null)
                {
                    var directory = directoryField.GetValue(manifest) as string;
                    if (!string.IsNullOrEmpty(directory))
                        return Path.Combine(directory, $"{MainFile.ModId}.json");
                }
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static string? TryGetManifestPathFromExecutable()
    {
        try
        {
            var executablePath = Godot.OS.GetExecutablePath();
            var gameDirectory = Path.GetDirectoryName(executablePath);
            if (string.IsNullOrEmpty(gameDirectory)) return null;

            var modsDirectory = Path.Combine(gameDirectory, "mods");
            if (!Directory.Exists(modsDirectory)) return null;

            var directPath = Path.Combine(modsDirectory, MainFile.ModId, $"{MainFile.ModId}.json");
            if (File.Exists(directPath))
                return directPath;

            foreach (var subDir in Directory.GetDirectories(modsDirectory))
            {
                var manifestPath = Path.Combine(subDir, $"{MainFile.ModId}.json");
                if (File.Exists(manifestPath))
                    return manifestPath;
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static string? TryGetManifestPathFromAssembly()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var location = assembly.Location;
            if (string.IsNullOrEmpty(location)) return null;

            var directory = Path.GetDirectoryName(location);
            if (string.IsNullOrEmpty(directory)) return null;

            var manifestPath = Path.Combine(directory, $"{MainFile.ModId}.json");
            if (File.Exists(manifestPath))
                return manifestPath;

            var parentDir = Directory.GetParent(directory);
            if (parentDir != null)
            {
                manifestPath = Path.Combine(parentDir.FullName, $"{MainFile.ModId}.json");
                if (File.Exists(manifestPath))
                    return manifestPath;
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    public static async Task<UpdateCheckResult> CheckForUpdateAsync()
    {
        lock (Lock)
        {
            if (_hasCheckedThisSession)
                return new UpdateCheckResult { AlreadyChecked = true };
            _hasCheckedThisSession = true;
        }

        MainFile.Logger.Debug("Checking for updates...");

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var response = await HttpClient.GetStringAsync(GitHubApiUrl);
                var releaseInfo = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (releaseInfo == null || string.IsNullOrEmpty(releaseInfo.TagName))
                {
                    MainFile.Logger.Warn("Failed to parse release info");
                    return new UpdateCheckResult { Success = false };
                }

                LatestVersion = releaseInfo.TagName;
                ReleaseUrl = releaseInfo.HtmlUrl ?? "https://github.com/yeyilin0/ManosabaLin";

                var currentVersion = CurrentVersion;
                MainFile.Logger.Debug($"Latest version: {LatestVersion}, Current version: {currentVersion}");

                var hasUpdate = CompareVersions(LatestVersion, currentVersion) > 0;

                return new UpdateCheckResult
                {
                    Success = true,
                    HasUpdate = hasUpdate,
                    CurrentVersion = currentVersion,
                    LatestVersion = LatestVersion,
                    ReleaseUrl = ReleaseUrl
                };
            }
            catch (HttpRequestException ex)
            {
                MainFile.Logger.Debug($"Network error (attempt {attempt}/{MaxRetries}): {ex.Message}");
                if (attempt < MaxRetries)
                    await Task.Delay(RetryDelayMs * attempt);
            }
            catch (TaskCanceledException)
            {
                MainFile.Logger.Debug($"Update check timed out (attempt {attempt}/{MaxRetries})");
                if (attempt < MaxRetries)
                    await Task.Delay(RetryDelayMs * attempt);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Debug($"Unexpected error: {ex.Message}");
                return new UpdateCheckResult { Success = false };
            }
        }

        MainFile.Logger.Debug("Update check failed after all retries");
        return new UpdateCheckResult { Success = false };
    }

    private static int CompareVersions(string version1, string version2)
    {
        static string Normalize(string v) => v.TrimStart('v', 'V');

        var v1Parts = Normalize(version1).Split('.');
        var v2Parts = Normalize(version2).Split('.');

        var maxLength = Math.Max(v1Parts.Length, v2Parts.Length);

        for (var i = 0; i < maxLength; i++)
        {
            var v1Part = i < v1Parts.Length && int.TryParse(v1Parts[i], out var v1) ? v1 : 0;
            var v2Part = i < v2Parts.Length && int.TryParse(v2Parts[i], out var v2) ? v2 : 0;

            if (v1Part != v2Part)
                return v1Part.CompareTo(v2Part);
        }

        return 0;
    }

    public static void ResetCheckState()
    {
        lock (Lock)
        {
            _hasCheckedThisSession = false;
        }
    }

    private class ModManifestData
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }
}

public class UpdateCheckResult
{
    public bool AlreadyChecked { get; set; }
    public bool Success { get; set; }
    public bool HasUpdate { get; set; }
    public string CurrentVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string ReleaseUrl { get; set; } = string.Empty;
}

internal class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }
}
