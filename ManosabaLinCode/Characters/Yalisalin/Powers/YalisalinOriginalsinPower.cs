using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Yalisalin.Components;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Threading.Tasks;

using MegaCrit.Sts2.Core.Entities.Powers;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
///     原罪（亚里沙专属，由「自罚上瘾」消耗 100 层【魔女化】获得）：
///     每当你的【点火】攻击到友方 1 次，随机使你牌组里的 1 张牌获得【余火】。
///     <para>
///         「友方」= <c>combatState.Allies</c>（<b>包含你自己</b>），因此单机时等价于「点火打到自己」。
///     </para>
/// </summary>
[RegisterPower]
public sealed class YalisalinOriginalsinPower : ManosabaPowerTemplate
{
    private bool _hooked;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        Hook();
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        Unhook();
        return Task.CompletedTask;
    }

    private void Hook()
    {
        if (_hooked) return;
        IgnitePower.AllyHit += OnIgniteAllyHit;
        _hooked = true;
    }

    private void Unhook()
    {
        if (!_hooked) return;
        IgnitePower.AllyHit -= OnIgniteAllyHit;
        _hooked = false;
    }

    /// <summary>
    ///     点火的事件是静态的，多人时全场共用，所以必须按「发起者是不是我」过滤。
    /// </summary>
    private void OnIgniteAllyHit(PlayerChoiceContext choiceContext, Player igniter)
    {
        try
        {
            if (Owner?.Player is not { } me) return;
            if (!ReferenceEquals(igniter, me)) return;

            // 从「手 / 抽 / 弃」里随机挑一张还没有【余火】的牌（已自动排除选择锁定中的牌）
            var card = YalisalinFireComponentRules.RandomCardWithoutFireComponent(me);
            if (card == null) return;

            if (!YalisalinFireComponentRules.TryAddFireComponent(card)) return;

            Flash();
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }
    }
}
