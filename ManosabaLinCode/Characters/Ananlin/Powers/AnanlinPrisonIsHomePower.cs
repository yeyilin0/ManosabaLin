namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinPrisonIsHomePower : ManosabaPowerTemplate
{
    private bool _hasPreviousTurn;
    private bool _attackedThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    internal void InitializeCurrentTurn(bool attackedThisTurn)
    {
        _attackedThisTurn = attackedThisTurn;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        if (_hasPreviousTurn && !_attackedThisTurn)
        {
            Flash();
            await PowerCmd.Apply<AnanlinCellPower>(choiceContext, Owner, 2, Owner, null);

            if (Amount > 0)
                await PlayerCmd.GainEnergy((int)Amount, player);
        }
        else if (Owner.GetPower<AnanlinCellPower>() is { } cell)
        {
            await PowerCmd.Remove(cell);
        }

        _hasPreviousTurn = true;
        _attackedThisTurn = false;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card.Type != CardType.Attack) return;

        _attackedThisTurn = true;
        if (Owner.GetPower<AnanlinCellPower>() is { } cell)
            await PowerCmd.Remove(cell);
    }
}
