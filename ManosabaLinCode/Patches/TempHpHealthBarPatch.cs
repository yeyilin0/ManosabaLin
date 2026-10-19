using System.Collections.Generic;
using HarmonyLib;
using ManosabaLin.Characters.Yalisalin.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ManosabaLin.Patches;

/// <summary>
///     血条上的【临时生命】（<see cref="TempHpPower" />）条：紧贴当前生命填充条的右端向外延伸，
///     颜色 <c>#ffcc44</c>（与【夺火】强化光、【王魂】同色的火金），宽度 ∝ 临时生命 / 最大生命。
///     <para>
///     实现要点：**复用原版血条填充的白底贴图**——原版血量本身就是拿 <c>SelfModulate</c>
///     染红/蓝/紫的（见 <c>NHealthBar.RefreshBlockUi</c> 的 <c>_redForegroundColor</c> 等），
///     说明填充贴图是白底的，染 <c>#ffcc44</c> 即得金条；九Patch 边缘一并照抄，形状与原版完全一致，
///     **不需要任何新贴图**。坐标口径照抄毒/末日 overlay 的 OffsetLeft/OffsetRight 算法。
///     </para>
///     <para>
///     刷新时机：原版只在格挡变化/死亡/复活/战斗状态变化时刷血条，**能力变化不刷**
///     （见 <c>NCreatureStateDisplay.SubscribeToCreatureEvents</c>）——所以这里自己订阅
///     <see cref="Creature.PowerApplied"/> / <see cref="Creature.PowerIncreased"/> /
///     <see cref="Creature.PowerDecreased"/>，再叠加 <c>NHealthBar.RefreshValues</c> 后处理
///     （生命/上限变化会移动填充条右端，金条要跟着挪）。
///     </para>
/// </summary>
[HarmonyPatch(typeof(NHealthBar))]
public static class TempHpHealthBarPatch
{
    private const string BarNodeName = "ManosabaLinTempHpBar";

    /// <summary>临时生命条颜色：火金（夺火强化光 / 王魂同色）。</summary>
    private static readonly Color TempHpColor = new("ffcc44");

    private static readonly Dictionary<NHealthBar, Control> Bars = new();

    private static readonly Dictionary<NHealthBar, Creature> BarCreatures = new();

    /// <summary>每条血条复用来的九Patch 左边缘宽（金条左端要避开它，免得压住血量条的圆头）。</summary>
    private static readonly Dictionary<NHealthBar, int> BarMargins = new();

    /// <summary>已订阅能力事件的生物（静态事件处理器，不捕获 bar，重复订阅只会重复触发）。</summary>
    private static readonly HashSet<Creature> Subscribed = new();

    [HarmonyPostfix]
    [HarmonyPatch("SetCreature")]
    public static void Postfix_SetCreature(NHealthBar __instance, Creature creature)
    {
        if (creature == null)
            return;

        BarCreatures[__instance] = creature;
        if (Subscribed.Add(creature))
        {
            creature.PowerApplied += OnPowerApplied;
            creature.PowerIncreased += OnPowerIncreased;
            creature.PowerDecreased += OnPowerDecreased;
        }

        Refresh(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch("RefreshValues")]
    public static void Postfix_RefreshValues(NHealthBar __instance)
    {
        Refresh(__instance);
    }

    private static void OnPowerApplied(PowerModel power) => RefreshCreature(power.Owner);

    private static void OnPowerIncreased(PowerModel power, int delta, bool silent) => RefreshCreature(power.Owner);

    private static void OnPowerDecreased(PowerModel power, bool silent) => RefreshCreature(power.Owner);

    private static void RefreshCreature(Creature? creature)
    {
        if (creature == null)
            return;

        // 先快照再刷：Refresh 内部会修剪失效条目，不能边遍历边改字典。
        var bars = new List<NHealthBar>();
        foreach (var kv in BarCreatures)
        {
            if (kv.Value == creature)
                bars.Add(kv.Key);
        }

        foreach (var bar in bars)
            Refresh(bar);
    }

    private static void Refresh(NHealthBar bar)
    {
        Prune();

        if (bar is not { } validBar || !GodotObject.IsInstanceValid(validBar))
            return;
        if (!BarCreatures.TryGetValue(validBar, out var creature) || creature == null)
            return;

        var tempHp = creature.GetPowerAmount<TempHpPower>();
        if (tempHp <= 0)
        {
            Hide(validBar);
            return;
        }

        var node = EnsureBar(validBar);
        var hpFg = validBar.GetNodeOrNull<Control>("%HpForeground");
        var container = validBar.GetNodeOrNull<Control>("%HpForegroundContainer");
        if (node == null || hpFg == null || container == null)
            return;

        // 满宽参照：与引擎 MaxFgWidth 同一口径（HpForegroundContainer 的宽度）。
        var maxFg = container.Size.X;
        if (maxFg <= 0f)
        {
            node.Visible = false;
            return;
        }

        // 当前生命填充条的右端：引擎 OffsetRight = 填充宽 − 满宽 ⇒ 填充宽 = 满宽 + OffsetRight。
        var hpWidth = Mathf.Clamp(maxFg + hpFg.OffsetRight, 0f, maxFg);
        var tempWidth = creature.MaxHp > 0
            ? Mathf.Clamp((float)tempHp / (float)creature.MaxHp * maxFg, 0f, maxFg - hpWidth)
            : 0f;
        if (tempWidth <= 0f)
        {
            node.Visible = false;
            return;
        }

        var margin = BarMargins.TryGetValue(validBar, out var m) ? m : 0;
        node.Visible = true;
        node.OffsetLeft = Mathf.Max(0f, hpWidth - margin);
        node.OffsetRight = hpWidth + tempWidth - maxFg;
    }

    /// <summary>按需创建金条节点（懒创建：等血条布局完成后第一次刷新时再建）。</summary>
    private static Control? EnsureBar(NHealthBar bar)
    {
        if (Bars.TryGetValue(bar, out var existing) && existing is { } node0 && GodotObject.IsInstanceValid(node0))
            return node0;
        if (existing != null)
        {
            Bars.Remove(bar);
            BarMargins.Remove(bar);
        }

        var container = bar.GetNodeOrNull<Control>("%HpForegroundContainer");
        var hpFg = bar.GetNodeOrNull("%HpForeground");
        if (container == null || hpFg == null)
            return null;

        // 复用原版血条填充的白底贴图 + 九Patch 边缘（染金即金条，无需新贴图）。
        Texture2D? texture;
        int marginLeft;
        int marginTop;
        int marginRight;
        int marginBottom;
        if (hpFg is NinePatchRect nine)
        {
            texture = nine.Texture;
            marginLeft = nine.PatchMarginLeft;
            marginTop = nine.PatchMarginTop;
            marginRight = nine.PatchMarginRight;
            marginBottom = nine.PatchMarginBottom;
        }
        else if (hpFg is TextureRect tex)
        {
            texture = tex.Texture;
            marginLeft = 0;
            marginTop = 0;
            marginRight = 0;
            marginBottom = 0;
        }
        else
        {
            return null;
        }

        if (texture == null)
            return null;

        var node = new NinePatchRect
        {
            Name = BarNodeName,
            Texture = texture,
            PatchMarginLeft = marginLeft,
            PatchMarginTop = marginTop,
            PatchMarginRight = marginRight,
            PatchMarginBottom = marginBottom,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
        };
        node.SelfModulate = TempHpColor;
        node.AnchorLeft = 0f;
        node.AnchorTop = 0f;
        node.AnchorRight = 1f;
        node.AnchorBottom = 1f;
        node.GrowHorizontal = Control.GrowDirection.Both;
        node.GrowVertical = Control.GrowDirection.Both;

        container.AddChild(node);
        Bars[bar] = node;
        BarMargins[bar] = marginLeft;
        return node;
    }

    private static void Hide(NHealthBar bar)
    {
        if (Bars.TryGetValue(bar, out var node) && node is { } n && GodotObject.IsInstanceValid(n))
            n.Visible = false;
    }

    /// <summary>修剪已释放的血条条目（战斗结束节点销毁后，字典里会留下失效引用）。</summary>
    private static void Prune()
    {
        if (Bars.Count == 0)
            return;

        List<NHealthBar>? dead = null;
        foreach (var kv in Bars)
        {
            if (kv.Key is not { } bar || !GodotObject.IsInstanceValid(bar) ||
                kv.Value is not { } node || !GodotObject.IsInstanceValid(node))
            {
                (dead ??= new List<NHealthBar>()).Add(kv.Key);
            }
        }

        if (dead == null)
            return;

        foreach (var bar in dead)
        {
            Bars.Remove(bar);
            BarMargins.Remove(bar);
            BarCreatures.Remove(bar);
        }
    }
}
