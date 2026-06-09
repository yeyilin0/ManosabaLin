using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 恼惧能力（愤怒+恐惧）：无法打出攻击牌，回合结束给予随机队友13伤害并使其获得等量护盾。
/// </summary>
[RegisterPower]
public sealed class EmotionIrritatedFearPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;

        // 给予自身13伤害，然后获得等量护盾
        await CreatureCmd.Damage(choiceContext, Owner, 13m, ValueProp.Unpowered, Owner, null);
        await CreatureCmd.GainBlock(Owner, 13m, ValueProp.Unpowered, null);
    }
}
