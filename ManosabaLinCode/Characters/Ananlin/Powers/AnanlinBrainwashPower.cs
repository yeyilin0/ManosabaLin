using ManosabaLin.Characters.Ananlin.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using MinionLib.RightClick;
using MinionLib.RightClick.Easy;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinBrainwashPower : ManosabaPowerTemplate, IEasyRightClickablePower
{
    [SavedProperty] public bool UsedThisTurn { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add(new IntVar("BacklashSilenceCostIncrease", AnanlinBrainwashBacklashPower.BrainwashSilenceCostIncrease));
            description.Add(new IntVar("RequiredSilenceCost", RequiredSilenceCostDescriptionVar));
            return description;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("BacklashSilenceCost", AnanlinBrainwashBacklashPower.BrainwashSilenceCost),
        new IntVar("BacklashSilenceCostIncrease", AnanlinBrainwashBacklashPower.BrainwashSilenceCostIncrease),
        new IntVar("RequiredSilenceCost", AnanlinBrainwashBacklashPower.BrainwashSilenceCost)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<AnanlinBrainwashBacklashPower>()
    ];

    public bool CanHandleRightClickLocal(RightClickContext context)
    {
        return context.Model == this
            && context.Player == Owner.Player
            && CanUseRightClick();
    }

    public async Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        if (clickContext.Player != Owner.Player) return;
        if (!CanUseRightClick()) return;

        var backlash = Owner.GetPower<AnanlinBrainwashBacklashPower>();
        var silenceCost = CurrentRequiredSilenceCost;
        var paidSilenceCost = false;

        async Task<bool> SpendSilenceCost()
        {
            if (silenceCost <= 0) return true;
            var silence = Owner.GetPower<SilentPower>();
            if (silence is null || silence.Amount < silenceCost) return false;

            await PowerCmd.ModifyAmount(choiceContext, silence, -silenceCost, Owner, null);
            paidSilenceCost = true;
            return true;
        }

        var rewritten = await AnanlinSilenceIntentManager.ForceBrainwash(
            choiceContext,
            clickContext.Player,
            SpendSilenceCost);
        if (!rewritten) return;

        UsedThisTurn = true;
        if (paidSilenceCost)
        {
            backlash?.IncreaseBrainwashSilenceCostAfterPaidUse();
            RefreshRequiredSilenceCostVar();
        }

        await PowerCmd.Apply<AnanlinBrainwashBacklashPower>(
            choiceContext,
            Owner,
            1m,
            Owner,
            null,
            false);

        RefreshRequiredSilenceCostVar();
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
            UsedThisTurn = false;

        return Task.CompletedTask;
    }

    private int CurrentRequiredSilenceCost =>
        IsCanonical
            ? 0
            : Owner.GetPower<AnanlinBrainwashBacklashPower>()?.CurrentBrainwashSilenceCost ?? 0;

    private int RequiredSilenceCostDescriptionVar =>
        IsCanonical
            ? AnanlinBrainwashBacklashPower.BrainwashSilenceCost
            : Owner.GetPower<AnanlinBrainwashBacklashPower>()?.BrainwashSilenceCostForDescription
              ?? AnanlinBrainwashBacklashPower.BrainwashSilenceCost;

    private bool CanUseRightClick()
    {
        if (UsedThisTurn) return false;
        if (Owner.Player is not { } player) return false;
        if (!AnanlinSilenceIntentManager.CanForceBrainwash(player)) return false;

        var cost = CurrentRequiredSilenceCost;
        return cost <= 0 || Owner.GetPower<SilentPower>()?.Amount >= cost;
    }

    internal void RefreshRequiredSilenceCostVar()
    {
        if (DynamicVars.TryGetValue("RequiredSilenceCost", out var requiredSilenceCost))
            requiredSilenceCost.BaseValue = RequiredSilenceCostDescriptionVar;
    }

    public string RightClickPrompt => "强制洗脑";
}
