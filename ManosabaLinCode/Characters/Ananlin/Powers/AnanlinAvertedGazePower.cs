using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinAvertedGazePower : ManosabaPowerTemplate
{
    [SavedProperty] public bool UsedThisTurn { get; set; }
    [SavedProperty] public bool RepeatNextAttack { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
            UsedThisTurn = RepeatNextAttack;

        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            UsedThisTurn = false;

        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (power is not AnanlinPeaceOfMindPower || power.Owner != Owner || amount >= 0) return;
        if (UsedThisTurn || RepeatNextAttack) return;

        UsedThisTurn = true;
        RepeatNextAttack = true;
        Flash();
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (!RepeatNextAttack) return playCount;
        if (card.Owner?.Creature != Owner || card.Type != CardType.Attack) return playCount;

        return playCount + 1;
    }

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        RepeatNextAttack = false;
        Flash();
        return Task.CompletedTask;
    }
}
