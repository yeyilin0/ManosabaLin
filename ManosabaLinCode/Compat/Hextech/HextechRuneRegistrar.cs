using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ManosabaLin.Compat.Hextech;

/// <summary>
///     注册桥：把 <see cref="HextechRuneCatalog" /> 里的我方符文交给海克斯的**公开注册 API**。
///     <para>
///         <b>零 Harmony 补丁</b>（指南明令禁止 patch 对方的 <c>internal</c> 类），
///         全部通过反射调用 <c>HextechRunes.HextechRunesInterop</c> 的 public static 成员。
///     </para>
///     <para>
///         <b>能力探测优先于版本号</b>：每个成员单独用
///         <see cref="HextechCompat.ResolveInteropMethod" /> 解析，解析不到就<b>只降级那一步</b>，
///         不整体放弃。⚠️ 指南的签名可能与本机版本不一致（本机版本可能更旧）—— 这是有意的健壮性设计。
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         <b>逐符文幂等</b>：登记成功的符文记在 <see cref="RegisteredRunes" /> 里，重复调用
///         <see cref="RegisterAll" /> 只补「还没成功的那几个」。
///         ⇒ <see cref="HextechCompat" /> 的兜底重试（<c>GameReady</c> / <c>MainMenuReady</c>）
///         可以放心地再调一次，不会重复注册、也不会打断已成功的部分。
///     </para>
///     <para>
///         ⚠️ <b>注册失败必须吵</b>：逐符文 <c>try/catch</c> 是为了「绝不让单个符文打断整个 mod 的初始化」，
///         但代价是失败**不会崩**、只是那几张牌整局不出现（还完全没有别的症状）。
///         所以「不齐」时走 <c>Error</c> 级日志 + 逐条原因，不允许静默降级。
///     </para>
/// </remarks>
internal static class HextechRuneRegistrar
{
    // ⚠️ 参数类型列表必须与指南签名逐字一致，否则 GetMethod 返回 null（= 该能力不可用）。
    private static readonly Type[] RegisterPlayerRuneParameters =
    [
        typeof(Type), typeof(string), typeof(string), typeof(string), typeof(int),
        typeof(string), typeof(string), typeof(Func<Player, bool>)
    ];

    private static readonly Type[] SetPlayerRuneCharacterParameters = [typeof(Type), typeof(Type)];

    private static readonly Type[] SetPlayerRunePoolLabelParameters = [typeof(Type), typeof(string)];

    private static readonly Type[] RegisterConfigSectionTitleParameters = [typeof(string), typeof(string)];

    private static readonly object Gate = new();

    /// <summary>已经成功登记到海克斯注册表里的符文（按类型名）。</summary>
    private static readonly HashSet<string> RegisteredRunes = new(StringComparer.Ordinal);

    /// <summary>已经通过 <c>SetPlayerRuneCharacter</c> 完成角色绑定的符文（按类型名）。</summary>
    private static readonly HashSet<string> BoundRunes = new(StringComparer.Ordinal);

    private static bool _auxiliaryDone;

    /// <summary>
    ///     目录里的符文是否**全部**登记成功。
    ///     <para>
    ///         给两层用：① <see cref="HextechCompat.TryBind" /> 判断「还需不需要再绑一次」；
    ///         ② <see cref="HextechCompat.SubscribeLateSweeps" /> 判断「还需不需要挂兜底重试」。
    ///     </para>
    /// </summary>
    public static bool AllRegistered
    {
        get
        {
            lock (Gate)
                return RegisteredRunes.Count >= HextechRuneCatalog.All.Count;
        }
    }

    /// <summary>
    ///     对方 <c>ApiVersion &gt;= 2</c> 时 <c>SetPlayerRuneCharacter</c> 可用 ⇒ 角色绑定才有意义。
    ///     <para>低于该版本则恒为 false（没有可绑的东西，<see cref="AllBound" /> 退化为 true）。</para>
    /// </summary>
    public static bool BindingAvailable => HextechCompat.ApiVersion >= 2;

    /// <summary>
    ///     目录里的符文是否**全部**完成角色绑定（仅在 <see cref="BindingAvailable" /> 时为有意义约束）。
    /// </summary>
    public static bool AllBound
    {
        get
        {
            if (!BindingAvailable) return true;
            lock (Gate)
                return BoundRunes.Count >= HextechRuneCatalog.All.Count;
        }
    }

    /// <summary>执行一次注册（幂等：已成功的符文不会再注册，可安全重复调用）。</summary>
    public static void RegisterAll()
    {
        if (!HextechCompat.IsReady)
        {
            HextechCompat.Info("not ready ⇒ skip registration.");
            return;
        }

        // 目录自检放在最前面：把「配错了但编译得过」变成显式日志。
        var problems = HextechRuneCatalog.Validate();
        foreach (var problem in problems)
            HextechCompat.Warn($"catalog problem: {problem}");

        RegisterRunes();

        // 来源标签 / 配置菜单分组标题 + 角色绑定，只对「已登记的符文」有意义；符文不齐时先做一次尽力而为，
        // 并把「已完成」标记留空 —— 等兜底重试把剩余符文补上后，这里会再做一次。
        if (!_auxiliaryDone)
        {
            RegisterPoolLabels();
            RegisterConfigSection();
            BindCharacters();
            _auxiliaryDone = AllRegistered && AllBound;
        }
    }

    private static void RegisterRunes()
    {
        var register = HextechCompat.ResolveInteropMethod("RegisterPlayerRune", RegisterPlayerRuneParameters);
        if (register is null)
        {
            HextechCompat.Error(
                $"'RegisterPlayerRune' 解析失败（对方版本可能早于本指南）⇒ {HextechRuneCatalog.All.Count} 个符文全部未注册。");
            return;
        }

        // R4（2026-10-13 作者回复，施工单见 Downloads/hextech-r4-reply-to-manosabalin.md）：
        // ① ApiVersion >= 2 时 flags 追加 GuaranteedFirstPick ⇒ 每幕第一次三选一保底塞一个专属符文（放第三格）；
        // ② 同版本新增 SetPlayerRuneCharacter ⇒ 模组角色也吃到 150% 角色加权 + 进角色专属档。
        // ⚠️ 旧版本不认识 GuaranteedFirstPick，传了会抛 ArgumentException ⇒ 严格按版本门控，不传就是没有保底。
        var hasGuaranteedPick = HextechCompat.ApiVersion >= 2;

        var newlyRegistered = new List<string>();
        var failures = new List<(string Name, Exception Error)>();
        var pending = 0;

        foreach (var spec in HextechRuneCatalog.All)
        {
            var name = spec.RuneType.Name;
            var registered = IsRegistered(name);

            if (!registered)
            {
                pending++;

                object?[] args =
                [
                    spec.RuneType,
                    spec.Rarity,
                    ComposeFlags(spec.Flags, hasGuaranteedPick),
                    spec.CharacterPool,
                    spec.CharacterOrder,
                    spec.TagKey,
                    spec.AssetModId,
                    spec.Availability
                ];

                try
                {
                    register.Invoke(null, args);
                    MarkRegistered(name);
                    newlyRegistered.Add(name);
                    registered = true;
                }
                catch (TargetInvocationException ex) when (ex.InnerException is not null)
                {
                    // 参数无效 ⇒ 对方抛 ArgumentException，被反射包成 TargetInvocationException。
                    // ⚠️ 必须捕获：绝不能让单个符文打断整个 mod 的初始化。
                    failures.Add((name, ex.InnerException));
                }
                catch (Exception ex)
                {
                    failures.Add((name, ex));
                }
            }
        }

        // 本次没有任何待办（注册都完成了）⇒ 不必重复打日志。
        if (pending == 0) return;

        var total = RegisteredCount;
        var count = HextechRuneCatalog.All.Count;

        if (total < count)
        {
            // ⚠️ 这里**不能**只是 Warn：少注册 = 那几张牌整局不出现，且没有任何其它症状。
            // 用 Error 级 + 逐条原因，把「静默少注册」变成一眼可见（唯一可靠的验收手段就是看这行）。
            HextechCompat.Error(
                $"RegisterPlayerRune: ok={total}/{count} —— {count - total} 个符文**未注册**，" +
                "它们在本局里永远不会出现在海克斯的符文三选一里。" +
                "若原因是注册窗口已关闭（异常里提到 pool 或 ModelIdSerializationCache），" +
                "说明海克斯比本模组晚加载到了窗口之外。");

            foreach (var (name, error) in failures)
                HextechCompat.Error($"RegisterPlayerRune failed: {name}({error.GetType().Name}: {error.Message})");
        }
        else
        {
            HextechCompat.Info(
                $"RegisterPlayerRune: ok={total}/{count} [{string.Join(", ", RegisteredNames())}]" +
                (newlyRegistered.Count > 0 ? $" (+{newlyRegistered.Count} this call)" : string.Empty) +
                (hasGuaranteedPick ? " [flags=GuaranteedFirstPick]" : " [no guarantee: ApiVersion<2]"));
        }
    }

    /// <summary>
    ///     角色绑定：经 <c>SetPlayerRuneCharacter</c> 把每个符文登记到专属我方角色，
    ///     让这些符文在海克斯侧吃到 150% 角色加权、并归属角色专属档（而非通用档）。
    ///     <para>
    ///         ⚠️ 已知回归：该调用会在<b>海克斯启用时</b>间接触发我方全部卡牌 LocKey 前缀
    ///         （<c>MANOSABA_LIN_CARD_</c>）被剥离，导致本地化整体显示成无前缀短键（如 <c>SHERRYLIN_NYXM</c>）。
    ///         根因在海克斯黑盒/加载顺序内（反编译确认海克斯不扫我方 <c>relic_collection</c>、也不 patch 我方
    ///         <c>LocTable</c>/卡牌前缀，仅把我们的类型存进自己的注册表；但注册行为本身扰乱了 RitsuLib
    ///         对本 mod 内容前缀的判定）。用户 2026-10-15 最终裁定：<b>保留此绑定</b>（联动遗物进专属池优先），
    ///         本地化问题改由向海克斯作者报兼容问题另行解决。
    ///     </para>
    /// </summary>
    private static void BindCharacters()
    {
        if (!BindingAvailable)
        {
            HextechCompat.Info("SetPlayerRuneCharacter 不可用（ApiVersion<2）⇒ 跳过角色绑定。");
            return;
        }

        var bind = HextechCompat.ResolveInteropMethod("SetPlayerRuneCharacter", SetPlayerRuneCharacterParameters);
        if (bind is null)
        {
            HextechCompat.Warn("'SetPlayerRuneCharacter' 解析失败 ⇒ 符文保持通用档（无 150% 角色加权）。");
            return;
        }

        var failures = new List<(string Name, Exception Error)>();
        var pending = 0;

        foreach (var spec in HextechRuneCatalog.All)
        {
            var name = spec.RuneType.Name;
            if (IsBound(name)) continue;

            pending++;

            try
            {
                bind.Invoke(null, [spec.RuneType, spec.CharacterType]);
                MarkBound(name);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                failures.Add((name, ex.InnerException));
            }
            catch (Exception ex)
            {
                failures.Add((name, ex));
            }
        }

        if (pending == 0) return;

        var total = BoundCount;
        var count = HextechRuneCatalog.All.Count;

        if (total < count)
        {
            HextechCompat.Error(
                $"SetPlayerRuneCharacter: ok={total}/{count} —— 未绑定的符文不会进角色专属档。" +
                "若异常提到类型不在对方枚举里，说明 CharacterType 需改为对方认识的表示。");
            foreach (var (name, error) in failures)
                HextechCompat.Error($"SetPlayerRuneCharacter failed: {name}({error.GetType().Name}: {error.Message})");
        }
        else
        {
            HextechCompat.Info($"SetPlayerRuneCharacter: ok={total}/{count} [{string.Join(", ", BoundNames())}]");
        }
    }

    /// <summary>
    ///     组合 <c>PlayerRuneFlags</c>：R4 起在 <c>ApiVersion &gt;= 2</c> 时追加 <c>GuaranteedFirstPick</c>。
    ///     <para>已有其他标志时按对方约定用逗号拼接；本组当前恒为 <c>null</c>，走不到拼接分支。</para>
    /// </summary>
    private static string? ComposeFlags(string? flags, bool guaranteedFirstPick)
    {
        if (!guaranteedFirstPick) return flags;
        return string.IsNullOrEmpty(flags) ? "GuaranteedFirstPick" : $"{flags},GuaranteedFirstPick";
    }

    /// <summary>
    ///     三选一界面上的「来源标签」；文案取 <c>relic_collection</c> 表的 <c>HEXTECH_POOL.&lt;poolKey&gt;</c>。
    /// </summary>
    private static void RegisterPoolLabels()
    {
        var setLabel = HextechCompat.ResolveInteropMethod("SetPlayerRunePoolLabel", SetPlayerRunePoolLabelParameters);
        if (setLabel is null)
        {
            HextechCompat.Info("'SetPlayerRunePoolLabel' 不可用 ⇒ 来源标签保持默认。");
            return;
        }

        var count = 0;

        foreach (var spec in HextechRuneCatalog.All)
        {
            try
            {
                setLabel.Invoke(null, [spec.RuneType, HextechRuneCatalog.PoolLabelKey]);
                count++;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                HextechCompat.Warn(
                    $"SetPlayerRunePoolLabel('{spec.RuneType.Name}') failed: {ex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                HextechCompat.Warn($"SetPlayerRunePoolLabel('{spec.RuneType.Name}') failed: {ex.Message}");
            }
        }

        HextechCompat.Info($"SetPlayerRunePoolLabel: ok={count}/{HextechRuneCatalog.All.Count}");
    }

    /// <summary>配置菜单里我方这组内容的小标题；文案键是 <c>relic_collection</c> 表的完整键本身。</summary>
    private static void RegisterConfigSection()
    {
        var registerTitle = HextechCompat.ResolveInteropMethod(
            "RegisterConfigSectionTitle",
            RegisterConfigSectionTitleParameters);

        if (registerTitle is null)
        {
            HextechCompat.Info("'RegisterConfigSectionTitle' 不可用 ⇒ 配置菜单分组标题保持默认。");
            return;
        }

        try
        {
            registerTitle.Invoke(null, [HextechRuneCatalog.AssetModId, HextechRuneCatalog.ConfigSectionTitleKey]);
            HextechCompat.Info($"RegisterConfigSectionTitle: ok ('{HextechRuneCatalog.ConfigSectionTitleKey}')");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            HextechCompat.Warn($"RegisterConfigSectionTitle failed: {ex.InnerException.Message}");
        }
        catch (Exception ex)
        {
            HextechCompat.Warn($"RegisterConfigSectionTitle failed: {ex.Message}");
        }
    }

    /// <summary>测试/排障用：把已解析到的软依赖成员列出来。</summary>
    public static IReadOnlyList<string> DescribeCapabilities()
    {
        var result = new List<string> { $"ApiVersion={HextechCompat.ApiVersion}" };

        void Probe(string name, Type[] parameters)
        {
            var method = HextechCompat.ResolveInteropMethod(name, parameters);
            result.Add($"{name}: {(method is null ? "unavailable" : "ok")}");
        }

        Probe("RegisterPlayerRune", RegisterPlayerRuneParameters);
        Probe("SetPlayerRuneCharacter", SetPlayerRuneCharacterParameters);
        Probe("SetPlayerRunePoolLabel", SetPlayerRunePoolLabelParameters);
        Probe("RegisterConfigSectionTitle", RegisterConfigSectionTitleParameters);
        result.Add($"AllRegistered={AllRegistered}");
        result.Add($"AllBound={AllBound}");

        return result;
    }

    private static bool IsRegistered(string runeTypeName)
    {
        lock (Gate)
            return RegisteredRunes.Contains(runeTypeName);
    }

    private static void MarkRegistered(string runeTypeName)
    {
        lock (Gate)
            RegisteredRunes.Add(runeTypeName);
    }

    private static bool IsBound(string runeTypeName)
    {
        lock (Gate)
            return BoundRunes.Contains(runeTypeName);
    }

    private static void MarkBound(string runeTypeName)
    {
        lock (Gate)
            BoundRunes.Add(runeTypeName);
    }

    private static int RegisteredCount
    {
        get
        {
            lock (Gate)
                return RegisteredRunes.Count;
        }
    }

    private static IReadOnlyList<string> RegisteredNames()
    {
        lock (Gate)
            return [.. RegisteredRunes];
    }

    private static int BoundCount
    {
        get
        {
            lock (Gate)
                return BoundRunes.Count;
        }
    }

    private static IReadOnlyList<string> BoundNames()
    {
        lock (Gate)
            return [.. BoundRunes];
    }
}
