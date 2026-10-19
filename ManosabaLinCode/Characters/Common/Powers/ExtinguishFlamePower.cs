using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Common.Powers;

/// <summary>
///     【灭火】：世界之火熄灭。
///     你与所有敌人造成的伤害减半；你的格挡不再于每回合开始时消失（原版「壁垒」同款规则）。
///     升级版额外在每回合开始时获得 15 点格挡。
/// </summary>
[RegisterPower]
public sealed class ExtinguishFlamePower : ManosabaPowerTemplate
{
    /// <summary>升级版每回合开始时获得的格挡（基础版为 0，不给格挡）。</summary>
    private const int UpgradedBlockPerTurn = 15;

    public override PowerType Type => PowerType.Buff;

    /// <summary>
    ///     只会有 1 个实例；<c>Amount</c> 仅作「已存在」标记（真正决定效果的是 <see cref="CardUpgraded" />），
    ///     所以用 <c>Single</c> —— 不显示层数数字。
    /// </summary>
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>由升级版【灭火】打出时置 true：每回合开始获得 15 点格挡，并切到「升级后」的描述。</summary>
    [SavedProperty] public bool CardUpgraded { get; set; }

    private int BlockPerTurn => CardUpgraded ? UpgradedBlockPerTurn : 0;

    /// <summary>
    ///     描述随「来源牌是否升级」切键 —— 做法对齐「怪力」<c>SuperStrengthPower</c> /
    ///     「刑枷加身」<c>XingJiaJiaShenPower</c>：power 悬浮读 <c>Description</c>，
    ///     smart 通道读 <c>SmartDescriptionLocKey</c>，两侧都切。
    /// </summary>
    public override LocString Description =>
        new LocString("powers", CardUpgraded ? $"{Id.Entry}.descriptionEnhanced" : $"{Id.Entry}.description");

    protected override string SmartDescriptionLocKey =>
        CardUpgraded ? $"{Id.Entry}.smartDescriptionEnhanced" : $"{Id.Entry}.smartDescription";

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Block)];

    /// <summary>
    ///     你打出去的伤害、以及敌人打出来的伤害都减半；无关的第三方（其他友方）不受影响。
    /// </summary>
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (props.HasFlag(ValueProp.Unpowered))
            return 1m;
        if (dealer is null)
            return 1m;
        if (dealer != Owner && !dealer.IsEnemy)
            return 1m;

        return 0.5m;
    }

    public override bool ShouldClearBlock(Creature creature)
    {
        return creature != Owner;
    }

    public override Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
    {
        if (ReferenceEquals(preventer, this))
            Flash();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature != Owner)
            return;

        if (Owner is not { IsAlive: true } owner)
            return;

        // 基础版不给格挡；升级版每回合开始获得 15 点。
        if (BlockPerTurn > 0)
            await CreatureCmd.GainBlock(owner, BlockPerTurn, ValueProp.Unpowered, null);
    }
}
