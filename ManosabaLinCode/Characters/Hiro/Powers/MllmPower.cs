using ManosabaLin.Characters.Common;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public  class MllmPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        // 恢复 30 生命
        await CreatureCmd.Heal(Owner, 30);

        // 消耗一层（少于2层直接移除）
        if (Amount <= 1)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(choiceContext, this, -1, Owner, null, false);
    }
}