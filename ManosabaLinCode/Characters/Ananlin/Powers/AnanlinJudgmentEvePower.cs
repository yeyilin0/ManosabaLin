using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinJudgmentEvePower : ManosabaPowerTemplate
{
    private const int MinimumRecords = 3;

    [SavedProperty] public int RecordedRewrites { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => RecordedRewrites;

    internal void RecordRewrites(int count)
    {
        if (count <= 0) return;

        RecordedRewrites += count;
        InvokeDisplayAmountChanged();
        Flash();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || RecordedRewrites < MinimumRecords) return;

        var rewards = Math.Min(RecordedRewrites, (int)Amount);
        RecordedRewrites = 0;
        InvokeDisplayAmountChanged();

        Flash();
        await CardPileCmd.Draw(choiceContext, rewards, player);
        await PlayerCmd.GainEnergy(rewards, player);
    }
}
