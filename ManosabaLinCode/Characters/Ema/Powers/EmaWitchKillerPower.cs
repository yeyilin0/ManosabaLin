using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class EmaWitchKillerPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<EmaWitchFactorPower>(); }
    }

    // 回合开始：敌方+10，友方+1
    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != Owner.Side) return;

        Flash();

        foreach (var enemy in combatState.Enemies.Where(e => e is { IsAlive: true }))
            await PowerCmd.Apply<EmaWitchFactorPower>(
                new ThrowingPlayerChoiceContext(), enemy, 10m * Amount, Owner, null, false);

        foreach (var ally in combatState.Allies.Where(a => a is { IsAlive: true }))
            await PowerCmd.Apply<EmaWitchFactorPower>(
                new ThrowingPlayerChoiceContext(), ally, 1m * Amount, Owner, null, false);
    }

    // 友方受到伤害时：增加等于伤害四分之一的层数
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (result.TotalDamage <= 0) return;
        if (target.Side != Owner.Side) return;

        var stacks = (int)(result.TotalDamage / 4m);
        if (stacks <= 0) return;

        await PowerCmd.Apply<EmaWitchFactorPower>(
            choiceContext, target, stacks, Owner, null, false);
    }

    // 敌方受到伤害时：增加等于伤害的层数（任何友方造成伤害都触发）
    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (result.TotalDamage <= 0) return;
        if (target.Side == Owner.Side) return;
        if (dealer == null) return;
        if (dealer.Side != Owner.Side) return;

        await PowerCmd.Apply<EmaWitchFactorPower>(
            choiceContext, target, result.TotalDamage, Owner, null, false);
    }
}