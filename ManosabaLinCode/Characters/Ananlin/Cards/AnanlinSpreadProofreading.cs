using ManosabaLin.Characters.Ananlin.Relics;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinSpreadProofreading() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (this.Sketchbook() is not { } sketchbook) return;

        var recordedPools = sketchbook.GetRecordedCardPools();
        var candidates = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card != this
                && card.Pool.Id == ModelDb.GetId(typeof(AnanlinCardPool))
                && card.Type is CardType.Attack or CardType.Skill or CardType.Power
                && card.Rarity is CardRarity.Common or CardRarity.Uncommon
                && AnansSketchbook.CanSketchbookGenerate(card)
                && recordedPools.Any(pool => pool.Id != card.Pool.Id))
            .ToList();
        if (candidates.Count == 0) return;

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1))).FirstOrDefault();
        if (selected is null) return;

        var newCard = sketchbook.RollHigherRarityCardFromOtherRecordedPool(selected);
        if (newCard is null)
        {
            await CardPileCmd.Draw(choiceContext, 1, Owner);
            return;
        }

        sketchbook.CopyVisibleAdditions(selected, newCard);
        if (IsUpgraded)
            newCard.EnergyCost.AddThisTurnOrUntilPlayed(-1, reduceOnly: true);

        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
