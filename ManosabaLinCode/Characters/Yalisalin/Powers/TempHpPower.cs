using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
///     临时生命（夺火·升级效果「格挡转化」的产物）：层数即临时生命值。
///     <para>
///     吸收伤害挂在 <see cref="ModifyHpLostAfterOsty" /> —— 格挡结算完之后、真实扣血前的最后一道闸
///     （伤害被 Osty 转走时作用在最终目标上，语义正确）；每点临时生命抵消 1 点伤害，扣完层数归零。
///     </para>
///     <para>
///     层数归零后图标自动隐藏（<see cref="IsVisibleInternal" />），再次获得时自动复亮；
///     联机安全：层数走 PowerState 同步（id+amount），且吸收发生在两端一致执行的伤害流程里。
///     血条上的 #ffcc44 临时生命条见 <see cref="Patches.TempHpHealthBarPatch" />。
///     </para>
///     <para>
///     ⚠️ 已知边界：上限下降（LoseMaxHp 走的 SetMaxHpInternal）不过 Modifier 钩子，临时生命不吸收那部分流失
///     （古明地恋mod 用 patch SetMaxHpInternal 处理，本项目暂不跟——那是罕见的 corner case）。
///     </para>
/// </summary>
[RegisterPower]
public sealed class TempHpPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>层数归零后隐藏图标（0 层没有展示价值；再获得时自动复亮）。</summary>
    protected override bool IsVisibleInternal => Amount > 0 && base.IsVisibleInternal;

    public override decimal ModifyHpLostAfterOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || amount <= 0m || Amount <= 0)
            return amount;

        // 只吸收整数点（伤害基本全是整数；万一有小数额，剩余部分照常扣真实生命）。
        var incoming = Math.Floor(amount);
        var absorb = Math.Min((decimal)Amount, incoming);
        if (absorb <= 0m)
            return amount;

        SetAmount(Amount - (int)absorb);
        return amount - absorb;
    }
}
