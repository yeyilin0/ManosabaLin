namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinDoorGapBlockBonusPower : ManosabaPowerTemplate
{
    private CardModel? _card;
    private int _block;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    protected override bool IsVisibleInternal => false;

    internal void Track(CardModel card, int block)
    {
        _card = card;
        _block = block;
        Amount = Math.Max(1, block);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != _card) return;

        if (_block > 0)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner, _block, ValueProp.Move, cardPlay);
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
