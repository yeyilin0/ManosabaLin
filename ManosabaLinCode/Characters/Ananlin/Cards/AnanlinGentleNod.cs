using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinGentleNod()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self),
        IAnanlinPeaceOfMindSpecialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
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
        var peace = this.PeaceOfMindAmount();
        if (peace > 0)
        {
            await this.PullMatchingCardsToHand(
                choiceContext,
                peace * DynamicVars.Cards.IntValue,
                card => AnanlinCardHelpers.IsPlayableCombatCard(card) && card.Type != CardType.Attack);
            await this.LosePeaceOfMind(choiceContext);
            this.Sketchbook()?.QueueNonAttackRepeatThisTurn();
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
