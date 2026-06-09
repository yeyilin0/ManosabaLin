using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
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
/// 恐惧能力：回合结束获得当前一半格挡。
/// </summary>
[RegisterPower]
public sealed class EmotionFearPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;

        var halfBlock = Math.Floor(Owner.Block / 2m);
        if (halfBlock > 0)
            await CreatureCmd.GainBlock(Owner, halfBlock, ValueProp.Unpowered, null);
    }
}
