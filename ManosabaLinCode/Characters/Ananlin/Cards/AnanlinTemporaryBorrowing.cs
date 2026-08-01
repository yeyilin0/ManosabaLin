namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinTemporaryBorrowing() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (this.Sketchbook() is not { } sketchbook) return;

        await this.AddBlankPageToHand(IsUpgraded);

        var card = sketchbook.RollCombatCardFromRecordedPools();
        if (card is null) return;

        if (IsUpgraded)
            CardCmd.Upgrade(card);

        var isLowCost = !card.EnergyCost.CostsX && card.EnergyCost.GetWithModifiers(CostModifiers.None) <= 1;
        if (isLowCost)
        {
            card.ExhaustOnNextPlay = true;

            var addResult = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
            if (addResult.success && AnanlinCardHelpers.HasValidEffectTarget(card, CombatState))
                await CardCmd.AutoPlay(choiceContext, card, AnanlinCardHelpers.PickValidEffectTarget(card, CombatState));

            return;
        }

        card.AddKeyword(CardKeyword.Exhaust);

        if (!sketchbook.HasFullRecordedPools)
        {
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
            return;
        }

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Exhaust, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
