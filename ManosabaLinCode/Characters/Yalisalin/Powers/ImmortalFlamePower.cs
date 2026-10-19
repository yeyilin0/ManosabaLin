using MegaCrit.Sts2.Core.Entities.Powers;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
///     不灭之炎的豁免标记（挂在你自己身上）：
///     你的生命低于阈值百分比时，受到攻击就会让攻击者获得 1 层【不灭之炎】。
///     层数即阈值百分比（15 / 升级 30）。
/// </summary>
[RegisterPower]
public sealed class ImmortalFlamePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<ImmortalFlameBrandPower>()];

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner)
            return;
        if (dealer is null || !dealer.IsEnemy || dealer.IsDead)
            return;
        if (result.BlockedDamage + result.UnblockedDamage <= 0)
            return;
        if (Owner is not { IsAlive: true } owner)
            return;
        if (owner.MaxHp <= 0m)
            return;

        // 「低于 15%」——用严格小于，刚好处在阈值上不触发。
        var threshold = Amount / 100m;
        if (owner.CurrentHp >= owner.MaxHp * threshold)
            return;

        await PowerCmd.Apply<ImmortalFlameBrandPower>(
            choiceContext, dealer, 1m, owner, null, false);
    }
}
