using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class GuiltyChainPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private int _triggerCount;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner) return;
        if (cardPlay.Target == null || !cardPlay.Target.IsEnemy) return;
        if (_triggerCount >= 2) return;

        var targetSuspect = cardPlay.Target.GetPower<SuspectPower>();
        if (targetSuspect == null || targetSuspect.Amount <= 0) return;

        var otherEnemies = Owner.CombatState.Creatures
            .Where(c => c.IsAlive && c.IsEnemy && c != cardPlay.Target)
            .ToList();

        if (otherEnemies.Count == 0) return;

        Flash();
        _triggerCount++;

        var randomTarget = Owner.Player.RunState.Rng.Shuffle.NextItem(otherEnemies);
        await PowerCmd.Apply<SuspectPower>(
            choiceContext, randomTarget, 1,
            Owner, cardPlay.Card, false
        );
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner.Player) return;
        _triggerCount = 0;
    }
}