using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 守绪之盾能力：记录本回合受到的攻击次数，下回合开始时获得等量情绪层数。
/// </summary>
[RegisterPower]
public sealed class EmotionShieldPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private int _attacksTaken;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        if (result.TotalDamage <= 0) return;

        _attacksTaken++;
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side) return;
        if (_attacksTaken <= 0) return;

        Flash();

        await PowerCmd.Apply<EmotionPower>(
            new ThrowingPlayerChoiceContext(),
            Owner, _attacksTaken,
            Owner, null, false);

        _attacksTaken = 0;
        await PowerCmd.Remove(this);
    }
}
