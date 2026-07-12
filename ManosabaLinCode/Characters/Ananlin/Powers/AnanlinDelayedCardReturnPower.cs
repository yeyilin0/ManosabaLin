namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinDelayedCardReturnPower : ManosabaPowerTemplate
{
    private readonly List<ReturnEntry> _cards = [];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    internal void AddCard(CardModel card)
    {
        _cards.Add(new ReturnEntry(card.CanonicalInstance, card.CurrentUpgradeLevel));
        Amount = _cards.Count;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        var cards = _cards.ToArray();
        _cards.Clear();

        foreach (var entry in cards)
        {
            var returned = CombatState.CreateCard(entry.CanonicalCard, player);
            for (var i = 0; i < entry.UpgradeLevel; i++)
                CardCmd.Upgrade(returned);

            await CardPileCmd.AddGeneratedCardToCombat(returned, PileType.Hand, player);
        }

        await PowerCmd.Remove(this);
    }

    private sealed record ReturnEntry(CardModel CanonicalCard, int UpgradeLevel);
}
