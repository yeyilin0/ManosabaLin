using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 魔女化情绪：造成伤害时变为三倍，此伤害不致死。1回合后移除。
/// </summary>
[RegisterPower]
public sealed class WitchificationEmotionPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner) return 1m;

        // 三倍伤害
        return 3m;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Any(p => p == Owner)) return;
        await PowerCmd.Remove(this);
    }
}
