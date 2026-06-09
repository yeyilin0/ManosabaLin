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
/// 受到冷漠能力：被施加者攻击时，攻击者获得1层情绪，此能力层数减一。
/// </summary>
[RegisterPower]
public sealed class CosmicApathyDebuffPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 施加此能力的生物（即玩家角色）。
    /// </summary>
    public Creature? Applier { get; set; }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        // 此能力的Owner是被施加的敌人
        if (target != Owner) return;
        if (result.TotalDamage <= 0) return;
        if (dealer == null) return;

        // 只有施加者攻击时才触发
        if (dealer != Applier) return;

        Flash();

        // 施加者获得1层情绪
        await PowerCmd.Apply<EmotionPower>(
            choiceContext, dealer, 1, dealer, null, false);

        // 此能力层数减一
        Amount--;
        if (Amount <= 0)
            RemoveInternal();
    }
}
