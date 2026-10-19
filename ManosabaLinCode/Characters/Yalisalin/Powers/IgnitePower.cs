using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
/// 点火（亚里沙的专属魔法卡能力）：
/// 回合开始或回合结束时，按当前层数 N 随机攻击 N 次：
/// 每次随机选择全场目标（敌我均可）。
/// 命中敌方：造成等于层数一半（向下取整，至少 1）的伤害；所有友方获得 1 点格挡；若该敌方没有易伤，给予 1 层易伤。
/// 命中友方：改为造成 1 点伤害，然后本能力 +2 层。
/// [color=#ff0000]回合结束[/color]时失去一半层数（向下取整；1 层时损失 0，刚打出的点火不会直接消失）。
/// <para>
/// ⭐ 衰减只挂在**真实的回合结束钩子**（<see cref="AfterSideTurnEnd" />）上，
///     **不放进** <see cref="TriggerIgnite" />：卡牌手动调用（如「紫藤亚里沙」）只触发攻击、不触发衰减
///     ——否则一打出紫藤亚里沙就当场掉一半层数。「自罚上瘾」的文案本就是「触发 1 次攻击」，同理只打不衰。
/// </para>
/// <para>
/// ⭐ 表现节流：一次点火最多**播放 10 次打击（受伤）动画**，第 11 次起改为「无动画快速结算」
/// （伤害/格挡/易伤/层数的**数值结果完全不变**，只是不再逐个播受击表现与闪光等待）。
/// 层数堆到几十层时逐个播动画会非常磨叽，故设此上限。
/// </para>
/// </summary>
[RegisterPower]
public sealed class IgnitePower : ManosabaPowerTemplate
{
    /// <summary>
    /// 一次点火里**播放打击（受伤）动画**的最大次数；超过该序号的命中走无动画快速结算。
    /// </summary>
    private const int MaxPlayedHits = 10;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    ///     点火的一次攻击命中<b>友方</b>（含玩家自己；<c>combatState.Allies</c> 包含自己）后触发。
    ///     参数为点火能力的持有者（<see cref="Player" />）。
    ///     「自罚上瘾」的【原罪】引擎靠它把「点火打到自己人」转成【余火】。
    /// </summary>
    public static event Action<PlayerChoiceContext, Player>? AllyHit;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature != Owner) return;
        await TriggerIgnite(choiceContext);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        await TriggerIgnite(choiceContext);

        // 本地化规范：「[color=#ff0000]回合结束[/color]时失去一半层数」。
        // 先打完再衰减（层数就是这一击的弹药），顺序与卡面文案一致；
        // ⚠️ 衰减只在这里（真实回合结束）生效，TriggerIgnite 里没有——手动触发不触发衰减。
        await DecayHalfStacks(choiceContext);
    }

    /// <summary>
    ///     回合结束衰减：失去一半层数（向下取整）。
    ///     1 层时损失 0 ⇒ 点火被打出的当回合不会因为衰减直接归零消失。
    /// </summary>
    private async Task DecayHalfStacks(PlayerChoiceContext choiceContext)
    {
        var loss = (int)Amount / 2;
        if (loss <= 0) return;

        await PowerCmd.ModifyAmount(choiceContext, this, -loss, Owner, null);
    }

    /// <summary>
    ///     立刻执行一次完整点火（按当前层数随机攻击 N 次）。回合开始/结束自动调用；
    ///     卡牌（如「紫藤亚里沙」「自罚上瘾」）也可以手动调用它来「触发一次点火能力的攻击」。
    ///     ⚠️ **只含攻击，不含「回合结束失去一半层数」的衰减**——衰减挂在
    ///     <see cref="AfterSideTurnEnd" /> 上，手动触发不触发衰减。
    /// </summary>
    internal async Task TriggerIgnite(PlayerChoiceContext choiceContext)
    {
        if (Owner?.CombatState is not { } combatState) return;
        if (Owner?.Player is not { } player) return;

        var hitCount = (int)Amount;
        if (hitCount <= 0) return;

        Flash();

        var rng = player.RunState.Rng.CombatTargets;

        for (var i = 0; i < hitCount; i++)
        {
            // ⭐ 前 MaxPlayedHits 次命中正常播放打击/受击表现；之后改为快速结算。
            //   animated=false 时：伤害带 ValueProp.SkipHurtAnim（不播受伤动画）、
            //   格挡用 fast:true（0.03s 而非 0.25s）、能力增减 silent:true（不播闪光）。
            //   ⚠️ 只关表现，**数值与结算顺序完全不变**。
            var animated = i < MaxPlayedHits;

            var allies = combatState.Allies.Where(c => c.IsAlive).ToArray();
            var enemies = combatState.Enemies.Where(c => c.IsAlive).ToArray();
            var candidates = allies.Concat(enemies).ToArray();
            if (candidates.Length == 0) break;

            var target = rng.NextItem(candidates);
            if (target == null) continue;

            var isEnemy = enemies.Contains(target);

            // 命中敌方：造成等于当前层数一半的伤害（向下取整，至少 1）
            // 命中友方：改为造成 1 点伤害
            // 能力来源伤害统一用 Unpowered（不受力量/魔女化影响，可被格挡）
            var damage = isEnemy ? Math.Max(1m, Math.Floor(Amount / 2m)) : 1m;
            var props = animated
                ? ValueProp.Unpowered
                : ValueProp.Unpowered | ValueProp.SkipHurtAnim;
            await CreatureCmd.Damage(
                choiceContext, target, damage,
                props,
                Owner, null, null);
            if (Owner == null) return;

            if (isEnemy)
            {
                // 所有友方获得 1 点格挡（卡面文案为「所有友方」，不是随机一名）
                foreach (var ally in allies)
                {
                    if (ally.IsAlive)
                        await GainFixedBlock(ally, animated);
                }

                // 被攻击的敌方没有易伤时，给予 1 层易伤
                if (target.GetPower<VulnerablePower>() == null)
                    await PowerCmd.Apply<VulnerablePower>(
                        choiceContext, target, 1m, Owner, null, silent: !animated);
            }
            else
            {
                // 命中友方：本能力 +2 层
                await PowerCmd.ModifyAmount(
                    choiceContext, this, 2m, Owner, null, silent: !animated);

                // 广播「点火命中友方」（卡牌/能力可订阅，例如【原罪】）
                AllyHit?.Invoke(choiceContext, player);
            }
        }
    }

    /// <summary>
    ///     点火命中敌方时给友方的 +1 格挡：**刻意绕过引擎全部格挡修改钩子**，
    ///     任何能力/遗物都不能改动它（【无法获得格挡】的 <c>ModifyBlockMultiplicative</c>、
    ///     一批改格挡的遗物/能力挂的 <c>ModifyBlockAdditive/Multiplicative</c>、
    ///     以及 <c>BeforeBlockGained/AfterBlockGained</c> 上的触发器，全部不经过）。
    ///     <para>
    ///     引擎 <see cref="CreatureCmd.GainBlock" /> 会依次过
    ///     <c>BeforeBlockGained → ModifyBlock(Additive/Multiplicative) → AfterBlockGained</c>，
    ///     所以这里不走它，只复用它的**非钩子部分**（战斗结束/死亡守卫、音效、VFX、战斗历史、等待），
    ///     数值直接走 <see cref="Creature.GainBlockInternal" />（public；Block 直接累加，
    ///     Block setter 仍发 <c>BlockChanged</c> ⇒ 血条格挡数字照常刷新）。
    ///     </para>
    /// </summary>
    private static async Task GainFixedBlock(Creature ally, bool animated)
    {
        if (CombatManager.Instance.IsOverOrEnding || ally.IsDead) return;

        SfxCmd.Play("event:/sfx/block_gain");
        VfxCmd.PlayOnCreatureCenter(ally, "vfx/vfx_block");
        ally.GainBlockInternal(1m);
        CombatManager.Instance.History.BlockGained(ally.CombatState, ally, 1, ValueProp.Move, null);

        // 与引擎 GainBlock 的 fast 参数同口径：animated 时 0.25s 保持打击节奏，快速结算时 0.03s。
        await Cmd.CustomScaledWait(animated ? 0.1f : 0f, animated ? 0.25f : 0.03f);
    }
}
