// GuardThreeCombatSingleton.cs
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

        // 1. 立即打断当前意图
        if (monster.MoveStateMachine?.States.TryGetValue("PHASE2_ATTACK", out var move) == true &&
            move is MoveState moveState)
        {
            monster.SetMoveImmediate(moveState, forceTransition: true);
        }

        // 2. 回满血
        await CreatureCmd.SetCurrentHp(creature, creature.MaxHp);

        // 3. 添加新能力，确认成功后再移除旧能力
        await PowerCmd.Apply<ThirteenWaterIntelPower>(
            new ThrowingPlayerChoiceContext(), creature, 1, creature, null);

        if (creature.GetPower<ThirteenWaterIntelPower>() != null)
            await PowerCmd.Remove(justice);

        // 4. 播放转阶段动画（不阻塞意图切换）
        await monster.EnterPhaseTwo();
    }
}
