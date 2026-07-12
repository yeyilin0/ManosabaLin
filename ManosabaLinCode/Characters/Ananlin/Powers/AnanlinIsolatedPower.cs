using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Combat.HandSize;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinIsolatedPower : ManosabaPowerTemplate, IMaxHandSizeModifier
{
    private const int DrawPenalty = 1;
    private const int EnergyPenalty = 1;

    [SavedProperty] public bool HasPenalizedTurnStart { get; set; }

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

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

        modifiedAmount = 0;
        Flash();
        return true;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.GetPower<AnanlinPeaceOfMindPower>() is { } peace)
            await PowerCmd.ModifyAmount(
                new ThrowingPlayerChoiceContext(),
                peace,
                -peace.Amount,
                Owner,
                cardSource);
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner.Player) return count;
        return Math.Max(0m, count - DrawPenalty);
    }

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player) return;

        HasPenalizedTurnStart = true;
        Flash();
        await PlayerCmd.LoseEnergy(EnergyPenalty, player);
    }

    public int ModifyMaxHandSize(Player player, int currentMaxHandSize)
    {
        if (player != Owner.Player) return currentMaxHandSize;
        return Math.Max(1, currentMaxHandSize / 2);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (!HasPenalizedTurnStart) return;

        await PowerCmd.Remove(this);
    }
}
