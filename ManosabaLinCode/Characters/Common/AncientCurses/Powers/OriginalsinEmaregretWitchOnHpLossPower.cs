namespace ManosabaLin.Characters.Common.AncientCurses.Powers;

/// <summary>
///     【悔恨】：「本回合」你失去生命时，随机一名敌人获得等量的【魔女因子】。
///     <para>
///         文案里的「本回合」跨过敌方回合 ⇒ <b>不在自己回合结束时</b>清除，
///         而是在<b>下一个自己回合开始时</b>才清除（写法参照同目录
///         <see cref="OriginalsinMiliaReturnPower" /> / <see cref="OriginalsinWitchificationLossPower" />）。
///     </para>
/// </summary>
[RegisterPower]
public sealed class OriginalsinEmaregretWitchOnHpLossPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner) return;
        if (delta >= 0m) return;
        if (Owner.CombatState is not { } combatState) return;

        var enemies = combatState.Enemies.Where(c => c.IsAlive).ToList();
        if (enemies.Count == 0) return;

        var rng = Owner.Player?.RunState.Rng.CombatCardGeneration;
        if (rng is null) return;
        var enemy = rng.NextItem(enemies);
        if (enemy is null) return;

        var ctx = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<EmaWitchFactorPower>(ctx, enemy, Math.Abs(delta), Owner, null);
    }

    // 本回合跨过敌方回合（期间失去生命照样触发），下一个自己回合开始时才清除
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;
        await PowerCmd.Remove(this);
    }
}
