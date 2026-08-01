using ManosabaLin.Characters.Ananlin.Powers;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Capabilities;

[RegisterModelCapability]
public sealed class AnanlinAuditionPeaceEnergyCapability : ManosabaCardCapability
{
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != Owner || !cardPlay.IsLastInSeries) return;
        if (Owner.Owner is null) return;

        await PowerCmd.Apply<AnanlinPeaceOfMindPower>(
            choiceContext,
            Owner.Owner.Creature,
            1m,
            Owner.Owner.Creature,
            Owner);
        await PlayerCmd.GainEnergy(1m, Owner.Owner);
        RemoveFromOwner();
    }

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Owner?.Owner?.Creature is { } creature && participants.Contains(creature))
            RemoveFromOwner();

        return Task.CompletedTask;
    }
}
