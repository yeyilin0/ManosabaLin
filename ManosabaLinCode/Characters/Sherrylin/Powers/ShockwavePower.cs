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

[RegisterPower]
public sealed class ShockwavePower : ManosabaPowerTemplate, IHealthBarForecastSource
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // ========== 回合开始时失去血�?==========
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
            cardSource: null,
            cardPlay: null);
    }

    // ========== 被攻击时额外受到伤害 ==========
    public override async Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        Decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (!props.IsPoweredAttack()) return;

        var extraDamage = (int)Math.Ceiling(Amount / 2m);
        if (extraDamage <= 0) return;

        Flash();
        await CreatureCmd.Damage(choiceContext, Owner, extraDamage, ValueProp.Move, null, null);
    }

    // ========== 血条预�?==========
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
