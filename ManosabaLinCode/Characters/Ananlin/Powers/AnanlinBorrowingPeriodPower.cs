using ManosabaLin.Characters.Ananlin.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinBorrowingPeriodPower : ManosabaPowerTemplate
{
    private readonly HashSet<CardModel> _cardsChangedToExhaust = [];
    private readonly HashSet<CardModel> _cardsAwaitingDraw = [];

    [SavedProperty] public int UsedThisTurn { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            UsedThisTurn = 0;
            _cardsChangedToExhaust.Clear();
            _cardsAwaitingDraw.Clear();
        }

        return Task.CompletedTask;
    }

    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation cardLocation)
    {
        if (cardLocation.pileType != PileType.Discard || UsedThisTurn >= Amount) return cardLocation;
        if (!IsRecordedPoolCardOwnedByOwner(card)) return cardLocation;

        _cardsChangedToExhaust.Add(card);
        return new CardLocation(cardLocation.player, PileType.Exhaust, cardLocation.position);
    }

    public override Task AfterModifyingCardPlayResultLocation(CardModel card, CardLocation cardLocation)
    {
        if (!_cardsChangedToExhaust.Remove(card)) return Task.CompletedTask;
        if (cardLocation.pileType != PileType.Exhaust) return Task.CompletedTask;
        if (UsedThisTurn >= Amount) return Task.CompletedTask;

        UsedThisTurn++;
        _cardsAwaitingDraw.Add(card);
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!_cardsAwaitingDraw.Remove(cardPlay.Card)) return;
        if (cardPlay.ResultPile != PileType.Exhaust) return;
        if (Owner.Player is not { } ownerPlayer) return;

        Flash();
        await CardPileCmd.Draw(choiceContext, 1, ownerPlayer);
    }

    private bool IsRecordedPoolCardOwnedByOwner(CardModel card)
    {
        if (card.Owner?.Creature != Owner) return false;
        if (Owner.Player?.Relics.OfType<AnansSketchbook>().FirstOrDefault() is not { } sketchbook) return false;

        return sketchbook.IsFromRecordedPool(card);
    }
}
