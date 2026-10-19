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
///     无限制魔法格斗（传火·燎原库）：本回合你的<b>攻击牌</b>造成的伤害 +<see cref="Bonus" />。
///     只加攻击牌（能力牌/技能牌的伤害不吃），回合结束时移除。
/// </summary>
[RegisterPower]
public sealed class UnrestrictedMagicFightPower : ManosabaPowerTemplate
{
    /// <summary>攻击牌伤害加成（同时也是图标上显示的层数）。</summary>
    public const decimal Bonus = 4m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer != Owner)
            return 0m;

        if (cardSource == null || cardSource.Type != CardType.Attack)
            return 0m;

        if (!props.IsPoweredAttack())
            return 0m;

        return Amount;
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
