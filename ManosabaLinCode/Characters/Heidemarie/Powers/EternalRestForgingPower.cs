using ManosabaLin.Characters.Heidemarie.Mechanics;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class EternalRestForgingPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    [SavedProperty]
    public int[] LayerGenerationCounts { get; set; } = [];

    public void AddLayer(int generationCount)
    {
        AssertMutable();

        var layers = LayerGenerationCounts;
        Array.Resize(ref layers, layers.Length + 1);
        layers[^1] = generationCount;
        LayerGenerationCounts = layers;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        Flash();
        foreach (var generationCount in GetGenerationCounts())
            await SwordTokenGeneration.AddAuroraSwordsToHand(choiceContext, Owner.Player, generationCount, this);
    }

    private IEnumerable<int> GetGenerationCounts()
    {
        if (LayerGenerationCounts.Length >= Amount)
            return LayerGenerationCounts.Take(Amount);

        return LayerGenerationCounts.Concat(Enumerable.Repeat(1, Amount - LayerGenerationCounts.Length));
    }
}
