using ManosabaLin.Characters.Ananlin.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinPeaceOfMindPower : ManosabaPowerTemplate
{
    internal const int MaxStacks = 3;
    private const int MaxTurnsEndedWithPeace = 2;

    [SavedProperty] public int TurnsEndedWithPeace { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not AnanlinPeaceOfMindPower) return false;
        if (target != Owner || amount <= 0) return false;

        modifiedAmount = Math.Min(amount, Math.Max(0, MaxStacks - Amount));
        return modifiedAmount != amount;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        ClampStacks();
        await Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (power == this)
        {
            ClampStacks();
            if (Amount < MaxStacks)
                TurnsEndedWithPeace = 0;
        }
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (result.UnblockedDamage <= 0) return;

        Flash();
        var doorNotLocked = Owner.GetPower<AnanlinDoorNotLockedPower>();
        doorNotLocked?.AllowNextPeaceLossFromUnblockedDamage();
        try
        {
            await PowerCmd.ModifyAmount(choiceContext, this, -Amount, Owner, cardSource);
        }
        finally
        {
            doorNotLocked?.ClearUnblockedDamagePeaceLoss();
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        // 每有 1 层安心，随机触发一次：获得 1 点能量 或 抽 1 张牌（二选一随机）。
        // 例如 3 层安心 → 随机触发 3 次。
        var stacks = (int)Amount;
        if (stacks <= 0) return;

        var rng = player.RunState.Rng.CombatCardGeneration;
        for (var i = 0; i < stacks; i++)
        {
            if (rng.NextFloat() < 0.5f)
            {
                await PlayerCmd.GainEnergy(1, player);
            }
            else
            {
                await CardPileCmd.Draw(choiceContext, 1, player);
            }
        }
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (Amount < MaxStacks)
        {
            TurnsEndedWithPeace = 0;
            return;
        }

        TurnsEndedWithPeace++;
        if (TurnsEndedWithPeace < MaxTurnsEndedWithPeace)
            return;

        Flash();
        await PowerCmd.ModifyAmount(choiceContext, this, -Amount, Owner, null);
        await PowerCmd.Apply<AnanlinIsolatedPower>(choiceContext, Owner, 1, Owner, null);
    }

    private void ClampStacks()
    {
        if (Amount > MaxStacks)
            SetAmount(MaxStacks, silent: true);
    }
}

public interface IAnanlinPeaceOfMindSpecialCard
{
}
