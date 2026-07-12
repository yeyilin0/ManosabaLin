namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinGroupChatSmellPower : ManosabaPowerTemplate
{
    private readonly HashSet<ulong> _triggeredPlayersThisTurn = [];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature.Side == Owner.Side)
            _triggeredPlayersThisTurn.Remove(player.NetId);

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner is not { } player) return;
        if (player == Owner.Player) return;
        if (player.Creature.Side != Owner.Side || !player.Creature.IsAlive) return;
        if (!_triggeredPlayersThisTurn.Add(player.NetId)) return;

        var enemies = CombatState.HittableEnemies.ToArray();
        if (enemies.Length == 0) return;

        Flash();
        var target = Owner.Player?.RunState.Rng.CombatTargets.NextItem(enemies)
            ?? enemies[0];
        await PowerCmd.Apply<AnanlinStrangeSmellPower>(
            choiceContext,
            target,
            Math.Max(1, (int)Amount),
            Owner,
            null);
    }
}
