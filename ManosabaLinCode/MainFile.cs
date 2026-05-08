using System.Reflection;
using Godot;
using HarmonyLib;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Scaffolding.Cards.HandOutline;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace ManosabaLin;

// 告诉模组加载器在启动时调用当前类的 Initialize 方法。
[ModInitializer(nameof(Initialize))]
// 定义当前模组的主入口节点。
public partial class MainFile : Node
{
    // 定义模组唯一 Id，后续会被资源路径和日志器复用。
    public const string ModId = "ManosabaLin";

    // 拼出当前模组在 Godot 资源系统中的根目录。
    public const string ResPath = $"res://{ModId}";

    // 创建全局日志器，方便在模组运行时输出统一前缀的日志。
    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    // 这是模组被框架调用的初始化入口。
    public static void Initialize()
    {
        var ctx = RitsuLibFramework.CreateContentPack(ModId)
            .CardHandOutline<ManosabaCardTemplate>(new ModCardHandOutlineRule(
                card => true,
                new Color(204f / 255f, 102f / 255f, 102f / 255f)
            ))
            .Apply();
        
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        // 使用模组 Id 创建 Harmony 实例，便于标识补丁来源。
        Harmony harmony = new(ModId);


        // 扫描当前程序集并应用所有 Harmony 补丁。
        harmony.PatchAll();
    }
}