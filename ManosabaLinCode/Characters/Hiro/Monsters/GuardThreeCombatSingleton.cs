using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Hiro.Powers;

namespace ManosabaLin.Characters.Hiro.Monsters;

[RegisterSingleton]
public sealed class GuardThreeCombatSingleton : SingletonModel
{
    public GuardThreeCombatSingleton()
    {
        ModHelper.SubscribeForCombatStateHooks(Id.Entry, CombatSubModels);
    }

    public override bool ShouldReceiveCombatHooks => true;

    private IEnumerable<AbstractModel> CombatSubModels(CombatState _)
    {
        return [this];
    }

    public override bool ShouldDie(Creature creature)
    {
        if (creature.Monster is not GuardThreeMonster) return true;
        return creature.GetPower<UncontrolledJusticePower>() == null;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature.Monster is not GuardThreeMonster monster) return;

        var justice = creature.GetPower<UncontrolledJusticePower>();
        if (justice == null) return;

        await PowerCmd.Remove(justice);
        await CreatureCmd.SetCurrentHp(creature, creature.MaxHp);

        await PowerCmd.Apply<ThirteenWaterIntelPower>(
            new ThrowingPlayerChoiceContext(), creature, 1, creature, null);

        GuardThreeWrongTextVfx.Spawn(creature, 1);

        if (monster.MoveStateMachine?.States.TryGetValue("PHASE2_ATTACK", out var move) == true &&
            move is MoveState moveState)
        {
            monster.SetMoveImmediate(moveState, forceTransition: true);
        }
    }
}
