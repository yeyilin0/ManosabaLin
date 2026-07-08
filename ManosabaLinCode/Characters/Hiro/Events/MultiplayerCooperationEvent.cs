using ManosabaLin.Characters.Ema.Relics;
using ManosabaLin.Characters.Hiro.Relics;
using ManosabaLin.Settings;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Hiro.Events;

[RegisterActEvent(typeof(Overgrowth))]
public sealed class MultiplayerCooperationEvent : ModEventTemplate
{
    private const int GoldReward = 300;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://ManosabaLin/images/events/multiplayercooperationevent.png"
    );

    public override bool IsShared => true;

    public override bool IsAllowed(IRunState state)
    {
        if (!EventSettingsService.IsMultiplayerCooperationEventEnabled)
            return false;

        return base.IsAllowed(state);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(this, ChooseAllIn, InitialOptionKey("ALL_IN")),
        new EventOption(this, ChooseGold, InitialOptionKey("GOLD"))
    ];

    private async Task ChooseAllIn()
    {
        if (Owner == null) return;

        if (Owner.Gold > 0)
            await PlayerCmd.LoseGold(Owner.Gold, Owner);

        if (Rng.NextFloat() >= 0.5f || !await TryObtainRandomChip())
        {
            SetEventFinished(PageDescription("OPTION_ALL_IN_EMPTY"));
            return;
        }

        SetEventFinished(PageDescription("OPTION_ALL_IN_CHIP"));
    }

    private async Task ChooseGold()
    {
        if (Owner == null) return;

        await PlayerCmd.GainGold(GoldReward, Owner);
        SetEventFinished(PageDescription("OPTION_GOLD"));
    }

    private async Task<bool> TryObtainRandomChip()
    {
        var availableChips = new List<RelicModel>
        {
            ModelDb.Relic<Hirochouma>(),
            ModelDb.Relic<Emachouma>(),
            ModelDb.Relic<Xuelichouma>()
        }.Where(relic => Owner.Relics.All(owned => owned.Id != relic.Id)).ToList();

        var chip = Rng.NextItem(availableChips);
        if (chip == null)
            return false;

        await RelicCmd.Obtain(chip.ToMutable(), Owner);
        return true;
    }
}
