using MegaCrit.Sts2.Core.Entities.Powers;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
///     龙之魔女（挂在你自己身上的本回合效果）：
///     本回合每当你造成伤害时获得 1 层【嫌疑】，且你免疫【嫌疑】造成的减力量；
///     回合结束时，把你的全部【嫌疑】改造成「等量 ×5」的【魔女化】，
///     并对所有敌人造成等同于被改造层数 ×2（升级 ×3）的伤害。
/// </summary>
[RegisterPower]
public sealed class DragonWitchPower : ManosabaPowerTemplate
{
    private const int SuspectToWitchificationRatio = 5;

    /// <summary>升级版：每层改造的伤害改为 3。</summary>
    public bool StrongConversion { get; set; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    /// <summary>
    ///     描述随「来源牌是否升级」切键（升级也是一种达成条件）。两侧通道都切，做法对齐「共犯」/
    ///     被缚的普罗米修斯。
    /// </summary>
    public override LocString Description =>
        new LocString("powers", StrongConversion ? $"{Id.Entry}.descriptionEnhanced" : $"{Id.Entry}.description");

    protected override string SmartDescriptionLocKey =>
        StrongConversion ? $"{Id.Entry}.smartDescriptionEnhanced" : $"{Id.Entry}.smartDescription";

    private int DamagePerWitchification => StrongConversion ? 3 : 2;

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
        if (result.BlockedDamage + result.UnblockedDamage <= 0)
            return;
        if (Owner is not { IsAlive: true } owner)
            return;

        await PowerCmd.Apply<SuspectPower>(choiceContext, owner, 1m, owner, null, true);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
            return;
        if (Owner is not { IsAlive: true } owner)
            return;

        var suspect = owner.GetPower<SuspectPower>();
        var stacks = suspect?.Amount ?? 0;

        if (stacks > 0)
        {
            // 转化优先：先把【嫌疑】整层拿走（避免凑满 12 层触发坏结局），再换算成【魔女化】。
            await PowerCmd.Remove(suspect!);

            var converted = stacks * SuspectToWitchificationRatio;
            await PowerCmd.Apply<WithPower>(
                choiceContext, owner, converted, owner, null, false);

            if (owner.CombatState is { } combatState)
            {
                var damage = converted * DamagePerWitchification;
                var enemies = combatState.HittableEnemies.Where(static enemy => enemy.IsAlive).ToList();
                foreach (var enemy in enemies)
                    await CreatureCmd.Damage(
                        choiceContext,
                        enemy,
                        damage,
                        ValueProp.Unpowered | ValueProp.Move,
                        owner,
                        null,
                        null);
            }
        }

        await PowerCmd.Remove(this);
    }
}
