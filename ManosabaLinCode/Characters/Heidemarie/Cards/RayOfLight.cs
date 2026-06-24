using ManosabaLin.Characters.Common.Components;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class RayOfLight() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
    [
        new RestComponent()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var energyGain = DynamicVars.Energy.BaseValue;
        if (ShouldGainLinkCleanupBonus(cardPlay))
            energyGain += DynamicVars.Energy.BaseValue;

        await PlayerCmd.GainEnergy(energyGain, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
    }

    private bool ShouldGainLinkCleanupBonus(CardPlay cardPlay)
    {
        return IsUpgraded
            && cardPlay.IsAutoPlay
            && ((IComponentsCardModel)this).GetComponent<LinkComponent>() != null
            && LinkDiscardContext.IsActiveFor(this);
    }
}
