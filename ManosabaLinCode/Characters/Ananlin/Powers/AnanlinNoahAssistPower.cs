using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinNoahAssistPower : ManosabaPowerTemplate
{
    internal const string RewrittenNymPowerKey = "RewrittenNymPower";
    internal const string NoRewriteNymPowerKey = "NoRewriteNymPower";

    [SavedProperty] public int RewrittenNymAmount { get; set; } = 1;
    [SavedProperty] public int NoRewriteNymAmount { get; set; } = 3;
    [SavedProperty] public bool FreeRewriteUsedThisTurn { get; set; }
    [SavedProperty] public bool RewroteEnemyThisTurn { get; set; }

    private readonly HashSet<Creature> _rewrittenTargetsThisTurn = [];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Description
    {
        get
        {
            RefreshNymVars();
            var description = base.Description;
            DynamicVars.AddTo(description);
            return description;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<NymPower>(RewrittenNymPowerKey, 1m),
        new PowerVar<NymPower>(NoRewriteNymPowerKey, 3m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<AnanlinBrainwashPower>(),
        HoverTipFactory.FromPower<NymPower>()
    ];

    internal bool HasFreeRewriteCharge => Amount > 0 && !FreeRewriteUsedThisTurn;

    internal void Configure(int rewrittenNymAmount, int noRewriteNymAmount)
    {
        RewrittenNymAmount = Math.Max(RewrittenNymAmount, rewrittenNymAmount);
        NoRewriteNymAmount = Math.Max(NoRewriteNymAmount, noRewriteNymAmount);
        RefreshNymVars();
        InvokeDisplayAmountChanged();
    }

    internal Task ResolveFreeRewriteAttempt(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<Creature> rewrittenTargets,
        Creature? applier,
        CardModel? source)
    {
        if (!HasFreeRewriteCharge) return Task.CompletedTask;

        Flash();
        FreeRewriteUsedThisTurn = true;
        RewroteEnemyThisTurn |= rewrittenTargets.Any();

        foreach (var target in rewrittenTargets
            .Where(static target => target.IsAlive)
            .Distinct())
        {
            _rewrittenTargetsThisTurn.Add(target);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || !participants.Contains(Owner)) return;

        await ApplyNymAtTurnEnd(choiceContext);
        await PowerCmd.Remove(this);
    }

    private async Task ApplyNymAtTurnEnd(PlayerChoiceContext choiceContext)
    {
        var hasRewrittenTargets = RewroteEnemyThisTurn || _rewrittenTargetsThisTurn.Count > 0;
        var targets = _rewrittenTargetsThisTurn
            .Where(static target => target.IsAlive)
            .ToArray();
        var nymAmount = RewrittenNymAmount;

        if (!hasRewrittenTargets)
        {
            targets = Owner.CombatState?.Enemies
                .Where(static target => target.IsAlive)
                .ToArray() ?? [];
            nymAmount = NoRewriteNymAmount;
        }

        if (targets.Length == 0) return;

        Flash();
        foreach (var target in targets)
            await PowerCmd.Apply<NymPower>(
                choiceContext,
                target,
                nymAmount,
                Owner,
                null,
                false);
    }

    private void RefreshNymVars()
    {
        if (DynamicVars.TryGetValue(RewrittenNymPowerKey, out var rewritten))
            rewritten.BaseValue = RewrittenNymAmount;

        if (DynamicVars.TryGetValue(NoRewriteNymPowerKey, out var noRewrite))
            noRewrite.BaseValue = NoRewriteNymAmount;
    }
}
