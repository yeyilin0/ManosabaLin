using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 厌恶能力：受到敌人攻击时反弹等量伤害。
/// </summary>
[RegisterPower]
public sealed class EmotionDisgustPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        if (dealer == null || dealer.Side == Owner.Side) return;
        if (result.TotalDamage <= 0) return;

        await CreatureCmd.Damage(
            choiceContext, dealer, result.TotalDamage,
            ValueProp.Unpowered, Owner, null);
    }
}
