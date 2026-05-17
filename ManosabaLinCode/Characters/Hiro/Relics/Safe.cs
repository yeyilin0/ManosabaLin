using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Safe : ManosabaRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    private static IEnumerable<RelicModel> GetValidRelics(IRunState state)
    {
        var orobasOptions = ModelDb.Event<Orobas>().AllPossibleOptions
            .Where(o => o.Relic != null && o.Relic.IsAllowed(state) && !(o.Relic is Safe));
        var paelOptions = ModelDb.Event<Pael>().AllPossibleOptions
            .Where(o => o.Relic != null && o.Relic.IsAllowed(state) && !(o.Relic is Safe));
        var tezcataraOptions = ModelDb.Event<Tezcatara>().AllPossibleOptions
            .Where(o => o.Relic != null && o.Relic.IsAllowed(state) && !(o.Relic is Safe));

        return orobasOptions.Concat(paelOptions).Concat(tezcataraOptions)
            .Select(o => o.Relic).OfType<RelicModel>();
    }

    public override async Task AfterObtained()
    {
        var relic = this;
        var availableRelics = GetValidRelics(relic.Owner.RunState).ToList();
        if (availableRelics.Count == 0) return;

        var rng = relic.Owner.PlayerRng.Rewards;
        var selectedRelics = availableRelics
            .OrderBy(_ => rng.NextFloat())
            .Take(2)
            .ToList();

        var rewards = selectedRelics
            .Select(r => (Reward)new RelicReward(r, relic.Owner))
            .ToList();

        await new RewardsSet(relic.Owner)
            .WithCustomRewards(rewards)
            .WithSkippingDisallowed()
            .Offer();
    }
}