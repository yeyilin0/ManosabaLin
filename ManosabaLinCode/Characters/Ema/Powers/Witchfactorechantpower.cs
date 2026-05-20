using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Hiro.Powers;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class Witchfactorechantpower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 打出卡牌时，每层使随机敌人获得3层魔女因子
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner) return;
        if (Amount <= 0) return;

        var enemies = Owner.CombatState.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0) return;

        var rng = Owner.Player.RunState.Rng.CombatTargets;
        var target = rng.NextItem(enemies);

        await PowerCmd.Apply<EmaWitchFactorPower>(
            choiceContext, target, 3m * Amount, Owner, cardPlay.Card, false);
    }
}
