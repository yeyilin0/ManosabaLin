using HarmonyLib;
using ManosabaLin.Characters.Ananlin;
using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ema.Cards;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Characters.Sherrylin;
using ManosabaLin.Characters.Sherrylin.Cards;
using ManosabaLin.Characters.Yalisalin;
using ManosabaLin.Characters.Yalisalin.Components;
using ManosabaLin.MainMenu;
using ManosabaLin.Utils;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;
using STS2RitsuLib.Audio;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace ManosabaLin;

// 告诉模组加载器在启动时调用当前类的 Initialize 方法。
[ModInitializer(nameof(Initialize))]
// 定义当前模组的主入口节点。
public partial class MainFile : Node
{
    // 定义模组唯一 Id，后续会被资源路径和日志器复用。
    public const string ModId = "ManosabaLin";

    // 定义模组 Slug，通常用于资源路径中，保持小写和连字符风格。
    public static readonly string Slug = StringHelper.Slugify(ModId);

    // 拼出当前模组在 Godot 资源系统中的根目录。
    public const string ResPath = $"res://{ModId}";

    // 创建全局日志器，方便在模组运行时输出统一前缀的日志。
    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    // 雪莉琳专属额外牌堆
    public static PileType CaseFilePile;

    // 这是模组被框架调用的初始化入口。
    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        using (RitsuLibFramework.BeginModDataRegistration(ModId));
        MainMenuBackgroundSettingsService.Register();
        RegisterManosabaLinTelemetry();

        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        // 加载 FMOD 音频库
        FmodStudioServer.TryLoadBank($"res://{ModId}/audio/{ModId}.bank");
        FmodStudioServer.TryLoadStudioGuidMappings($"res://{ModId}/audio/GUIDs.txt");


        var ctx = RitsuLibFramework.CreateContentPack(ModId)
            .CardHandOutline<ManosabaCardTemplate>(card =>
                card.GlowColor != null
                    ? null
                    : card.VisualCardPool switch
                    {
                        HiroCardPool => new Color(204f / 255f, 102f / 255f, 102f / 255f),
                        EmalinCardPool => new Color(1f, 0.6f, 0.8f),
                        SherrylinCardPool => new Color(0.2f, 0.8f, 1f),
                        YalisalinCardPool => new Color(0.67f, 0.4f, 0.8f),
                        LinCardPool => new Color(0.8f, 0.8f, 0.8f),
                        _ => null
                    })
            .CardHandOutline<CardModel>(card =>
                YalisalinFireComponentRules.HasFireComponent(card)
                    ? new Color(1f, 0.42f, 0.08f)
                    : null)
            .DustyTomeCard<Hiro, HiroWith>()
            .DustyTomeCard<Emalin, Emawichpower>()
            .DustyTomeCard<Sherrylin, SherrylinWitchPower>()
            .DustyTomeCard<Ananlin, AnanlinWitchPower>()
            .Apply();
// 注册雪莉琳专属「案卷」牌堆
        CaseFilePile = ModCardPileRegistry.For(ModId)
            .RegisterOwned("case_file_pile", new ModCardPileSpec
            {
                Scope = ModCardPileScope.RunPersistent,
                Style = ModCardPileUiStyle.TopBarDeck,
                Anchor = new ModCardPileAnchor(
                    ModCardPileAnchorKind.TopBarAfterDeck),
                IconPath = $"res://{ModId}/images/ui/case_file_pile.png",
                OnOpen = ctx => ctx.ShowDefaultPileScreen(),
                VisibleWhen = ctx => ctx.Player?.Character is Sherrylin,
            }).PileType;

        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);

        Harmony harmony = new(ModId);
        harmony.PatchAll();

        _ = CheckUpdateAsync();
    }

    private static void RegisterManosabaLinTelemetry()
    {
        TelemetryRegistry.RegisterApplicant(new()
        {
            ApplicantId = ModId,
            OwnerModId = ModId,
            DisplayName = ModId,
            DisplayNameText = ModSettingsText.LocString("settings_ui", "MANOSABALIN_TELEMETRY_NAME", ModId),
            Adapter = new PostHogTelemetryAdapter(
                host: "https://telemetry.r9jji.icu",
                projectApiKey: "proxy"
            ),
            Requests =
            [
                TelemetryRequest.BasicUsage(ModSettingsText.LocString("settings_ui", "MANOSABALIN_TELEMETRY_USAGE", "Basic usage data")),
                TelemetryRequest.ModInventory(ModSettingsText.LocString("settings_ui", "MANOSABALIN_TELEMETRY_MODS", "Mod list")),
                TelemetryRequest.Diagnostics(ModSettingsText.LocString("settings_ui", "MANOSABALIN_TELEMETRY_DIAGNOSTICS", "Diagnostics")),
                TelemetryRequest.RunHistory(ModSettingsText.LocString("settings_ui", "MANOSABALIN_TELEMETRY_RUNS", "Run history"))
            ],
        });
    }

    private static async Task CheckUpdateAsync()
    {
        try
        {
            var result = await UpdateChecker.CheckForUpdateAsync();
            if (result is { Success: true, HasUpdate: true })
            {
                Logger.Info($"New version available: {result.LatestVersion} (current: {result.CurrentVersion})");
                await Task.Delay(2000);
                var tree = (SceneTree?)Engine.GetMainLoop();
                if (tree?.Root != null)
                {
                    var popup = UpdatePopup.Create(result.CurrentVersion, result.LatestVersion, result.ReleaseUrl);
                    if (popup != null)
                        tree.Root.CallDeferred(Node.MethodName.AddChild, popup);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"Update check failed: {ex.Message}");
        }
    }
}
