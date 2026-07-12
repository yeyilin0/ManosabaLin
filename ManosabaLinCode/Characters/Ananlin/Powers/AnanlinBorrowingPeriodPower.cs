using ManosabaLin.Characters.Ananlin.Relics;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinBorrowingPeriodPower : ManosabaPowerTemplate
{
    private readonly HashSet<CardModel> _cardsToReplaceWithExhaust = [];
    private int _usedThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            _usedThisTurn = 0;
            _cardsToReplaceWithExhaust.Clear();
        }

        return Task.CompletedTask;
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        if (pileType != PileType.Discard || _usedThisTurn >= Amount) return (pileType, position);
        if (card.Owner?.Creature != Owner) return (pileType, position);
        if (Owner.Player?.Relics.OfType<AnansSketchbook>().FirstOrDefault() is not { } sketchbook) return (pileType, position);
        if (!sketchbook.IsFromRecordedPool(card)) return (pileType, position);

        _usedThisTurn++;
        _cardsToReplaceWithExhaust.Add(card);
        return (PileType.Exhaust, position);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!_cardsToReplaceWithExhaust.Remove(cardPlay.Card)) return;
        if (Owner.Player is not { } ownerPlayer) return;

        Flash();
        await CardPileCmd.Draw(choiceContext, 1, ownerPlayer);
    }
}
