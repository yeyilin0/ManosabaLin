using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinThePrisonIsHome() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(0)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinPrisonIsHomePower>(),
        HoverTipFactory.FromPower<AnanlinCellPower>(),
        HoverTipFactory.FromPower<SilentPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var prison = await PowerCmd.Apply<AnanlinPrisonIsHomePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars.Energy.BaseValue,
            Owner.Creature,
            this);
        prison?.InitializeCurrentTurn(this.Sketchbook()?.AttacksPlayedThisTurn > 0);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
