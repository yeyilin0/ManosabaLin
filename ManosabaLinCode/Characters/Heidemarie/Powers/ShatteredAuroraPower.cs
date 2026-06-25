using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class ShatteredAuroraPower : ManosabaPowerTemplate
{
    public const string DiscardThresholdKey = "DiscardThreshold";
    public const string EnergyGainKey = "EnergyGain";
    public const int BaseDiscardThreshold = 2;
    public const int BaseEnergyGain = 1;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public int DiscardThreshold { get; private set; } = BaseDiscardThreshold;
    public decimal EnergyGain { get; private set; } = BaseEnergyGain;
    public int DiscardedAuroraSwordRemainder { get; private set; }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(DiscardThresholdKey, BaseDiscardThreshold),
        new EnergyVar(EnergyGainKey, BaseEnergyGain)
    ];

    public void Configure(int discardThreshold, decimal energyGain)
    {
        if (discardThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(discardThreshold), discardThreshold, null);
        if (energyGain <= 0)
            throw new ArgumentOutOfRangeException(nameof(energyGain), energyGain, null);

        DiscardThreshold = Math.Min(DiscardThreshold, discardThreshold);
        EnergyGain = Math.Max(EnergyGain, energyGain);
        DynamicVars[DiscardThresholdKey].BaseValue = DiscardThreshold;
        DynamicVars[EnergyGainKey].BaseValue = EnergyGain;
    }

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card.Owner != Owner.Player || card is not IAuroraSwordCard)
            return;

        DiscardedAuroraSwordRemainder++;
        var triggers = DiscardedAuroraSwordRemainder / DiscardThreshold;
        DiscardedAuroraSwordRemainder %= DiscardThreshold;
        if (triggers <= 0)
            return;

        Flash();
        await PlayerCmd.GainEnergy(EnergyGain * triggers, Owner.Player);
    }
}
