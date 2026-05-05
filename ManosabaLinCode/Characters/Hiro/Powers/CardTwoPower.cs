using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class CardTwoPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;
        if (Owner.IsDead) return;
        if (Amount <= 0) return;

        Flash();

        // 第一步：获取当前正义能力层数
        var justicePower = Owner.GetPower<JusticePower>();
        var justiceAmount = justicePower?.Amount ?? 0;

        // 第二步：根据正义层数回复血量
        if (justiceAmount > 0) await CreatureCmd.Heal(Owner, justiceAmount);
    }
}