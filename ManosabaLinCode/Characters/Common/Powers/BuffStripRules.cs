namespace ManosabaLin.Characters.Common.Powers;

/// <summary>
///     「移除目标当前身上全部增益」类效果（【能力失效】、【嫌疑】满层触发）共用的过滤规则。
/// </summary>
/// <remarks>
///     引擎里有一批结构性 Buff：它们不是给敌人的"好处"，而是维持怪物自身机制的标记。
///     惊吓幻影（Parafright）出生即带 <see cref="IllusionPower"/>（死亡时下回合满血复活），
///     IllusionPower 施加时又会补一个 <see cref="MinionPower"/>。剥掉这两个会同时炸掉两处：
///     <list type="bullet">
///         <item>
///             <description>
///                 没有 IllusionPower 时，任何一次受伤都会执行 <c>Parafright.GenerateAnimator</c>
///                 里注册的 Hit 触发器条件
///                 <c>() =&gt; !Creature.GetPower&lt;IllusionPower&gt;().IsReviving</c> ——
///                 两个分支都没有判空，直接空引用，把整个打牌动作以 AggregateException 炸掉。
///             </description>
///         </item>
///         <item>
///             <description>
///                 没有 MinionPower 时判定直接错乱：它把 <c>OwnerIsSecondaryEnemy</c> 覆写为
///                 true，而 <c>Creature.IsSecondaryEnemy</c> 就是
///                 <c>Powers.Any(p =&gt; p.OwnerIsSecondaryEnemy)</c>（MinionPower 是引擎里唯一的
///                 覆写者）。一旦剥掉，惊吓幻影就变成"主要敌人"，于是战斗结束判定
///                 <c>Enemies.Any(e =&gt; e.IsAlive &amp;&amp; e.IsPrimaryEnemy)</c> 只要它还活着就永远
///                 为真 —— 战斗结算卡死；Parafright 的 Dead 动画条件同样按 IsPrimaryEnemy 判定，
///                 一起跟着错。
///             </description>
///         </item>
///     </list>
///     【爆裂魔法】先上 debuff（含【能力失效】）再打伤害，同一次出牌内必现。
///     所以剥增益时必须原样保留这两个能力。
/// </remarks>
internal static class BuffStripRules
{
    /// <summary>
    ///     是否是引擎结构性能力（剥离会破坏怪物自身机制，必须保留）。
    /// </summary>
    public static bool IsStructural(PowerModel power) => power is IllusionPower or MinionPower;

    /// <summary>
    ///     取出目标当前可被剥离的增益。返回快照列表：调用方会在遍历中移除能力，
    ///     必须先在枚举外层物化，不能边枚举边删。
    /// </summary>
    public static List<PowerModel> StrippableBuffs(Creature creature) =>
        creature.Powers
            .Where(static p => p.Type == PowerType.Buff && !IsStructural(p))
            .ToList();
}
