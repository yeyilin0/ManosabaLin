using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class DeathRewindPower : ManosabaPowerTemplate
{
    public override PowerType Type => (PowerType)1;

    public override PowerStackType StackType => (PowerStackType)2;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new("HealPercent", 0m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<WithPower>()];

    public override Task AfterApplied(Creature applier, CardModel cardSource)
    {
        SyncHealPercent();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, // ★ 新增
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is WithPower && power.Owner == Owner)
            SyncHealPercent();
        return Task.CompletedTask;
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner || Amount < 1) return true;
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        _ = this;
        Flash();
        var healPercent = Math.Min(100m, creature.GetPowerAmount<WithPower>());
        await PowerCmd.Remove<DeathRewindPower>(creature);
        var num = Math.Max(1m, creature.MaxHp * (healPercent / 100m));
        await CreatureCmd.Heal(creature, num);
    }

    private void SyncHealPercent()
    {
        if (Owner != null)
            DynamicVars["HealPercent"].BaseValue = Math.Min(100m, Owner.GetPowerAmount<WithPower>());
    }
}