using ManosabaLin.Characters.Common.LinRelics;
using ManosabaLin.Characters.Ananlin;
using ManosabaLin.Characters.Hiro;
using ManosabaLin.Characters.Sherrylin;
using ManosabaLin.Settings;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Compat.Hextech;

/// <summary>
///     一个「我方符文」在海克斯侧的全部元数据。
///     <para>
///         字段<b>就是</b> <c>RegisterPlayerRune</c> 的入参（软依赖签名收的是<b>枚举名字符串</b>，
///         所以这里不需要引用对方的任何类型）。规则全记在我们自己这边，注册时一次交出去。
///     </para>
/// </summary>
/// <param name="RuneType">我方遗物类型；必须是具体、非泛型 <c>RelicModel</c> 子类且 <c>Rarity == Starter</c>。</param>
/// <param name="CharacterType">
///     该符文专属的<b>我方角色</b>类型（<c>CharacterModel</c> 子类）。
///     <para>
///         R4（海克斯 <c>ApiVersion &gt;= 2</c>，2026-10-13 作者回复）起经 <c>SetPlayerRuneCharacter</c>
///         登记给对方，让模组角色也吃到 150% 角色加权；<see cref="CharacterPool" /> 仍恒 <c>null</c>
///         （对方枚举里没有 mod 角色）。
///     </para>
/// </param>
/// <param name="Rarity">
///     海克斯品级：<c>Silver</c> / <c>Gold</c> / <c>Prismatic</c>（枚举名，<b>不接受数字</b>）。
///     ⚠️ 这与我方 <c>RelicRarity.Starter</c> 是<b>两个维度</b>：前者是海克斯自己的三档品级，后者是原版稀有度。
/// </param>
/// <param name="Flags">
///     逗号分隔的 <c>PlayerRuneFlags</c>；本组全部为 <c>null</c>。
///     ⚠️ 注册桥在 <c>ApiVersion &gt;= 2</c> 时会追加 <c>GuaranteedFirstPick</c>（每幕第一次三选一的保底槽），
///     见 <c>HextechRuneRegistrar.RegisterRunes</c>；旧版本传这个标志会抛 <c>ArgumentException</c>。
/// </param>
/// <param name="CharacterPool">原版 5 角色池；通用 = <c>null</c>（模组角色的专属标记走 <see cref="CharacterType" />）。</param>
/// <param name="CharacterOrder">角色池内排序；<see cref="CharacterPool" /> 为 null 时无意义。</param>
/// <param name="TagKey">海克斯标签；<b>决定三选一加权</b>，自造可让我们只在自己人之间互相加权。</param>
/// <param name="AssetModId">配置菜单「来源」显示用。</param>
/// <param name="Availability">可用性过滤；见 <see cref="HextechRuneCatalog" /> 的确定性要求。</param>
/// <param name="Note">仅供人读的说明，不传给对方。</param>
internal sealed record HextechRuneSpec(
    Type RuneType,
    Type CharacterType,
    string Rarity,
    string? Flags,
    string? CharacterPool,
    int CharacterOrder,
    string TagKey,
    string AssetModId,
    Func<Player, bool>? Availability,
    string Note);

/// <summary>
///     我方符文 × 海克斯的元数据表（<b>纯数据，无副作用</b>）。
/// </summary>
internal static class HextechRuneCatalog
{
    /// <summary>自造海克斯标签 —— 让我们的符文只在自己人之间互相加权（默认 <c>COMPREHENSIVE</c> 会和对方「综合」挤一桶）。</summary>
    public const string TagKey = "MANOSABA_LIN";

    /// <summary>
    ///     我方全部符文的**海克斯品级**（**用户 2026-09-29 裁定：一律 Prismatic**）。
    ///     <para>
    ///         ⚠️ 这与我方遗物自己的 <c>RelicRarity.Starter</c> 是**两个维度**，不要混为一谈：
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             原版 <c>RelicRarity</c> 枚举里**没有 Prismatic**（只有 <c>None / Starter / Common / Uncommon /
    ///             Rare / Shop / Event / Ancient</c>）⇒ 「棱彩」在原版稀有度这一侧**根本不存在**；
    ///         </item>
    ///         <item>
    ///             <c>Starter</c> 是海克斯《符文类的要求》里写死的（保证原版别的抽取路径抽不到，
    ///             掉率完全交给海克斯管理）⇒ **不能**改成别的；
    ///         </item>
    ///         <item>
    ///             所以用户说的「棱彩品质」落下来就是**这里这个字符串**，且所有条目一致。
    ///         </item>
    ///     </list>
    /// </summary>
    public const string RuneRarity = "Prismatic";

    /// <summary>配置菜单「来源」显示用。</summary>
    public const string AssetModId = MainFile.ModId;

    /// <summary>来源标签池键；文案取 <c>relic_collection</c> 表的 <c>HEXTECH_POOL.MANOSABA_LIN</c>。</summary>
    public const string PoolLabelKey = "MANOSABA_LIN";

    /// <summary>配置菜单分组小标题键；<b>这就是 <c>relic_collection</c> 表里的完整键本身</b>。</summary>
    public const string ConfigSectionTitleKey = "MANOSABA_LIN_HEXTECH_SECTION";

    /// <summary>
    ///     全部我方符文。
    ///     <para>
    ///         ⚠️ <b>可用性过滤必须是确定性纯函数（除全局开关外）</b>：联机两端都会执行且必须得出相同结果，
    ///         只允许读<b>同步状态</b>（<c>player.Character</c> / 遗物 / 牌组 / <c>RunState</c>），
    ///         以及本模组的<b>全局</b>设置（<c>HextechLinkageSettingsService.IsEnabled</c>，默认开、两端默认一致）。
    ///         不读本地 UI 或随机数。
    ///     </para>
///     <para>
///         我们用 <see cref="HextechRuneSpec.Availability" /> 而不是 <c>characterPool</c> 来按角色收窄，
///         因为对方枚举里<b>只有原版 5 个角色</b>，没有本 mod 的角色。
///     </para>
///     <para>
///         ⚠️ R4（2026-10-13）起对方新增 <c>SetPlayerRuneCharacter</c>：模组角色的 150% 角色加权
///         改由 <see cref="HextechRuneSpec.CharacterType" /> 登记（<c>characterPool</c> 仍恒 <c>null</c>）。
///     </para>
    ///     <para>
    ///         门控语义 = <b>角色是安安/希罗/雪莉/艾玛，且全局联动开关未关</b>。用户的「专属联动遗物」门槛
    ///         等价于角色门槛本身（像海克斯原版角色专属遗物那样，不硬查某个具体遗物），故只按角色判断 + 设置开关。
    ///     </para>
    /// </summary>
    public static IReadOnlyList<HextechRuneSpec> All { get; } =
    [
        // 1. 用户裁定：仅雪莉琳（原本我按「不依赖角色专属机制」定成通用，被否）。
        new HextechRuneSpec(
            typeof(HextechDeckCleanser),
            typeof(Sherrylin),
            RuneRarity,
            null,
            null,
            0,
            TagKey,
            AssetModId,
            static player => player.Character is Sherrylin && HextechLinkageSettingsService.IsEnabled,
            "雪莉琳专属：每回合可把四堆里任意张牌扔进弃牌堆"),

        // 2. 情绪球是雪莉琳专属机制 ⇒ 别的角色拿到是废牌。
        new HextechRuneSpec(
            typeof(HextechEmotionOverflow),
            typeof(Sherrylin),
            RuneRarity,
            null,
            null,
            0,
            TagKey,
            AssetModId,
            static player => player.Character is Sherrylin && HextechLinkageSettingsService.IsEnabled,
            "雪莉琳专属：情绪球被挤出时立刻激发"),

        // 3. 用户裁定：仅雪莉琳（【魔女化】虽 5 个角色都有，但本遗物绑定的是雪莉琳那套组件链）。
        new HextechRuneSpec(
            typeof(HextechWitchificationCore),
            typeof(Sherrylin),
            RuneRarity,
            null,
            null,
            0,
            TagKey,
            AssetModId,
            static player => player.Character is Sherrylin && HextechLinkageSettingsService.IsEnabled,
            "雪莉琳专属：【魔女化】组件被激发时放大层数，回合开始把超量层数转成计数"),

        // 4 / 5 / 6. 【轮回】【伪证】【正义】都只存在于希罗的卡池（别的角色 0 处引用）。
        new HextechRuneSpec(
            typeof(HextechTransmigrationEcho),
            typeof(Hiro),
            RuneRarity,
            null,
            null,
            0,
            TagKey,
            AssetModId,
            static player => player.Character is Hiro && HextechLinkageSettingsService.IsEnabled,
            "希罗专属：打出【轮回】改为自动打出抽牌堆任意 2 张【轮回】"),

        new HextechRuneSpec(
            typeof(HextechPerjuryPayment),
            typeof(Hiro),
            RuneRarity,
            null,
            null,
            0,
            TagKey,
            AssetModId,
            static player => player.Character is Hiro && HextechLinkageSettingsService.IsEnabled,
            "希罗专属：能量不足时用【伪证】顶替，【正义】按 1:4 转换"),

        new HextechRuneSpec(
            typeof(HextechJusticeRegen),
            typeof(Hiro),
            RuneRarity,
            null,
            null,
            0,
            TagKey,
            AssetModId,
            static player => player.Character is Hiro && HextechLinkageSettingsService.IsEnabled,
            "希罗专属：回合结束把【再生】补到【正义】层数，【正义】不再回血"),

        // 7 / 8 / 9. 安安专属三件套（2026-09-29 追加）。书页 / 安心 / 缄默·洗脑 都只存在于安安的卡池与组件链。
        new HextechRuneSpec(
            typeof(HextechColorlessPage),
            typeof(Ananlin),
            RuneRarity,
            null,
            null,
            0,
            TagKey,
            AssetModId,
            static player => player.Character is Ananlin && HextechLinkageSettingsService.IsEnabled,
            "安安专属：书页额外多出一个可免费打出一次的无色选项"),

        new HextechRuneSpec(
            typeof(HextechPeaceOfMindEcho),
            typeof(Ananlin),
            RuneRarity,
            null,
            null,
            0,
            TagKey,
            AssetModId,
            static player => player.Character is Ananlin && HextechLinkageSettingsService.IsEnabled,
            "安安专属：【安心】按层数追加效果（攻击→活力／技能→格挡／能力→换留白书页并补回3层）"),

        new HextechRuneSpec(
            typeof(HextechBrainwashResonance),
            typeof(Ananlin),
            RuneRarity,
            null,
            null,
            0,
            TagKey,
            AssetModId,
            static player => player.Character is Ananlin && HextechLinkageSettingsService.IsEnabled,
            "安安专属：洗脑共用【缄默】的可成长替换池，且每场战斗第一次洗脑不获得【洗脑反噬】"),

        // 10 / 11 / 12. 艾玛专属三件套（2026-10-01 追加）。魔女因子 / 审判附魔 / 亲近·疏远 都只存在于艾玛的机制链。
        new HextechRuneSpec(
            typeof(HextechWitchFactorErosion),
            typeof(Emalin),
            RuneRarity,
            null,
            null,
            0,
            TagKey,
            AssetModId,
            static player => player.Character is Emalin && HextechLinkageSettingsService.IsEnabled,
            "艾玛专属：敌方回合开始时，带【魔女因子】的敌人按自身层数受到等量伤害"),

        new HextechRuneSpec(
            typeof(HextechTrialEmbodiment),
            typeof(Emalin),
            RuneRarity,
            null,
            null,
            0,
            TagKey,
            AssetModId,
            static player => player.Character is Emalin && HextechLinkageSettingsService.IsEnabled,
            "艾玛专属：卡牌获得【审判】附魔时自动转换成同等效果的组件"),

        new HextechRuneSpec(
            typeof(HextechBondDrift),
            typeof(Emalin),
            RuneRarity,
            null,
            null,
            0,
            TagKey,
            AssetModId,
            static player => player.Character is Emalin && HextechLinkageSettingsService.IsEnabled,
            "艾玛专属：亲近/疏远变化让模型左右漂移；左移给队友多人卡，右移移除一张并给下一次减费")
    ];

    /// <summary>
    ///     校验目录自身是否满足海克斯的硬性约束（具体类型 / <c>Starter</c> / 品级名合法）。
    ///     返回问题描述列表；空表示通过。
    ///     <para>在 <c>MainFile.Initialize()</c> 时跑一次，把「配错了但编译得过」变成显式日志。</para>
    /// </summary>
    public static IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();

        foreach (var spec in All)
        {
            var name = spec.RuneType.Name;

            if (spec.RuneType.IsAbstract || spec.RuneType.ContainsGenericParameters)
                problems.Add($"{name}: 必须是具体、非泛型类型");

            if (!typeof(RelicModel).IsAssignableFrom(spec.RuneType))
                problems.Add($"{name}: 必须是 RelicModel 子类");

            if (spec.Rarity is not ("Silver" or "Gold" or "Prismatic"))
                problems.Add($"{name}: 品级 '{spec.Rarity}' 非法（只接受 Silver/Gold/Prismatic）");

            // CharacterType 经 SetPlayerRuneCharacter 交给对方（用户 2026-10-15 裁定保留该绑定，
            // 联动遗物进角色专属池 + 150% 加权靠它）；类型合法性仍自检一道，避免漏配。
            if (spec.CharacterType.IsAbstract || spec.CharacterType.ContainsGenericParameters)
                problems.Add($"{name}: CharacterType 必须是具体、非泛型类型");

            if (!typeof(CharacterModel).IsAssignableFrom(spec.CharacterType))
                problems.Add($"{name}: CharacterType 必须是 CharacterModel 子类");
        }

        return problems;
    }
}
