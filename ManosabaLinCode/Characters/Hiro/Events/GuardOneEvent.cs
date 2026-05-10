using ManosabaLin.Characters.Hiro.Monsters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
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
        // 只在第一次进入火堆时出现
        return GuardOneEventState.ShouldTrigger;
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

        SetEventFinished(PageDescription("OPTION_POWER"));
    }

    private async Task ChooseHeal()
    {
        if (Owner?.Creature == null) return;

        await CreatureCmd.Heal(Owner.Creature, 13m);
        await UpgradeOneCard();

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
            if (runState == null)
            {
                MainFile.Logger.Info("[GuardOneEvent] ReplaceBoss: runState is null");
                return;
            }

            var act = runState.Acts[runState.CurrentActIndex];
            var encounter = ModelDb.Get<GuardOneEncounter>();
            if (encounter == null)
            {
                MainFile.Logger.Info("[GuardOneEvent] ReplaceBoss: GuardOneEncounter not found in ModelDb");
                return;
            }

            act.SetBossEncounter(encounter);
            MainFile.Logger.Info("[GuardOneEvent] ReplaceBoss: Boss encounter set to GuardOne");
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Info($"[GuardOneEvent] ReplaceBoss failed: {ex.Message}");
        }
    }
}
