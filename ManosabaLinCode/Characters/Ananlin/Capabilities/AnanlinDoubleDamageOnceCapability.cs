using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Capabilities;

[RegisterModelCapability]
public sealed class AnanlinDoubleDamageOnceCapability : ManosabaCardCapability
{
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return cardSource == Owner && props.IsPoweredAttack() ? 2m : 1m;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == Owner)
            RemoveFromOwner();

        return Task.CompletedTask;
    }
}
