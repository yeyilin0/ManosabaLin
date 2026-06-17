using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class EmotionFusionPower : ManosabaActionTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override TargetType TargetType => TargetType.Self;
    public override bool DecrementAfterAct => false;

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = Owner.Player;
        if (player == null) return;

        var combatState = Owner.CombatState;
        if (combatState == null) return;

        var caseFileCards = MainFile.CaseFilePile.GetPile(player).Cards.ToList();
        if (caseFileCards.Count < 2) return;

        var availableRecipes = EmotionFusionRecipeRegistry.All
            .Where(r => r.CanCraft(caseFileCards))
            .ToList();

        if (availableRecipes.Count == 0) return;

        var previewCards = new List<CardModel>();
        var recipeMap = new Dictionary<CardModel, EmotionFusionRecipe>();

        foreach (var recipe in availableRecipes)
        {
            var card = recipe.Factory(combatState, player);
            if (card != null)
            {
                previewCards.Add(card);
                recipeMap[card] = recipe;
            }
        }

        if (previewCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(
            new LocString("powers", Id.Entry + ".selectionScreenPrompt"), 1);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, previewCards, player, prefs);
        var selectedCard = selected.FirstOrDefault();

        await Task.Yield();
        foreach (var card in previewCards)
        {
            if (!ReferenceEquals(card, selectedCard))
                card.RemoveFromState();
        }

        if (selectedCard == null || !recipeMap.TryGetValue(selectedCard, out var chosenRecipe))
            return;

        var remaining = caseFileCards.ToList();
        foreach (var ingredientType in chosenRecipe.IngredientTypes)
        {
            var ingredientId = ModelDb.GetId(ingredientType);
            var idx = remaining.FindIndex(c => c.Id == ingredientId);
            if (idx >= 0)
            {
                await CardPileCmd.Add(remaining[idx], PileType.Exhaust);
                remaining.RemoveAt(idx);
            }
        }

        var resultCard = chosenRecipe.Factory(combatState, player);
        if (resultCard != null)
            await CardPileCmd.Add(resultCard, MainFile.CaseFilePile, CardPilePosition.Top);

        await PowerCmd.Remove(this);
    }
}