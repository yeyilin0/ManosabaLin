using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinOppressiveSilencePower : ManosabaPowerTemplate
{
    [SavedProperty] public int NextCycleStep { get; set; } = 1;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
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
        NextCycleStep = currentStep == 5 ? 1 : currentStep + 1;
        InvokeDisplayAmountChanged();

        var reward = Math.Max(1, (int)Amount);
        if (currentStep is 1 or 5)
            await PlayerCmd.GainEnergy(reward, ownerPlayer);
        else
            await CardPileCmd.Draw(choiceContext, reward, ownerPlayer);
    }
}
