using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinSilentAmplificationPower : ManosabaPowerTemplate
{
    private const int DefaultRewritesPerAmplification = 3;

    [SavedProperty] public int PendingRewrites { get; set; }
    [SavedProperty] public int RewritesPerAmplification { get; set; } = DefaultRewritesPerAmplification;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add(new IntVar("Rewrites", RewritesPerAmplification));
            return description;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Rewrites", DefaultRewritesPerAmplification)
    ];

    internal int ReplacementValueMultiplier => Math.Max(1, Amount);

    internal void SetRewritesPerAmplification(int value)
    {
        RewritesPerAmplification = Math.Max(1, value);
    }

    internal void RecordRewrites(int count)
    {
        if (count <= 0) return;

        PendingRewrites += count;
        var gainedMultipliers = PendingRewrites / RewritesPerAmplification;
        if (gainedMultipliers <= 0) return;

        PendingRewrites %= RewritesPerAmplification;
        SetAmount(ReplacementValueMultiplier + gainedMultipliers);
        Flash();
    }
}
