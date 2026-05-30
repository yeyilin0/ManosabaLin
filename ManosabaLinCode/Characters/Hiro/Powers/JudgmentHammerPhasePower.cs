using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Monsters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Emalin.Components;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Entities.Players;

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
            .OfType<Creature>()
            .FirstOrDefault(c => c.GetPower<GuardTwoBossPhasePower>() != null);

        if (boss?.Monster is not GuardTwoBossMonster bossMonster) return;

        if (bossMonster.IsDoubleAttackMode) return;

        if (conditionMet)
        {
            var bonusAttack3 = new MoveState("ATTACK_3_MOVE", bossMonster.Attack3Move,
                new AbstractIntent[]
                {
                    new SingleAttackIntent(bossMonster.Turn3Damage),
                    new BuffIntent(),
                    new DefendIntent()
                });

            bonusAttack3.FollowUpState = new MoveState("CHECK_MOVES", bossMonster.CheckMovesMove, new DebuffIntent());

            bossMonster.SetMoveImmediate(bonusAttack3, true);
        }
    }

    public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (!Hook.ShouldFlush(player.Creature.CombatState, player)) return;
        await PowerCmd.Remove<JudgmentHammerPhasePower>(Owner);
    }
}