using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
///     被缚的普罗米修斯（本回合）：你的火色消耗全部视为同色，不触发连续；每次消耗对该敌人造成伤害。
///     升级版把颜色锁定为打出时消耗的那一格，之后每次消耗都按这个颜色结算消耗效果。
///     回合结束时移除。
/// </summary>
[RegisterPower]
public sealed class BoundPrometheusPower : ManosabaPowerTemplate
{
    public const int DamagePerConsume = 6;

    /// <summary>非空时，本回合所有消耗都按这个颜色结算。</summary>
    public YalisalinFireColor? LockedColor { get; set; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public async Task OnFireColorConsumed(PlayerChoiceContext choiceContext, Creature target)
    {
        if (!target.IsAlive)
            return;

        Flash();
        await CreatureCmd.Damage(choiceContext, target, DamagePerConsume, ValueProp.Unpowered, Owner, null, null);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }
}
