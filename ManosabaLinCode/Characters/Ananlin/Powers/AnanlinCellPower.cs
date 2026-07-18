using ManosabaLin.Characters.Ananlin.Relics;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinCellPower : ManosabaPowerTemplate
{
    private CardModel? _returnedSkill;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation cardLocation)
    {
        if (_returnedSkill is not null) return cardLocation;
        if (card.Owner?.Creature != Owner) return cardLocation;
        if (card.Type != CardType.Skill) return cardLocation;
        if (cardLocation.pileType != PileType.Discard && cardLocation.pileType != PileType.Exhaust) return cardLocation;

        _returnedSkill = card;
        card.EnergyCost.AddThisTurnOrUntilPlayed(-1, reduceOnly: true);
        return new CardLocation(cardLocation.player, PileType.Draw, CardPilePosition.Top);
    }

    public override Task AfterModifyingCardPlayResultLocation(CardModel card, CardLocation cardLocation)
    {
        if (card == _returnedSkill)
            Flash();

        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (Owner.Player?.Relics.OfType<AnansSketchbook>().FirstOrDefault() is { } sketchbook)
            await sketchbook.AddSilence(choiceContext, (int)Amount, null);
        else
            await PowerCmd.Apply<SilentPower>(choiceContext, Owner, Amount, Owner, null);
    }

    public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        var retainable = PileType.Hand.GetPile(player).Cards
            .Where(static card => !card.ShouldRetainThisTurn)
            .ToList();
        if (retainable.Count == 0) return;

        var selected = await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, 1),
            card => retainable.Contains(card),
            this);

        foreach (var card in selected)
            card.GiveSingleTurnRetain();
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
            await PowerCmd.Remove(this);
    }
}
