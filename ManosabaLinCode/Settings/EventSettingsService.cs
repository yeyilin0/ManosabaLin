using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace ManosabaLin.Settings;

public static class EventSettingsService
{
    private const string SettingsLocTable = "settings_ui";
    private const string SettingsDataKey = "event_settings";
    private const string SettingsFileName = "event_settings.json";

    private static bool registeredData;
    private static bool settingsCacheLoaded;
    private static EventSettings cachedSettings = new();

    public static readonly IModSettingsValueBinding<bool> KinmaneyurakuchoEventBinding =
        BoolBinding(
            $"{SettingsDataKey}.kinmaneyurakucho_event",
            () => ReadSettings().KinmaneyurakuchoEvent,
            value => UpdateSettings(settings => settings.KinmaneyurakuchoEvent = value));

    public static readonly IModSettingsValueBinding<bool> MultiplayerCooperationEventBinding =
        BoolBinding(
            $"{SettingsDataKey}.multiplayer_cooperation_event",
            () => ReadSettings().MultiplayerCooperationEvent,
            value => UpdateSettings(settings => settings.MultiplayerCooperationEvent = value));

    public static readonly IModSettingsValueBinding<bool> TeamCardExchangeEventBinding =
        BoolBinding(
            $"{SettingsDataKey}.team_card_exchange_event",
            () => ReadSettings().TeamCardExchangeEvent,
            value => UpdateSettings(settings => settings.TeamCardExchangeEvent = value));

    public static bool IsKinmaneyurakuchoEventEnabled => KinmaneyurakuchoEventBinding.Read();
    public static bool IsMultiplayerCooperationEventEnabled => MultiplayerCooperationEventBinding.Read();
    public static bool IsTeamCardExchangeEventEnabled => TeamCardExchangeEventBinding.Read();

    public static void RegisterSettingsData()
    {
        if (registeredData) return;

        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            RitsuLibFramework.GetDataStore(MainFile.ModId).Register(
                SettingsDataKey,
                SettingsFileName,
                SaveScope.Global,
                () => new EventSettings(),
                autoCreateIfMissing: true);
        }

        registeredData = true;
        LoadSettingsCache();
    }

    public static ModSettingsText T(string key, string fallback)
    {
        return ModSettingsText.LocString(SettingsLocTable, key, fallback);
    }

    private static IModSettingsValueBinding<bool> BoolBinding(
        string dataKey,
        Func<bool> read,
        Action<bool> write)
    {
        return ModSettingsBindings.WithDefault(
            ModSettingsBindings.Callback(MainFile.ModId, dataKey, read, write, SaveSettings),
            () => true);
    }

    private static EventSettings ReadSettings()
    {
        if (!settingsCacheLoaded)
        {
            LoadSettingsCache();
        }

        return cachedSettings;
    }

    private static void LoadSettingsCache()
    {
        try
        {
            cachedSettings = registeredData
                ? RitsuLibFramework.GetDataStore(MainFile.ModId).Get<EventSettings>(SettingsDataKey)
                  ?? new EventSettings()
                : cachedSettings;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[EventSettings] Failed to load settings: {ex.Message}");
            cachedSettings = new EventSettings();
        }

        settingsCacheLoaded = true;
    }

    private static void UpdateSettings(Action<EventSettings> update)
    {
        var settings = ReadSettings();
        update(settings);
        cachedSettings = settings;

        if (!registeredData) return;

        try
        {
            RitsuLibFramework.GetDataStore(MainFile.ModId).Modify<EventSettings>(
                SettingsDataKey,
                persisted =>
                {
                    persisted.KinmaneyurakuchoEvent = cachedSettings.KinmaneyurakuchoEvent;
                    persisted.MultiplayerCooperationEvent = cachedSettings.MultiplayerCooperationEvent;
                    persisted.TeamCardExchangeEvent = cachedSettings.TeamCardExchangeEvent;
                });
            SaveSettings();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[EventSettings] Failed to write settings: {ex.Message}");
        }
    }

    private static void SaveSettings()
    {
        if (!registeredData) return;

        try
        {
            RitsuLibFramework.GetDataStore(MainFile.ModId).Save(SettingsDataKey);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[EventSettings] Failed to save settings: {ex.Message}");
        }
    }
}

public sealed class EventSettings
{
    public bool KinmaneyurakuchoEvent { get; set; } = true;
    public bool MultiplayerCooperationEvent { get; set; } = true;
    public bool TeamCardExchangeEvent { get; set; } = true;
}
