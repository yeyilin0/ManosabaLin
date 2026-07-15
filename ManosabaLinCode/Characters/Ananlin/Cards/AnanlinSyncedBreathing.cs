using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinSyncedBreathing()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self),
        IAnanlinPeaceOfMindSpecialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("RequiredStreak", 2),
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var power = await PowerCmd.Apply<AnanlinSyncedBreathingPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
        power?.Arm(this, this.PeaceOfMindAmount() * DynamicVars.Cards.IntValue, DynamicVars["RequiredStreak"].IntValue);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
