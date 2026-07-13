using ManosabaLin.Characters.Ananlin.Cards;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinSyncedBreathingPower : ManosabaPowerTemplate
{
    private CardModel? _sourceCard;
    private CardType? _lastType;
    private int _streak;
    private int _requiredStreak = 3;
    private int _bonusDraws;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    protected override bool IsVisibleInternal => false;

    internal void Arm(CardModel sourceCard, int bonusDraws, int requiredStreak)
    {
        _sourceCard = sourceCard;
        _bonusDraws = bonusDraws;
        _requiredStreak = Math.Max(2, requiredStreak);
        Amount = 1;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == _sourceCard) return;
        if (cardPlay.Card.Owner?.Creature != Owner) return;

        if (_lastType == cardPlay.Card.Type)
            _streak++;
        else
        {
            _lastType = cardPlay.Card.Type;
            _streak = 1;
        }

        if (_streak < _requiredStreak) return;

        Flash();
        await PowerCmd.Apply<AnanlinPeaceOfMindPower>(choiceContext, Owner, 1, Owner, _sourceCard);

        if (_bonusDraws > 0 && _lastType is { } type)
        {
            await _sourceCard!.PullMatchingCardsToHand(
                choiceContext,
                _bonusDraws,
                card => AnanlinCardHelpers.IsPlayableCombatCard(card) && card.Type != type);
        }

        await PowerCmd.Remove(this);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
            await PowerCmd.Remove(this);
    }
}
