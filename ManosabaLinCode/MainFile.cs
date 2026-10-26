using HarmonyLib;
using ManosabaLin.Characters.Ananlin;
using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Common.AncientCurses;
using ManosabaLin.Characters.Common.LinRelics;
using ManosabaLin.Characters.Ema.Cards;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Characters.Hiro.Rewards;
using ManosabaLin.Characters.Sherrylin;
using ManosabaLin.Characters.Sherrylin.Cards;
using ManosabaLin.Characters.Yalisalin;
using ManosabaLin.Characters.Yalisalin.Components;
using ManosabaLin.Compat.Hextech;
using ManosabaLin.MainMenu;
using ManosabaLin.Utils;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Utilities.CustomGlowColor;
using STS2RitsuLib;
using STS2RitsuLib.Audio;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace ManosabaLin;

// ⚠️ 注意：本工程当前是单版本布局（MainFile 与可分发 dll 同一程序集），不存在独立的
// ManosabaLin.Loader 工程，因此必须挂 [ModInitializer] 让框架调用本初始化方法。
// 否则框架只会走兜底路径 Harmony.PatchAll，下面这一整批初始化（语音库加载、海克斯联动、
// 情绪球/附魔钩子、内容包扩展）会全部静默失效。
// 定义当前模组的主入口节点。
[ModInitializer(nameof(Initialize))]
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
        // 加载 FMOD 音频库（角色选择/语音）。非致命：即使 bank 缺失也绝不能阻断后续初始化。
        try
        {
            FmodStudioServer.TryLoadBank($"res://{ModId}/audio/{ModId}.bank");
            FmodStudioServer.TryLoadStudioGuidMappings($"res://{ModId}/audio/GUIDs.txt");
        }
        catch (Exception ex)
        {
            Logger.Warn($"FMOD bank load failed (voice disabled, continuing): {ex.Message}");
        }


        var ctx = RitsuLibFramework.CreateContentPack(ModId)
            .CardHandOutline<CardModel>(DefaultCardHandOutlineColor)
            .CardHandOutline<CardModel>(card =>
                HasCustomGlowColor(card) ? null : BondCardVisualRules.GetHandOutlineColor(card),
                priority: 200)
            .CardHandOutline<CardModel>(card =>
                card.CanPlay() && !HasCustomGlowColor(card) && YalisalinFireComponentRules.HasFireComponent(card)
                    ? new Color(1f, 0.42f, 0.08f)
                    : null,
                priority: 100)
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

        // 注册「残骸首领战胜奖励」的自定义 reward 类型（读档重建与联机同步都依赖它）。
        GuardOneRewardRegistrar.Register();

        // 联动遗物 2：「脱离球位、挂到血条下方」的持续型情绪球要作为战斗钩子监听者被枚举
        // （ModHelper.SubscribeForCombatStateHooks）⇒ 球离开球位后效果照旧、不需要任何能力。
        HangingEmotionOrbs.Register();

        // 审判具现：卡内「具现附魔」也要收战斗钩子 —— 原版不少附魔的效果是靠钩子实现的
        // （Goopy/Glam/Vigorous 的 AfterCardPlayed、Slither 的 AfterCardDrawn、Imbued 的
        //  AfterAutoPrePlayPhaseEntered…），附魔离开附魔槽后必须由遗物把它们登记成监听者，
        // 否则那些效果会静默失效。与 HangingEmotionOrbs 同一套机制。
        EmbodiedEnchantmentHooks.Register();

        // 海克斯符文联动（软依赖、零 patch）：装了 HextechRunes 就把这 6 个遗物作为「海克斯符文」登记过去。
        // ⚠️ 必须在模组初始化阶段调用（要赶在共享遗物池首次枚举之前）；对方没装时这里是空操作。
        HextechCompat.Initialize();

        Harmony harmony = new(ModId);
        harmony.PatchAll();

        _ = CheckUpdateAsync();
    }

    private static Color? DefaultCardHandOutlineColor(CardModel card)
    {
        if (!card.CanPlay() || HasSpecialGlowColor(card))
            return null;

        // 原罪诅咒卡（含魔女化诅咒卡）：发白色光
        if (card is LinAncientCurseCard)
            return new Color("#ffffff");

        return card.VisualCardPool switch
        {
            AnanlinCardPool => Ananlin.Color,
            HirolinCardPool => Hiro.Color,
            EmalinCardPool => Emalin.Color,
            SherrylinCardPool => Sherrylin.Color,
            YalisalinCardPool => new Color("#ff0000"),
            LinCardPool => OwnerCharacterColor(card) ?? new Color(0.8f, 0.8f, 0.8f),
            _ => null
        };
    }

    private static bool HasCustomGlowColor(CardModel card)
    {
        return card is ICustomGlowColorCard { GlowColor: not null };
    }

    private static bool HasSpecialGlowColor(CardModel card)
    {
        return HasCustomGlowColor(card) || card.ShouldGlowGold || card.ShouldGlowRed;
    }

    private static Color? OwnerCharacterColor(CardModel card)
    {
        return card.Owner?.Character switch
        {
            Ananlin => Ananlin.Color,
            Hiro => Hiro.Color,
            Emalin => Emalin.Color,
            Sherrylin => Sherrylin.Color,
            Yalisalin => Yalisalin.Color,
            _ => null
        };
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
