using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class KkmPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;

        Flash();

        var firstEnemy = combatState.Creatures
            .Where(e => e.IsEnemy && e.IsAlive && e.Monster != null)
            .FirstOrDefault();

        if (firstEnemy == null) return;

        foreach (var intent in firstEnemy.Monster.NextMove.Intents)
        {
            switch (intent.IntentType)
            {
                case IntentType.Attack:
                case IntentType.DeathBlow:
                    Owner.GainBlockInternal(5m);
                    break;

                case IntentType.Defend:
                case IntentType.Buff:
                    await PowerCmd.Apply<StrengthPower>(
                        new ThrowingPlayerChoiceContext(), Owner, 1m,
                        Owner, null, false);
                    break;

                case IntentType.Debuff:
                    await PowerCmd.Apply<DexterityPower>(
                        new ThrowingPlayerChoiceContext(), Owner, 1m,
                        Owner, null, false);
                    break;

                default:
                    foreach (var target in combatState.Creatures.Where(c => c.IsEnemy && c.IsAlive))
                    {
                        await PowerCmd.Apply<WeakPower>(
                            new ThrowingPlayerChoiceContext(), target, 1m,
                            Owner, null, false);
                    }
                    break;
            }
        }

        await PowerCmd.Decrement(this);
    }
}
