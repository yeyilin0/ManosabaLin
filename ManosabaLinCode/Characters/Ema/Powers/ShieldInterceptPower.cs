using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class ShieldInterceptPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private decimal _totalDamageTaken;
    private readonly List<Creature> _coveredCreatures = new();

    public void AddCoveredCreature(Creature c)
    {
        if (!_coveredCreatures.Contains(c))
            _coveredCreatures.Add(c);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target == Owner && result.TotalDamage > 0)
            _totalDamageTaken += result.TotalDamage;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        if (_totalDamageTaken <= 0) return;

        Flash();

        // 给掩护列表中的队友加格挡
        foreach (var covered in _coveredCreatures)
        {
            if (covered is { IsAlive: true })
                await CreatureCmd.GainBlock(covered, _totalDamageTaken, ValueProp.Move, null);
        }

        // 羁绊偏亲密时自己也获得格挡
        var bond = Owner.GetPower<BondPower>();
        if (bond != null && bond.Affinity > bond.Estrangement)
        {
            await CreatureCmd.GainBlock(Owner, _totalDamageTaken, ValueProp.Move, null);
        }

        _totalDamageTaken = 0;
        await PowerCmd.Remove(this);
    }
}
