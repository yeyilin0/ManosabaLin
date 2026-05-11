using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class EmaAndHiroPhasePower : ManosabaPowerTemplate
{
    public const string SecondPhaseMoveId = "DUAL_ASSAULT_MOVE";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override bool ShouldReceiveCombatHooks => true;

    private bool _hasTransitioned;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (_hasTransitioned) return;
        if (Owner.CurrentHp > Owner.MaxHp / 2) return;

        var monster = Owner.Monster;
        if (monster?.MoveStateMachine is not { } moveStateMachine) return;
        if (!moveStateMachine.States.TryGetValue(SecondPhaseMoveId, out var state)) return;
        if (state is not MoveState secondPhaseMove) return;

        _hasTransitioned = true;
        Flash();

        await CreatureCmd.TriggerAnim(Owner, "Stun", 0.6f);

        var withAmount = Owner.GetPowerAmount<WithPower>();
        var shieldAmount = withAmount * 2;
        if (shieldAmount > 0)
            await CreatureCmd.GainBlock(Owner, shieldAmount, ValueProp.Move, null);

        monster.SetMoveImmediate(secondPhaseMove);
    }
}
