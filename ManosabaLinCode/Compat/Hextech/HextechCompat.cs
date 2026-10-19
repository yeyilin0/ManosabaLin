using System.Reflection;
using STS2RitsuLib;

namespace ManosabaLin.Compat.Hextech;

/// <summary>
///     海克斯符文（HextechRunes）**软依赖**门控。
///     <para>
///         依据海克斯作者发布的《外部模组对接指南》：
///         <list type="bullet">
///             <item>按 <b>程序集名</b> <c>HextechRunes</c> 检测（<b>不是</b> manifest id —— 对方按游戏版本加载不同变体 dll，
///                 二创版也可能用同一程序集名）。</item>
///             <item>模组之间初始化顺序不固定 ⇒ 先查一次已加载程序集，没找到就订阅
///                 <see cref="AppDomain.AssemblyLoad" />，载入后立即注册。</item>
///             <item>注册必须在<b>模组初始化阶段</b>完成（要赶在共享遗物池首次枚举之前）。</item>
///         </list>
///     </para>
///     <para>
///         ⚠️ 本文件<b>刻意不引用</b> <c>HextechRunes.dll</c>：全程字符串 + 反射 ⇒ 对方没装也能正常加载本 mod。
///         因此产物 DLL 里不会出现对 <c>HextechRunes</c> 的任何编译期引用。
///     </para>
    ///     <para>
    ///         ⚠️ <b>绝不对对方的 <c>internal</c> 类（如 <c>HextechCatalog</c>）打 Harmony 补丁</b> —— 指南的立场；
    ///         缺能力就开 issue（草稿见 <c>docs/hextech-compat-hextech.md</c>）。
    ///         用户 2026-10-15 裁定：曾加的「角色专属分区」<b>展示层 Harmony 补丁</b>（postfix
    ///         <c>GetCharacterRuneGroups</c> / <c>GetGenericVisibleRuneTypes</c>，用 <c>SetPlayerRuneCharacter</c>
    ///         登记表反射重建图鉴档位）会导致本地化整体加载失败（仅在海克斯启用时触发），已删除；
    ///         <b>但 <c>SetPlayerRuneCharacter</c> 的角色绑定保留</b>——联动遗物进角色专属池 + 150% 加权靠它，
    ///         且它只登记数据、不碰展示层。已知回归：该绑定会在海克斯启用时间接触发我方全部卡牌 LocKey 前缀
    ///         （<c>MANOSABA_LIN_CARD_</c>）被剥离的本地化问题（根因在海克斯黑盒内），用户裁定保留绑定、
    ///         本地化问题另行向海克斯作者报兼容。
    ///     </para>
/// </summary>
/// <remarks>
///     <para>
///         <b>为什么还有「兜底重试」</b>：对方的实现分两层 —— <c>HextechRunes.Loader</c>（一个独立的
///         <c>[ModInitializer]</c>）在**自己的初始化方法里**才去 <c>LoadFromAssemblyPath</c> 真正的
///         <c>HextechRunes.dll</c> 并调用它的 <c>ModEntry.Initialize()</c>。
///         ⇒ 若本模组的初始化跑在 loader 之前，<see cref="AppDomain.AssemblyLoad" /> 会在
///         <c>LoadFromAssemblyPath</c> 那一刻（= 对方 <c>ModEntry.Initialize()</c> **之前**）触发。
///         单靠事件因此**不是一个确定的安全点**。见 <see cref="SubscribeLateSweeps" />。
///     </para>
/// </remarks>
internal static class HextechCompat
{
    /// <summary>按<b>程序集名</b>检测，不是 manifest id。</summary>
    public const string AssemblyName = "HextechRunes";

    /// <summary>软依赖入口类型全名。</summary>
    public const string InteropTypeName = "HextechRunes.HextechRunesInterop";

    /// <summary>软依赖公开面的最低版本；低于它整体跳过。</summary>
    public const int MinimumApiVersion = 1;

    /// <summary>我方日志前缀（与海克斯侧的 <c>[HextechRunes][ExternalContent]</c> 对得上）。</summary>
    public const string LogPrefix = "[ManosabaLin][Hextech]";

    private static readonly object Gate = new();

    private static bool _listening;
    private static bool _lateSweepsSubscribed;

    /// <summary>已绑定的海克斯程序集；未装为 <c>null</c>。</summary>
    public static Assembly? TargetAssembly { get; private set; }

    /// <summary>已绑定的 <c>HextechRunesInterop</c> 类型；解析失败为 <c>null</c>。</summary>
    public static Type? InteropType { get; private set; }

    /// <summary>对方的 <c>ApiVersion</c>；<c>0</c> = 不可用（太旧 / 取不到）。</summary>
    public static int ApiVersion { get; private set; }

    /// <summary>是否已具备可用的软依赖面。</summary>
    public static bool IsReady =>
        TargetAssembly is not null && InteropType is not null && ApiVersion >= MinimumApiVersion;

    /// <summary>
    ///     在 <c>MainFile.Initialize()</c> 里调用一次。
    ///     已加载 ⇒ 立即绑定并注册；未加载 ⇒ 订阅 <see cref="AppDomain.AssemblyLoad" /> 等它。
    ///     两条路都会再挂上 <see cref="SubscribeLateSweeps" /> 的兜底重试。
    /// </summary>
    public static void Initialize()
    {
        try
        {
            var loaded = FindLoadedAssembly();
            if (loaded is not null)
            {
                TryBind(loaded);
            }
            else
            {
                lock (Gate)
                {
                    if (!_listening)
                    {
                        _listening = true;
                        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
                    }
                }

                Info($"assembly '{AssemblyName}' not loaded yet; waiting for AssemblyLoad (+ late sweeps).");
            }
        }
        catch (Exception ex)
        {
            Warn($"Initialize failed: {ex.Message}");
        }

        SubscribeLateSweeps();
    }

    private static Assembly? FindLoadedAssembly()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(assembly.GetName().Name, AssemblyName, StringComparison.Ordinal))
                return assembly;
        }

        return null;
    }

    private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs e)
    {
        try
        {
            if (!string.Equals(e.LoadedAssembly.GetName().Name, AssemblyName, StringComparison.Ordinal))
                return;

            lock (Gate)
            {
                AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
                _listening = false;
            }

            Info("assembly loaded late; registering now.");
            TryBind(e.LoadedAssembly);
        }
        catch (Exception ex)
        {
            Warn($"AssemblyLoad handler failed: {ex.Message}");
        }
    }

    /// <summary>
    ///     绑定程序集 + 读 <c>ApiVersion</c>；只在拿到可用版本后才去注册。
    ///     <para>
    ///         ⚠️ <b>幂等且可重入</b>：已经绑定并且目录里的符文**全部**登记成功时直接返回；
    ///         否则允许再试一次（<see cref="RunLateSweep" /> 会带着同一个程序集再调进来）。
    ///     </para>
    /// </summary>
    private static void TryBind(Assembly assembly)
    {
        Type? interopType;

        lock (Gate)
        {
            TargetAssembly ??= assembly;

            // 已完成（绑定成功 + 全部符文登记 + 角色全部绑定）⇒ 不再重复解析/重复注册。
            if (IsReady && HextechRuneRegistrar.AllRegistered && HextechRuneRegistrar.AllBound) return;

            ApiVersion = 0;
            InteropType = interopType = assembly.GetType(InteropTypeName, throwOnError: false);

            if (interopType is null)
            {
                Warn($"type '{InteropTypeName}' not found —— 软依赖面不可用，整体跳过。");
                return;
            }

            // 能力探测优先于版本号：ApiVersion 只当「太旧就整体跳过」的门槛。
            var versionProperty = interopType.GetProperty("ApiVersion", BindingFlags.Public | BindingFlags.Static);
            if (versionProperty is null || versionProperty.PropertyType != typeof(int))
            {
                Info($"'{InteropTypeName}.ApiVersion' missing ⇒ 对方版本早于 0.9.6，整体跳过。");
                return;
            }

            ApiVersion = versionProperty.GetValue(null) is int version ? version : 0;
            if (ApiVersion < MinimumApiVersion)
                Info($"ApiVersion={ApiVersion} < {MinimumApiVersion} ⇒ 整体跳过。");
        }

        if (interopType is null || ApiVersion < MinimumApiVersion) return;

        Info($"bound HextechRunes (ApiVersion={ApiVersion}).");
        HextechRuneRegistrar.RegisterAll();
    }

    /// <summary>
    ///     兜底重试：在 <c>GameReady</c> 与 <c>MainMenuReady</c> 两个生命周期点各再尝试一次绑定 + 注册。
    ///     <para>
    ///         这两个点**一定晚于所有模组的初始化**（对方的程序集若装了，此时必定已加载，
    ///         且它自己的 <c>ModEntry.Initialize()</c> 已经跑完），又**早于任何跑局的遗物池首次枚举**
    ///         —— 而注册窗口（<c>ModHelper</c> 的池冻结 / <c>ModelIdSerializationCache.Init</c>）
    ///         正是在那之后才真正关闭。也就是说这是「窗口还开着、对方也准备好了」的最后一个安全时刻。
    ///     </para>
    ///     <para>
    ///         因此它兜住「注册窗口关闭 ⇒ 符文静默少注册 / 角色静默未绑定」的三条路径：
    ///         ① 对方程序集加载时我们还**没订阅** <see cref="AppDomain.AssemblyLoad" />（错过事件）；
    ///         ② 首次尝试发生在**对方自己的初始化完成之前**（loader 先加载 dll、后调它的 <c>Initialize</c>）；
    ///         ③ 首次尝试真的撞上窗口关闭 —— 这一次会以 <c>Error</c> 级日志把原因喊出来，不再静默。
    ///     </para>
    ///     <para>
    ///         ⚠️ 用 <c>SubscribeLifecycleOnce</c> 且默认 <c>replayCurrentState: true</c>：
    ///         即使订阅时该事件已经发生过，也会**同步回放**一次。
    ///     </para>
    /// </summary>
    private static void SubscribeLateSweeps()
    {
        if (HextechRuneRegistrar.AllRegistered && HextechRuneRegistrar.AllBound) return;

        lock (Gate)
        {
            if (_lateSweepsSubscribed) return;
            _lateSweepsSubscribed = true;
        }

        try
        {
            RitsuLibFramework.SubscribeLifecycleOnce<GameReadyEvent>(_ => RunLateSweep("GameReady"));
            RitsuLibFramework.SubscribeLifecycleOnce<MainMenuReadyEvent>(_ => RunLateSweep("MainMenuReady"));
        }
        catch (Exception ex)
        {
            Warn($"late-sweep subscription failed: {ex.Message}");
        }
    }

    private static void RunLateSweep(string whence)
    {
        try
        {
            if (HextechRuneRegistrar.AllRegistered && HextechRuneRegistrar.AllBound) return;

            var assembly = TargetAssembly ?? FindLoadedAssembly();
            if (assembly is null)
            {
                Warn($"late sweep ({whence}): '{AssemblyName}' 仍未加载 ⇒ 符文保持未注册。");
                return;
            }

            Info($"late sweep ({whence}): retrying Hextech rune registration.");
            TryBind(assembly);

            if (!HextechRuneRegistrar.AllRegistered || !HextechRuneRegistrar.AllBound)
                Error($"late sweep ({whence}): 仍未完成（注册/角色绑定）⇒ 见上面 RegisterPlayerRune / SetPlayerRuneCharacter failed 的逐条原因。");
        }
        catch (Exception ex)
        {
            Warn($"late sweep ({whence}) failed: {ex.Message}");
        }
    }

    /// <summary>给 <see cref="HextechRuneRegistrar" /> 用的反射入口；<see cref="IsReady" /> 为 false 时返回 null。</summary>
    public static MethodInfo? ResolveInteropMethod(string name, Type[] parameterTypes)
    {
        if (!IsReady) return null;

        try
        {
            return InteropType!.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: parameterTypes,
                modifiers: null);
        }
        catch (Exception ex)
        {
            Warn($"ResolveInteropMethod('{name}') failed: {ex.Message}");
            return null;
        }
    }

    public static void Info(string message) => MainFile.Logger.Info($"{LogPrefix} {message}");

    public static void Warn(string message) => MainFile.Logger.Warn($"{LogPrefix} {message}");

    public static void Error(string message) => MainFile.Logger.Error($"{LogPrefix} {message}");
}
