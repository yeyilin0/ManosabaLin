using ManosabaLin.Characters.Ananlin.Cards;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinClutchSleevePower : ManosabaPowerTemplate
{
    private CardModel? _card;
    private bool _armed;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    protected override bool IsVisibleInternal => false;

    internal void Track(CardModel card)
    {
        _card = card;
        Amount = 1;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
            _armed = true;

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != _card) return;

        if (_armed)
        {
            Flash();
            await cardPlay.Card.GainPeaceOfMind(choiceContext);
        }

        await PowerCmd.Remove(this);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side && _armed)
            await PowerCmd.Remove(this);
    }
}
