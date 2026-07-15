using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinOppressiveSilencePower : ManosabaPowerTemplate
{
    private const int CycleLength = 4;

    [SavedProperty] public int NextCycleStep { get; set; } = 1;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override int DisplayAmount => NextCycleStep;

    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return Task.CompletedTask;

        NextCycleStep = 1;
        InvokeDisplayAmountChanged();
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

        if (power is not SilentPower || power.Owner != Owner || amount >= 0) return;
        if (Owner.Player is not { } ownerPlayer) return;

        Flash();
        var currentStep = NextCycleStep;
        NextCycleStep = currentStep == CycleLength ? 1 : currentStep + 1;
        InvokeDisplayAmountChanged();

        if (currentStep == 1)
            await PlayerCmd.GainEnergy(1, ownerPlayer);
        else
            await CardPileCmd.Draw(choiceContext, 1, ownerPlayer);
    }
}
