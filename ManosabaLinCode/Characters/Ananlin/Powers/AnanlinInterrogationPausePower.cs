namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinInterrogationPausePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card.Type != CardType.Attack) return;
        if (Owner.Player is not { } ownerPlayer) return;

        Flash();
        await PlayerCmd.GainEnergy(1, ownerPlayer);
        await PowerCmd.ModifyAmount(choiceContext, this, -1, Owner, cardPlay.Card);
    }
}
