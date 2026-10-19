namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
///     【不灭之炎】（挂在敌人身上的负面能力）：
///     该敌人对玩家造成伤害（<b>包括被格挡的部分</b>）时，失去等量的生命，永不消退。
/// </summary>
[RegisterPower]
public sealed class ImmortalFlameBrandPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer != Owner)
            return;
        if (!target.IsPlayer)
            return;
        if (Owner is not { IsAlive: true } owner)
            return;

        // 包括被格挡的部分 —— 用「格挡 + 未格挡」而不是只看实际掉血。
        var total = result.BlockedDamage + result.UnblockedDamage;
        if (total <= 0)
            return;

        Flash();

        await CreatureCmd.Damage(
            choiceContext,
            owner,
            total,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null,
            null);
    }
}
