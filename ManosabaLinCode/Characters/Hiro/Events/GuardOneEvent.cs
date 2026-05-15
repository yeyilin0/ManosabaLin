using ManosabaLin.Characters.Hiro.Monsters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Hiro.Events;

[RegisterSharedEvent]
public sealed class GuardOneEvent : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://ManosabaLin/images/events/guard_one.png"
    );

    public override bool IsAllowed(IRunState runState)
    {
        if (runState is RunState concreteRunState)
            return GuardOneEventState.ShouldTrigger(concreteRunState);
        return false;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(this, ChoosePower, InitialOptionKey("POWER")),
        new EventOption(this, ChooseHeal, InitialOptionKey("HEAL"))
    ];

    private async Task ChoosePower()
    {
        if (Owner?.Creature == null) return;

        await CreatureCmd.GainMaxHp(Owner.Creature, 13m);
        await UpgradeOneCard();
        ReplaceBoss();

        if (Owner?.RunState is RunState runState)
            GuardOneEventState.MarkTriggered(runState);

        SetEventFinished(PageDescription("OPTION_POWER"));
    }

    private async Task ChooseHeal()
    {
        if (Owner?.Creature == null) return;

        await CreatureCmd.Heal(Owner.Creature, 13m);
        await UpgradeOneCard();

        if (Owner?.RunState is RunState runState)
            GuardOneEventState.MarkTriggered(runState);

        SetEventFinished(PageDescription("OPTION_HEAL"));
    }

    private async Task UpgradeOneCard()
    {
        if (Owner == null) return;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        var selection = await CardSelectCmd.FromDeckForUpgrade(Owner, prefs);
        foreach (var card in selection)
        {
            CardCmd.Upgrade(card, CardPreviewStyle.None);
        }
    }

    private void ReplaceBoss()
    {
        try
        {
            var runState = Owner?.RunState;
            if (runState == null) return;

            var encounter = ModelDb.Get<GuardOneEncounter>();

            MapCmd.SetBossEncounter(runState, encounter);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[GuardOneEvent] ReplaceBoss failed: {ex.Message}");
        }
    }
}