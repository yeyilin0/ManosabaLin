using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 雀跃能力（快乐+惊讶）：获得等于能量上限的能量但减少下回合能量。
/// </summary>
[RegisterPower]
public sealed class EmotionElationPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(
        CombatSide side,
        System.Collections.Generic.IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side) return;

        // 获得等于能量上限的能量，减少下回合能量
        var maxEnergy = Owner.Player.MaxEnergy;
        if (maxEnergy > 0)
        {
            await PlayerCmd.GainEnergy(maxEnergy, Owner.Player);
            await PowerCmd.Apply<LoseEnergyPower>(
                new ThrowingPlayerChoiceContext(),
                Owner, (int)maxEnergy,
                Owner, null, false);
        }
    }
}
