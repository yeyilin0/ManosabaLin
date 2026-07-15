using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Capabilities;

[RegisterModelCapability]
public sealed class AnanlinLoverDoublePlayCapability : ManosabaCardCapability
{
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        return card == Owner ? playCount + 1 : playCount;
    }

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        

        return Task.CompletedTask;
    }

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Owner.Owner?.Creature is { } creature && participants.Contains(creature))
            RemoveFromOwner();

        return Task.CompletedTask;
    }
}
