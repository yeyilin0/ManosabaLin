using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class HiroMagicRevivePower : ManosabaPowerTemplate
{
    private const decimal ReviveHealAmount = 40m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("ReviveHeal", ReviveHealAmount)];

    // 当拥有此能力且层数>=1时，阻止死亡
    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner || Amount < 1) return true;
        return false;
    }

    // 死亡被阻止后：消耗1层，回复40血
    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();

        // 减少1层
        await PowerCmd.ModifyAmount(
            new ThrowingPlayerChoiceContext(),
            this,
            -1m,
            Owner,
            null,
            false);

        // 回复40血
        await CreatureCmd.Heal(creature, ReviveHealAmount);
    }
}
