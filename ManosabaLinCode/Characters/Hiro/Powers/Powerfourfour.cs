using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class Powerfourfour : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;
        if (Owner.IsDead) return;

        Flash();

        await PowerCmd.Apply<WithPower>(
            choiceContext, Owner, 40,
            Owner, null, false
        );
    }
}