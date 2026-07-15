using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Capabilities;

[RegisterModelCapability]
public sealed class AnanlinIgnorePlayConditionsCapability : ManosabaCardCapability, ICardPlayStateContributor
{
    public bool? CanPlay(CardModel card) => true;

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == Owner && cardPlay.IsLastInSeries)
            RemoveFromOwner();

        return Task.CompletedTask;
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
