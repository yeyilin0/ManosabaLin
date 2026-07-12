using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
using ManosabaLin.Settings;

namespace ManosabaLin.MainMenu;

public static class MainMenuBackgroundSettingsService
{
    private const string SettingsLocTable = "settings_ui";
    private const string SettingsDataKey = "main_menu_background_settings";
    private const string SettingsFileName = "main_menu_background_settings.json";

    private static bool registeredData;
    private static bool settingsCacheLoaded;
    private static MainMenuBackgroundSettings cachedSettings = new();

    public static readonly IModSettingsValueBinding<bool> EnabledBinding =
        ModSettingsBindings.WithDefault(
            ModSettingsBindings.Callback(
                MainFile.ModId,
                $"{SettingsDataKey}.enabled",
                () => ReadSettings().Enabled,
                value => UpdateSettings(settings => settings.Enabled = value),
                SaveSettings),
            () => true);

    public static bool IsEnabled => EnabledBinding.Read();

    public static void Register()
    {
        RegisterSettingsData();
        EventSettingsService.RegisterSettingsData();
        RitsuLibFramework.RegisterModSettings(
            MainFile.ModId,
            page => page
                .WithTitle(T("MANOSABALIN_SETTINGS.title", "ManosabaLin"))
                .WithModDisplayName(T("MANOSABALIN_SETTINGS.title", "ManosabaLin"))
                .WithDescription(T(
                    "MANOSABALIN_SETTINGS.description",
                    "Adjust ManosabaLin options."))
                .AddSection("main_menu_background", section => section
                    .WithTitle(T(
                        "MANOSABALIN_SETTINGS.mainMenuBackground.section.title",
                        "Main menu background"))
                    .WithDescription(T(
                        "MANOSABALIN_SETTINGS.mainMenuBackground.section.description",
                        "Replace the game main menu with a random ManosabaLin still background."))
                    .AddToggle(
                        "enabled",
                        T(
                            "MANOSABALIN_SETTINGS.mainMenuBackground.enabled.label",
                            "Enable replacement"),
                        EnabledBinding,
                        T(
                            "MANOSABALIN_SETTINGS.mainMenuBackground.enabled.description",
                            "When enabled, one of three still backgrounds is chosen each time the main menu opens.")))
                .AddSection("events", section => section
                    .WithTitle(EventSettingsService.T(
                        "MANOSABALIN_SETTINGS.events.section.title",
                        "Events"))
                    .WithDescription(EventSettingsService.T(
                        "MANOSABALIN_SETTINGS.events.section.description",
                        "Choose which ManosabaLin events can appear in a run."))
                    .AddToggle(
                        "kinmaneyurakuchoEvent",
                        EventSettingsService.T(
                            "MANOSABALIN_SETTINGS.events.kinmaneyurakuchoEvent.label",
                            "Kinmane Yurakucho"),
                        EventSettingsService.KinmaneyurakuchoEventBinding,
                        EventSettingsService.T(
                            "MANOSABALIN_SETTINGS.events.kinmaneyurakuchoEvent.description",
                            "Allow the Kinmane Yurakucho event to appear."))
                    .AddToggle(
                        "multiplayerCooperationEvent",
                        EventSettingsService.T(
                            "MANOSABALIN_SETTINGS.events.multiplayerCooperationEvent.label",
                            "Multiplayer Cooperation"),
                        EventSettingsService.MultiplayerCooperationEventBinding,
                        EventSettingsService.T(
                            "MANOSABALIN_SETTINGS.events.multiplayerCooperationEvent.description",
                            "Allow the Multiplayer Cooperation event to appear."))
                    .AddToggle(
                        "teamCardExchangeEvent",
                        EventSettingsService.T(
                            "MANOSABALIN_SETTINGS.events.teamCardExchangeEvent.label",
                            "Cooperative Testimony"),
                        EventSettingsService.TeamCardExchangeEventBinding,
                        EventSettingsService.T(
                            "MANOSABALIN_SETTINGS.events.teamCardExchangeEvent.description",
                            "Allow the Cooperative Testimony event to appear."))),
            "manosabalin");
    }

    private static void RegisterSettingsData()
    {
        if (registeredData)
        {
            return;
        }

        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            RitsuLibFramework.GetDataStore(MainFile.ModId).Register(
                SettingsDataKey,
                SettingsFileName,
                SaveScope.Global,
                () => new MainMenuBackgroundSettings(),
                autoCreateIfMissing: true);
        }

        registeredData = true;
        LoadSettingsCache();
    }

    private static MainMenuBackgroundSettings ReadSettings()
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
                ? RitsuLibFramework.GetDataStore(MainFile.ModId).Get<MainMenuBackgroundSettings>(SettingsDataKey)
                    ?? new MainMenuBackgroundSettings()
                : cachedSettings;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[MainMenuBackgroundSettings] Failed to load settings: {ex.Message}");
            cachedSettings = new MainMenuBackgroundSettings();
        }

        settingsCacheLoaded = true;
    }

    private static void UpdateSettings(Action<MainMenuBackgroundSettings> update)
    {
        var settings = ReadSettings();
        update(settings);
        cachedSettings = settings;

        if (!registeredData)
        {
            return;
        }

        try
        {
            RitsuLibFramework.GetDataStore(MainFile.ModId).Modify<MainMenuBackgroundSettings>(
                SettingsDataKey,
                persisted => persisted.Enabled = cachedSettings.Enabled);
            SaveSettings();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[MainMenuBackgroundSettings] Failed to write settings: {ex.Message}");
        }
    }

    private static void SaveSettings()
    {
        if (!registeredData)
        {
            return;
        }

        try
        {
            RitsuLibFramework.GetDataStore(MainFile.ModId).Save(SettingsDataKey);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[MainMenuBackgroundSettings] Failed to save settings: {ex.Message}");
        }
    }

    private static ModSettingsText T(string key, string fallback)
    {
        return ModSettingsText.LocString(SettingsLocTable, key, fallback);
    }
}

public sealed class MainMenuBackgroundSettings
{
    public bool Enabled { get; set; } = true;
}
