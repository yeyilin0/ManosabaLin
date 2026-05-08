using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class KkmPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        Flash();

        var enemies = Owner.CombatState.Creatures
            .Where(e => e.IsEnemy && e.IsAlive && e.Monster != null)
            .ToList();

        foreach (var enemy in enemies)
        {
            foreach (var intent in enemy.Monster.NextMove.Intents)
            {
                switch (intent.IntentType)
                {
                    case IntentType.Attack:
                    case IntentType.DeathBlow:
                        Owner.GainBlockInternal(5m * Amount);
                        break;

                    case IntentType.Defend:
                    case IntentType.Buff:
                        await PowerCmd.Apply<StrengthPower>(
                            choiceContext, Owner, 1m * Amount,
                            Owner, null, false);
                        break;

                    case IntentType.Debuff:
                        await PowerCmd.Apply<DexterityPower>(
                            choiceContext, Owner, 1m * Amount,
                            Owner, null, false);
                        break;

                    default:
                        var validTargets = Owner.CombatState.Creatures
                            .Where(c => c.IsEnemy && c.IsAlive).ToList();
                        if (validTargets.Any())
                        {
                            var target = validTargets[new System.Random().Next(validTargets.Count)];
                            await PowerCmd.Apply<WeakPower>(
                                choiceContext, target, 1m * Amount,
                                Owner, null, false);
                        }
                        break;
                }
            }
        }

        // 触发完移除一层
        await PowerCmd.Decrement(this);
    }
}