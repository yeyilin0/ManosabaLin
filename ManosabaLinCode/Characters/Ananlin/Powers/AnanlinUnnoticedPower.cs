using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Relics;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinUnnoticedPower : ManosabaPowerTemplate
{
    private CardModel? _sourceCard;
    private int _cardsPerPeace;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    protected override bool IsVisibleInternal => false;

    internal void Arm(CardModel sourceCard, int cardsPerPeace)
    {
        _sourceCard = sourceCard;
        _cardsPerPeace = cardsPerPeace;
        Amount = 1;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == _sourceCard) return;
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card.Type == CardType.Attack) return;

        Flash();
        if (!HasAnyEnemyAttackIntent())
        {
            await PowerCmd.Apply<AnanlinPeaceOfMindPower>(choiceContext, Owner, 1, Owner, _sourceCard);

            var bonusDraws = _sourceCard?.PeaceOfMindAmount() * _cardsPerPeace ?? 0;
            if (bonusDraws > 0 && Owner.Player is { } player)
                await CardPileCmd.Draw(choiceContext, bonusDraws, player);
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

    private bool HasAnyEnemyAttackIntent()
    {
        return Owner.Player?.Relics.OfType<AnansSketchbook>().FirstOrDefault()?.HasAnyEnemyAttackIntent() == true;
    }
}
