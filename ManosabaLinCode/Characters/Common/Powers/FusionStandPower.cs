using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Patches;

namespace ManosabaLin.Characters.Common.Powers;

[RegisterPower]
public sealed class FusionStandPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.Monster != null && FusionStandManager.EnsureStand(Owner.Monster))
        {
            FusionStandVisualPatch.TryCreateStand(Owner.Monster);
            FusionStandIntentPatch.TryApplyFusionMove(Owner.Monster);
        }

        await Task.CompletedTask;
    }
}
