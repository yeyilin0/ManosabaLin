using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
///     熨平的刘海（传火·火势库）：本回合下一次受到的伤害降低 <see cref="Reduction" /> 点。
///     只抵一次；回合结束时移除（本回合内没挨打就白折了）。
/// </summary>
[RegisterPower]
public sealed class IronedBangsPower : ManosabaPowerTemplate
{
    /// <summary>下一次受到的伤害降低量（同时也是图标上显示的层数）。</summary>
    public const decimal Reduction = 8m;

    private bool _consumed;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyHpLostBeforeOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (_consumed || target != Owner || amount <= 0m)
            return amount;

        _consumed = true;
        return Math.Max(0m, amount - Amount);
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
