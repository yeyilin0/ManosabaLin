using ManosabaLin.Characters.Ananlin.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinPeaceOfMindPower : ManosabaPowerTemplate
{
    internal const int MaxStacks = 3;
    private const int MaxTurnsEndedWithPeace = 3;

    [SavedProperty] public int TurnsEndedWithPeace { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

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
            ClampStacks();
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

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.GetPower<AnanlinIsolatedPower>() is not null) return;
        if (!ShouldEnhance(cardPlay.Card)) return;

        var amount = Amount;
        if (amount <= 0) return;

        switch (cardPlay.Card.Type)
        {
            case CardType.Attack:
                if (cardPlay.Target is { IsAlive: true } target)
                {
                    Flash();
                    await CreatureCmd.Damage(
                        choiceContext,
                        target,
                        amount,
                        ValueProp.Unpowered | ValueProp.Move,
                        cardPlay.Card,
                        cardPlay);
                }

                break;
            case CardType.Skill:
                Flash();
                await CreatureCmd.GainBlock(Owner, amount, ValueProp.Move, cardPlay);
                break;
            case CardType.Power:
                Flash();
                await PowerCmd.Apply<SilentPower>(choiceContext, Owner, amount, Owner, cardPlay.Card);
                break;
        }
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (Amount <= 0) return;

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

    private static bool ShouldEnhance(CardModel card)
    {
        return card.Owner?.Creature is not null
            && card.Pool.Id == ModelDb.GetId(typeof(AnanlinCardPool))
            && card is not IAnanlinPeaceOfMindSpecialCard
            && card.Type is CardType.Attack or CardType.Skill or CardType.Power;
    }
}

public interface IAnanlinPeaceOfMindSpecialCard
{
}
