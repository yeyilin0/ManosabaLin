using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Ema.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Characters.Sherrylin.Cards;
using ManosabaLin.Characters.Yalisalin.Cards;
using ManosabaLin.Characters.Yalisalin.Relics;
using STS2RitsuLib.Combat.AttackHits;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
///     代你燎原（本回合）：被指定的队友对敌人造成伤害时，按「本次伤害的命中段数」
///     给该敌人增加等量火色。回合结束（玩家阵营回合）时移除。
/// </summary>
/// <remarks>
///     本能力挂在<b>队友</b>身上（这样界面上是「他获得了一个增益」、判定也只需比对
///     <see cref="PowerModel.Owner" />），火色量表却属于<b>施加者</b>的亚里沙发夹，
///     所以额外记一个 <see cref="Caster" />。
/// </remarks>
[RegisterPower]
public sealed class StokeForYouPower : ManosabaPowerTemplate, IAttackHitHookListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldReceiveCombatHooks => true;

    /// <summary>施加者（亚里沙发夹的持有者）。非存档：与 <c>BoundPrometheusPower.LockedColor</c> 同口径。</summary>
    public Player? Caster { get; set; }

    public async Task AfterAttackHit(AttackHitContext context)
    {
        // 一次攻击只结算一次，取第 0 段时按总段数给量。
        if (context.HitIndex != 0)
            return;
        if (context.Dealer != Owner)
            return;
        if (!context.DamageProps.IsPoweredAttack())
            return;
        if (Caster is not { } caster)
            return;
        if (!YalisalinFireColorSystem.TryGetHairpin(caster, out var hairpin))
            return;

        var amount = Math.Max(1, (int)context.TotalHitCount);
        if (amount <= 0)
            return;

        foreach (var target in context.Targets
                     .Where(target => target.IsAlive && target.Side != Owner.Side)
                     .Distinct())
            await hairpin.GiveFireColor(context.ChoiceContext, target, amount, null);
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

/// <summary>
///     薪火相传（本场战斗）：被指定的队友每次用指向敌人的技能牌时，
///     给那个敌人 1 格火色，并按「予燎」结算超出的格数。
/// </summary>
[RegisterPower]
public sealed class TorchPassingPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldReceiveCombatHooks => true;

    /// <summary>施加者（亚里沙发夹的持有者）。</summary>
    public Player? Caster { get; set; }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Type != CardType.Skill)
            return;
        if (cardPlay.Target is not { IsEnemy: true } target)
            return;
        if (Owner.Player is not { } ally || cardPlay.Player != ally)
            return;
        if (Caster is not { } caster)
            return;
        if (!YalisalinFireColorSystem.TryGetHairpin(caster, out var hairpin))
            return;

        // 予燎：超出 6 格的部分按对应格位颜色立即结算消耗效果。
        await hairpin.GiveFireColor(choiceContext, target, 1, null, overflowTriggersConsume: true);
    }
}

/// <summary>
///     共罪：每当有队友的【嫌疑】达到 <see cref="SuspectThreshold" />（引擎的坏结局阈值）时，
///     移除他刚吃到的坏结局；他的嫌疑减半、另一半归你；你与他各得 1 层【魔女仪式】，
///     并消耗 1 层本能力。升级后再清空他所有的「减力量」。
/// </summary>
/// <remarks>
///     触发点用 <see cref="AbstractModel.AfterPowerAmountChanged" />（该钩子广播给战斗中所有监听者），
///     并且<b>当场就把嫌疑减半</b>：本能力与 <see cref="SuspectPower" /> 自身处理器的先后顺序不保证，
///     先减半 ⇒ 后跑的人看到的是 &lt; 12，坏结局根本不会发；若它先跑，坏结局已经到手，
///     这里再用「移出战斗区」把它拿走（那四张坏结局卡被弃/被消耗都会回手，只能这样移除）。
/// </remarks>
[RegisterPower]
public sealed class SharedGuiltPower : ManosabaPowerTemplate
{
    public const int SuspectThreshold = 12;

    private bool _resolving;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>升级：救人之后顺便清空他所有的「减力量」。</summary>
    public bool ClearsStrengthDown { get; set; }

    /// <summary>
    ///     描述随「来源牌是否升级」切键（升级也是一种达成条件）。两侧通道都切，做法对齐「共犯」/
    ///     被缚的普罗米修斯。
    /// </summary>
    public override LocString Description =>
        new LocString("powers",
            ClearsStrengthDown ? $"{Id.Entry}.descriptionEnhanced" : $"{Id.Entry}.description");

    protected override string SmartDescriptionLocKey =>
        ClearsStrengthDown ? $"{Id.Entry}.smartDescriptionEnhanced" : $"{Id.Entry}.smartDescription";

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (_resolving)
            return;
        if (amount <= 0)
            return;
        if (power is not SuspectPower suspect)
            return;
        if (suspect.Owner == Owner)
            return;
        if (!suspect.Owner.IsPlayer || !suspect.Owner.IsAlive)
            return;
        if (suspect.Owner.Side != Owner.Side)
            return;
        if (suspect.Amount < SuspectThreshold)
            return;
        if (Amount <= 0)
            return;

        _resolving = true;
        try
        {
            var ally = suspect.Owner;

            // 1) 先把他已经吃到的坏结局拿走。
            await RemoveBadEndingCurse(ally);

            // 2) 嫌疑减半，另一半归你。
            var total = (int)suspect.Amount;
            var half = total / 2;
            var taken = total - half;
            if (taken > 0)
            {
                await PowerCmd.ModifyAmount(choiceContext, suspect, -taken, Owner, null, false);
                await PowerCmd.Apply<SuspectPower>(choiceContext, Owner, taken, Owner, null, false);
            }

            // 3) 你与他各得 1 层【魔女仪式】。
            await PowerCmd.Apply<RitualCeremonyPower>(choiceContext, Owner, 1m, Owner, null, false);
            await PowerCmd.Apply<RitualCeremonyPower>(choiceContext, ally, 1m, Owner, null, false);

            // 4) 升级：清空他所有的「减力量」。
            if (ClearsStrengthDown)
                await ClearStrengthDown(choiceContext, ally);

            // 5) 消耗 1 层本能力。
            if (Amount > 0)
                await PowerCmd.ModifyAmount(choiceContext, this, -1, Owner, null, false);
            if (Amount <= 0)
                await PowerCmd.Remove(this);
        }
        finally
        {
            _resolving = false;
        }
    }

    /// <summary>
    ///     把队友手牌里的「坏结局」诅咒移出战斗区。
    ///     四张坏结局卡被弃掉/被消耗都会自己回手，所以只能走 <see cref="CardPileCmd.RemoveFromCombat" />。
    /// </summary>
    private static async Task RemoveBadEndingCurse(Creature ally)
    {
        if (ally.Player is not { } player)
            return;

        var curses = PileType.Hand.GetPile(player).Cards
            .Where(static card => card is HiroBadEnding
                or EmaForgottenOne
                or AnanlinBadEnding
                or Sherrybadending
                or YalisalinBadEnding)
            .ToArray();

        foreach (var curse in curses)
            await CardPileCmd.RemoveFromCombat(curse);
    }

    /// <summary>清空一个生物身上所有的「减力量」：临时减力量 + 负向的原版力量。</summary>
    private static async Task ClearStrengthDown(PlayerChoiceContext choiceContext, Creature ally)
    {
        var tempDown = ally.GetPower<TempStrengthDown>();
        if (tempDown is { Amount: > 0 })
            await PowerCmd.ModifyAmount(choiceContext, tempDown, -tempDown.Amount, ally, null, false);

        var strength = ally.GetPower<StrengthPower>();
        if (strength is { Amount: < 0 })
            await PowerCmd.ModifyAmount(choiceContext, strength, -strength.Amount, ally, null, false);
    }
}
