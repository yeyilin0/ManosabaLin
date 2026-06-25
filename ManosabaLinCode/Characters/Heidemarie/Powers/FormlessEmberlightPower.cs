using ManosabaLin.Characters.Heidemarie.Mechanics;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class FormlessEmberlightPower : ManosabaPowerTemplate, ISwordGenerationListener
{
    public const string SwordCountKey = "SwordCount";
    public const int BaseSwordCount = 1;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(SwordCountKey, BaseSwordCount)
    ];

    [SavedProperty]
    public int SwordCount { get; private set; } = BaseSwordCount;

    public void Configure(int swordCount)
    {
        AssertMutable();

        SwordCount = Math.Max(SwordCount, Math.Max(BaseSwordCount, swordCount));
        DynamicVars[SwordCountKey].BaseValue = SwordCount;
    }

    public Task OnSwordGenerationRequested(PlayerChoiceContext choiceContext, SwordGenerationRequest request)
    {
        if (Amount <= 0)
            return Task.CompletedTask;
        if (!ReferenceEquals(request.Owner.Creature, Owner))
            return Task.CompletedTask;
        if (request.OriginalCount <= 0 || request.Batches.Count == 0)
            return Task.CompletedTask;

        request.ReplaceWith(CreateRandomSwordBatches(request.Owner, SwordCount));
        Flash();
        return Task.CompletedTask;
    }

    private static IEnumerable<SwordGenerationBatch> CreateRandomSwordBatches(Player owner, int count)
    {
        for (var i = 0; i < count; i++)
            yield return new SwordGenerationBatch(RandomSwordKind(owner), 1);
    }

    private static SwordTokenKind RandomSwordKind(Player owner)
    {
        return owner.RunState.Rng.CombatCardGeneration.NextInt(2) == 0
            ? SwordTokenKind.Aurora
            : SwordTokenKind.Crimson;
    }
}
