using ManosabaLin.Characters.Hiro.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Hooks;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class JudgmentHammerPhasePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        var combatState = Owner.CombatState;
        if (combatState == null) return;

        var players = combatState.Players.ToList();
        var playerCount = players.Count;
        var maxCount = 0;

        foreach (var p in players)
        {
            var count = PileType.Hand.GetPile(p).Cards
                .Count(c => c.HasComponent<BlackHandComponent>());
            if (count > maxCount)
                maxCount = count;
        }

        var conditionMet = maxCount > 3 * playerCount;

        var boss = combatState.Enemies
            .FirstOrDefault(c => c.GetPower<GuardTwoBossLastStandPower>() != null);

        if (boss?.Monster is not GuardTwoBossMonster bossMonster) return;

        if (bossMonster.IsDoubleAttackMode) return;

        if (conditionMet)
            bossMonster.SetMoveImmediate((MoveState)bossMonster.MoveStateMachine!.States["ATTACK_3_MOVE"]);
    }

    public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (!Hook.ShouldFlush(player.Creature.CombatState!, player)) return;
        await PowerCmd.Remove<JudgmentHammerPhasePower>(Owner);
    }
}
