using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.HealthBars;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 冲击：回合开始时失去等于层数的血量。
/// 拥有者被攻击时，额外受到层数一半的伤害。
/// </summary>
[RegisterPower]
public sealed class ShockwavePower : ManosabaPowerTemplate, IHealthBarForecastSource
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // ========== 回合开始时失去血量 ==========
    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner))
            return;

        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner,
            Amount,
            ValueProp.Unblockable | ValueProp.Unpowered,
            dealer: null,
            cardSource: null);
    }

    // ========== 被攻击时额外受到伤害 ==========
    public override decimal ModifyHpLostBeforeOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // 只有拥有者是受伤目标时才生效
        if (target != Owner)
            return amount;

        // 只对攻击伤害生效
        if (!props.IsPoweredAttack())
            return amount;

        // 额外受到层数一半的伤害（向上取整）
        return amount + (int)Math.Ceiling(Amount / 2m);
    }

    // ========== 血条预测 ==========
    public IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(
        HealthBarForecastContext context)
    {
        if (context.Creature != Owner)
            return Enumerable.Empty<HealthBarForecastSegment>();

        return HealthBarForecasts.Single(
            context.Creature.GetPowerAmount<ShockwavePower>(),
            new Color(0.2f, 0.8f, 1.0f),
            HealthBarForecastGrowthDirection.FromLeft
        );
    }
}