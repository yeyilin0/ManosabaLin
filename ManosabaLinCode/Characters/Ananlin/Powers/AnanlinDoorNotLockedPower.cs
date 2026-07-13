using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinDoorNotLockedPower : ManosabaPowerTemplate
{
    private bool _isUnblockedDamagePeaceLoss;

    [SavedProperty] public bool Used { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (Used) return false;
        if (!_isUnblockedDamagePeaceLoss) return false;
        if (Owner.Player is null || target != Owner) return false;
        if (canonicalPower is not AnanlinPeaceOfMindPower || amount >= 0) return false;

        Used = true;
        modifiedAmount = amount < -1m ? -1m : amount;
        Flash();
        return modifiedAmount != amount;
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (Used && power is AnanlinPeaceOfMindPower && power.Owner == Owner && amount < 0)
            await PowerCmd.Remove(this);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || Used) return;

        await PowerCmd.Apply<SilentPower>(choiceContext, Owner, 1, Owner, null);
        await PowerCmd.Remove(this);
    }

    internal void AllowNextPeaceLossFromUnblockedDamage()
    {
        _isUnblockedDamagePeaceLoss = true;
    }

    internal void ClearUnblockedDamagePeaceLoss()
    {
        _isUnblockedDamagePeaceLoss = false;
    }
}
