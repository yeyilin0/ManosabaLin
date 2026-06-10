using STS2RitsuLib.Telemetry;
using STS2RitsuLib.Updates;

namespace ManosabaLin.Telemetry;

internal static class TelemetryBootstrap
{
    private const string Domain = "manosabalin.fuynaloft.top";
    private const string ReleasePageUrl = "https://github.com/yeyilin0/ManosabaLin/releases";

    private static bool _initialized;

    internal static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;
        RegisterApplicant();

        var versionStr = typeof(TelemetryBootstrap).Assembly.GetName().Version!.ToString(4);
        ModUpdateChecker.RegisterOnFirstMainMenu(
            modId: ModId,
            displayName: "魔法少女的魔女裁决",
            currentVersion: versionStr,
            manifestUrl: $"https://{Domain}/update-manifest.json",
            releasePageUrl: ReleasePageUrl
        );
    }

    private static void RegisterApplicant()
    {
        TelemetryRegistry.RegisterApplicant(new TelemetryApplicant
        {
            ApplicantId = ModId,
            OwnerModId = ModId,
            DisplayName = "魔法少女的魔女裁决",
            Adapter = new HttpJsonTelemetryAdapter($"https://{Domain}/ingest"),
            Requests = new List<TelemetryRequest>
            {
                TelemetryRequest.BasicUsage("Session start, framework/game versions, platform, language, and anonymous install id."),
                TelemetryRequest.ModInventory("Installed mod list, versions, and load states for compatibility analysis."),
                TelemetryRequest.Diagnostics("Exception reports and runtime diagnostics."),
                TelemetryRequest.RunHistory("Complete run history after each run ends, for balance analysis.")
            }
        });
    }
}
