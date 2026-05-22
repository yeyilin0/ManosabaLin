using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class OriginalsinjusticePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || Owner.IsDead) return;

        Flash();

        var perjury = Owner.GetPower<PerjuryPower>();
        var suspect = Owner.GetPower<SuspectPower>();

        if (perjury != null && perjury.Amount > 0)
        {
            await PowerCmd.Apply<JusticePower>(choiceContext, Owner, 1, Owner, null, false);
            await PowerCmd.ModifyAmount(choiceContext, perjury, -1, Owner, null, false);
        }

        if (suspect != null && suspect.Amount > 0)
        {
            await PowerCmd.Apply<WithPower>(choiceContext, Owner, 20, Owner, null, false);
            await PowerCmd.ModifyAmount(choiceContext, suspect, -1, Owner, null, false);
        }
    }
}