using System.Reflection;
using Godot;
using HarmonyLib;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Audio;
using STS2RitsuLib.Interop;
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

    // 这是模组被框架调用的初始化入口。
    public static void Initialize()
    {
        // 加载 FMOD 音频库
        ModTypeDiscoveryHub.RegisterModAssembly("ManosabaLin", Assembly.GetExecutingAssembly());
        FmodStudioServer.TryLoadBank($"res://Manosabalin/audio/banks/Master.bank");
        FmodStudioServer.TryLoadStudioGuidMappings($"res://Manosabalin/audio/banks/GUIDs.txt");

        using (RitsuLibFramework.BeginModDataRegistration("MyMod")) ;

        var ctx = RitsuLibFramework.CreateContentPack(ModId)
            .CardHandOutline<ManosabaCardTemplate>(card =>
                card.GlowColor != null
                    ? null
                    : card.VisualCardPool switch
                    {
                        HiroCardPool => new Color(204f / 255f, 102f / 255f, 102f / 255f),
                        EmalinCardPool => new Color(1f, 0.6f, 0.8f),
                        LinCardPool => new Color(0.8f, 0.8f, 0.8f),
                        _ => null
                    })
            .Apply();

        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        Harmony harmony = new(ModId);
        harmony.PatchAll();
    }
}
