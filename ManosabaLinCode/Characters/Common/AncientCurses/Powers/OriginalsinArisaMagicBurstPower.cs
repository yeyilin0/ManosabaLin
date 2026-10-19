namespace ManosabaLin.Characters.Common.AncientCurses.Powers;

/// <summary>
///     【负疚】：「本回合」当你给予【紫藤亚里沙的魔法】达到 10 层时立刻引爆
///     —— 对该敌人造成等量伤害、移除其身上全部【紫藤亚里沙的魔法】，并获得 1 点能量。
///     <para>
///         文案里的「本回合」跨过敌方回合 ⇒ 不在自己回合结束时清除，
///         而是在<b>下一个自己回合开始时</b>才清除。
///     </para>
/// </summary>
[RegisterPower]
public sealed class OriginalsinArisaMagicBurstPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is not YlsmPower) return;
        if (power.Amount < 10m) return;
        if (applier != Owner) return;
        if (power.Owner == Owner) return;
        if (!Owner.HasPower<YlsmPower>()) return;

        // 达到10层时立刻触发目标身上所有【紫藤亚里沙的魔法】：每层造成1点不可阻挡伤害
        var stacks = power.Amount;
        if (stacks > 0m)
            await CreatureCmd.Damage(
                choiceContext,
                power.Owner,
                stacks,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                cardSource,
                null);

        // 移除目标身上所有的【紫藤亚里沙的魔法】
        power.RemoveInternal();

        // 获得1点能量
        if (Owner.Player is { } burstOwner)
            await PlayerCmd.GainEnergy(1m, burstOwner);
    }

    // 本回合跨过敌方回合，下一个自己回合开始时才清除
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;
        await PowerCmd.Remove(this);
    }
}
