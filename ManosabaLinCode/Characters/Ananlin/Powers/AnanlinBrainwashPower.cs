using ManosabaLin.Characters.Ananlin.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using MinionLib.RightClick;
using MinionLib.RightClick.Easy;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinBrainwashPower : ManosabaPowerTemplate, IEasyRightClickablePower
{
    private const int BacklashSilenceCost = 26;
    private const int BacklashSilenceCostIncrease = 13;

    [SavedProperty] public bool UsedThisTurn { get; set; }
    [SavedProperty] public int RequiredSilenceCost { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add(new IntVar("BacklashSilenceCostIncrease", BacklashSilenceCostIncrease));
            description.Add(new IntVar("RequiredSilenceCost", RequiredSilenceCostForDescription));
            return description;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("BacklashSilenceCost", BacklashSilenceCost),
        new IntVar("BacklashSilenceCostIncrease", BacklashSilenceCostIncrease),
        new IntVar("RequiredSilenceCost", CurrentRequiredSilenceCost)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<AnanlinBrainwashBacklashPower>()
    ];

    public bool CanHandleRightClickLocal(RightClickContext context)
    {
        return context.Player == Owner.Player
            && CanUseRightClick();
    }

    public async Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        if (clickContext.Player != Owner.Player) return;
        if (!CanUseRightClick()) return;

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
            RequiredSilenceCost = Math.Max(BacklashSilenceCost, RequiredSilenceCost) + BacklashSilenceCostIncrease;
            RefreshRequiredSilenceCostVar();
        }

        await PowerCmd.Apply<AnanlinBrainwashBacklashPower>(
            choiceContext,
            Owner,
            1m,
            Owner,
            null,
            false);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
            UsedThisTurn = false;

        return Task.CompletedTask;
    }

    internal void EnterBacklashCostMode()
    {
        if (RequiredSilenceCost <= 0)
            RequiredSilenceCost = BacklashSilenceCost;

        RefreshRequiredSilenceCostVar();
    }

    private int CurrentRequiredSilenceCost
    {
        get
        {
            if (RequiredSilenceCost > 0) return RequiredSilenceCost;
            return Owner?.GetPower<AnanlinBrainwashBacklashPower>()?.Amount >= 3
                ? BacklashSilenceCost
                : 0;
        }
    }

    private int RequiredSilenceCostForDescription => Math.Max(BacklashSilenceCost, CurrentRequiredSilenceCost);

    private bool CanUseRightClick()
    {
        if (UsedThisTurn) return false;
        if (Owner.Player is not { } player) return false;
        if (!AnanlinSilenceIntentManager.CanForceBrainwash(player)) return false;

        var cost = CurrentRequiredSilenceCost;
        return cost <= 0 || Owner.GetPower<SilentPower>()?.Amount >= cost;
    }

    private void RefreshRequiredSilenceCostVar()
    {
        if (DynamicVars.TryGetValue("RequiredSilenceCost", out var requiredSilenceCost))
            requiredSilenceCost.BaseValue = CurrentRequiredSilenceCost;
    }

    public string RightClickPrompt => "强制洗脑";
}
