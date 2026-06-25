using ManosabaLin.Characters.Heidemarie.Powers;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class ShatteredAurora()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(ShatteredAuroraPower.DiscardThresholdKey, ShatteredAuroraPower.BaseDiscardThreshold),
        new EnergyVar(ShatteredAuroraPower.EnergyGainKey, ShatteredAuroraPower.BaseEnergyGain)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<ShatteredAuroraPower>(); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var threshold = DynamicVars[ShatteredAuroraPower.DiscardThresholdKey].IntValue;
        var energyGain = DynamicVars[ShatteredAuroraPower.EnergyGainKey].BaseValue;

        var power = Owner.Creature.GetPower<ShatteredAuroraPower>();
        if (power == null)
        {
            power = await PowerCmd.Apply<ShatteredAuroraPower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this,
                false);
        }

        power?.Configure(threshold, energyGain);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[ShatteredAuroraPower.DiscardThresholdKey].UpgradeValueBy(-1m);
    }
}
