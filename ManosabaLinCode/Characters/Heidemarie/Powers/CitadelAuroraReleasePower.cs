using ManosabaLin.Characters.Heidemarie.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class CitadelAuroraReleasePower : ManosabaPowerTemplate
{
    private const int DefaultSwordThreshold = 2;
    private const int DefaultAuroraChainGain = 1;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    [SavedProperty]
    public int SwordThreshold { get; set; } = DefaultSwordThreshold;

    [SavedProperty]
    public int AuroraChainGain { get; set; } = DefaultAuroraChainGain;

    public void Configure(int swordThreshold, int auroraChainGain)
    {
        AssertMutable();

        SwordThreshold = Math.Max(1, swordThreshold);
        AuroraChainGain = Math.Max(0, auroraChainGain);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || !participants.Contains(Owner))
            return;

        var player = Owner.Player;
        if (player == null)
            return;

        var gainPerThreshold = Math.Max(0, AuroraChainGain);
        if (gainPerThreshold <= 0)
            return;

        var swordCount = PileType.Discard
            .GetPile(player)
            .Cards
            .Count(static card => card is AuroraSword or CrimsonSword);
        var chainGain = swordCount / Math.Max(1, SwordThreshold) * gainPerThreshold;
        if (chainGain <= 0)
            return;

        Flash();
        await PowerCmd.Apply<AuroraChainPower>(
            choiceContext,
            Owner,
            chainGain,
            Owner,
            null,
            false);
    }
}
