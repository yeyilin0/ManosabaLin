using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class Poweroneone : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var justice = Owner.GetPower<JusticePower>();
        if (justice == null || justice.Amount < 1) return;

        // 消耗 1 层正义
        if (justice.Amount == 1)
            await PowerCmd.Remove(justice);
        else
            await PowerCmd.ModifyAmount(choiceContext, justice, -1, Owner, null, false);

        // 获得 1 点能量
        await PlayerCmd.GainEnergy(1, Owner.Player);
    }
}