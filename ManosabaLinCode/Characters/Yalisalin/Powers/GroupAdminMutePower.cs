using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
///     群管理的禁言（传火·火势库，挂在敌人身上）：该敌人<b>下个回合无法获得增益</b>。
///     本回合先挂着不生效；敌人回合开始时激活，其回合结束时移除。
/// </summary>
[RegisterPower]
public sealed class GroupAdminMutePower : ManosabaPowerTemplate
{
    /// <summary>是否已激活（敌人的下回合开始后才挡增益）。</summary>
    private bool _active;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == Owner.Side)
            _active = true;

        return Task.CompletedTask;
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (!_active || target != Owner || amount <= 0m)
            return false;

        if (canonicalPower.Type != PowerType.Buff)
            return false;

        // 增益改成 0：PowerCmd.Apply 对 amount==0 直接 return，等于把增益挡掉。
        modifiedAmount = 0m;
        return true;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }
}
