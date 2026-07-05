// AmmPower.cs
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class AmmPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer?.GetPower<AmmPower>() == null) return;
        if (cardSource?.Owner?.Creature != Owner) return;
        if (cardSource?.Type != CardType.Attack) return;
        if (target == null || !target.IsEnemy || !target.IsAlive) return;
        if (result.UnblockedDamage <= 0) return;

        var with = Owner.GetPower<WithPower>();
        var withAmount = with?.Amount ?? 0;
        var threshold = (int)(withAmount / 4);

        if (target.CurrentHp <= threshold && target.CurrentHp > 0)
        {
            Flash();

            await CreatureCmd.Damage(
                choiceContext,
                target,
                99999m,
                ValueProp.Unblockable | ValueProp.Unpowered,
                Owner,
                cardSource,
                null);

            await PowerCmd.Apply<WithPower>(
                choiceContext, Owner, 10,
                Owner, null, false);
        }
    }
}
