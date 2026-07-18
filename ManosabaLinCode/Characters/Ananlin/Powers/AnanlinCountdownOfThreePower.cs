namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinCountdownOfThreePower : ManosabaPowerTemplate
{
    private CardModel? _sourceCard;
    private CardModel? _thirdCard;
    private int _playedCards;
    private int _bonus;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    internal void Arm(CardModel sourceCard, int bonus)
    {
        _sourceCard = sourceCard;
        _bonus = bonus;
        Amount = 1;
    }

    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation cardLocation)
    {
        if (_thirdCard is not null) return cardLocation;
        if (!ShouldCount(card)) return cardLocation;
        if (_playedCards != 2) return cardLocation;

        _thirdCard = card;
        return new CardLocation(cardLocation.player, PileType.Hand, CardPilePosition.Bottom);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return cardSource == _thirdCard && cardPlay is not null ? _bonus : 0m;
    }

    public override decimal ModifyBlockAdditive(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return cardSource == _thirdCard && cardPlay is not null ? _bonus : 0m;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!ShouldCount(cardPlay.Card)) return;
        if (cardPlay.PlayIndex != cardPlay.PlayCount - 1) return;

        if (cardPlay.Card == _thirdCard)
        {
            Flash();
            cardPlay.Card.EnergyCost.SetThisTurn(0, reduceOnly: true);
            cardPlay.Card.ExhaustOnNextPlay = true;
            await PowerCmd.Remove(this);
            return;
        }

        _playedCards++;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
            await PowerCmd.Remove(this);
    }

    private bool ShouldCount(CardModel card)
    {
        return card != _sourceCard && card.Owner?.Creature == Owner;
    }
}
