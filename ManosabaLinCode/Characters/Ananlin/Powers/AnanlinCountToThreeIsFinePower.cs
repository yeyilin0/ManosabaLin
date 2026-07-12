namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinCountToThreeIsFinePower : ManosabaPowerTemplate
{
    private CardModel? _sourceCard;
    private CardModel? _discountedCard;
    private int _drawCounter;
    private int _discountCounter;
    private int _discountReady;
    private int _discount;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    protected override bool IsVisibleInternal => false;

    internal void Arm(CardModel sourceCard, int discount)
    {
        _sourceCard = sourceCard;
        _discount = discount;
        Amount = 1;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (_discountReady <= 0 || _discount <= 0) return false;
        if (!ShouldCount(card)) return false;

        modifiedCost = Math.Max(0, originalCost - _discount);
        return modifiedCost != originalCost;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (_discountReady <= 0 || _discount <= 0) return Task.CompletedTask;
        if (!ShouldCount(cardPlay.Card)) return Task.CompletedTask;

        _discountReady--;
        _discountedCard = cardPlay.Card;
        Flash();
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!ShouldCount(cardPlay.Card)) return;
        if (cardPlay.PlayIndex != cardPlay.PlayCount - 1) return;

        if (cardPlay.Card == _discountedCard)
        {
            _discountedCard = null;
            return;
        }

        _drawCounter++;
        _discountCounter++;

        if (_drawCounter >= 3 && Owner.Player is { } player)
        {
            _drawCounter -= 3;
            Flash();
            await CardPileCmd.Draw(choiceContext, 1, player);
        }

        if (_discount <= 0) return;
        while (_discountCounter >= 2)
        {
            _discountCounter -= 2;
            _discountReady++;
        }
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
