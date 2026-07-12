using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinSilentAmplificationPower : ManosabaPowerTemplate
{
    private const int RewritesPerAmplification = 2;

    [SavedProperty] public int PendingRewrites { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Rewrites", RewritesPerAmplification)
    ];

    internal int ReplacementValueMultiplier => Math.Max(1, Amount);

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
