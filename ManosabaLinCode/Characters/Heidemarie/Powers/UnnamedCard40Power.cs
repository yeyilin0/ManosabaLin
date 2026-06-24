using ManosabaLin.Characters.Heidemarie.Mechanics;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class UnnamedCard40Power : ManosabaPowerTemplate, ISwordGenerationListener
{
    public const int BaseChancePercent = 1;
    public const int BaseCrimsonSwordCount = 1;

    private readonly HashSet<int> _activeLayerIndices = [];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    [SavedProperty]
    public int[] LayerChancePercents { get; set; } = [];

    [SavedProperty]
    public int[] LayerCrimsonSwordCounts { get; set; } = [];

    public void AddLayer(int chancePercent, int crimsonSwordCount)
    {
        AssertMutable();

        var chances = LayerChancePercents;
        Array.Resize(ref chances, chances.Length + 1);
        chances[^1] = Math.Max(0, chancePercent);
        LayerChancePercents = chances;

        var counts = LayerCrimsonSwordCounts;
        Array.Resize(ref counts, counts.Length + 1);
        counts[^1] = Math.Max(0, crimsonSwordCount);
        LayerCrimsonSwordCounts = counts;
    }

    public async Task OnSwordGenerationSucceeded(
        PlayerChoiceContext choiceContext,
        SwordGenerationRequest request,
        SwordGenerationResult result)
    {
        if (Amount <= 0)
            return;
        if (!ReferenceEquals(request.Owner.Creature, Owner))
            return;

        var crimsonSuccessCount = result.SuccessCountFor(SwordTokenKind.Crimson);
        if (crimsonSuccessCount <= 0)
            return;

        var layers = GetLayers().ToArray();
        for (var layerIndex = 0; layerIndex < layers.Length; layerIndex++)
        {
            if (_activeLayerIndices.Contains(layerIndex))
                continue;

            var layer = layers[layerIndex];
            var bonusCount = RollBonusCrimsonSwordCount(request.Owner, crimsonSuccessCount, layer);
            if (bonusCount <= 0)
                continue;

            Flash();
            _activeLayerIndices.Add(layerIndex);
            try
            {
                await SwordTokenGeneration.AddCrimsonSwordsToHand(choiceContext, request.Owner, bonusCount, this);
            }
            finally
            {
                _activeLayerIndices.Remove(layerIndex);
            }
        }
    }

    private IEnumerable<(int ChancePercent, int CrimsonSwordCount)> GetLayers()
    {
        var layerCount = Math.Max(0, (int)Amount);
        for (var i = 0; i < layerCount; i++)
        {
            var chancePercent = i < LayerChancePercents.Length
                ? LayerChancePercents[i]
                : BaseChancePercent;
            var crimsonSwordCount = i < LayerCrimsonSwordCounts.Length
                ? LayerCrimsonSwordCounts[i]
                : BaseCrimsonSwordCount;

            yield return (chancePercent, crimsonSwordCount);
        }
    }

    private static int RollBonusCrimsonSwordCount(
        Player owner,
        int crimsonSuccessCount,
        (int ChancePercent, int CrimsonSwordCount) layer)
    {
        if (layer.ChancePercent <= 0 || layer.CrimsonSwordCount <= 0)
            return 0;

        var bonusCount = 0;
        for (var i = 0; i < crimsonSuccessCount; i++)
        {
            if (RollSucceeds(owner, layer.ChancePercent))
                bonusCount += layer.CrimsonSwordCount;
        }

        return bonusCount;
    }

    private static bool RollSucceeds(Player owner, int chancePercent)
    {
        if (chancePercent >= 100)
            return true;

        return owner.RunState.Rng.CombatCardGeneration.NextInt(100) < chancePercent;
    }
}
