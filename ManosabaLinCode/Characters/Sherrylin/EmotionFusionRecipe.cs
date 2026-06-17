using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Sherrylin;

public sealed class EmotionFusionRecipe
{
    public string Name { get; }
    public Type ResultType { get; }
    public Func<ICombatState, Player, CardModel?> Factory { get; }
    public Type[] IngredientTypes { get; }
    public int RequiredCount => IngredientTypes.Length;

    public EmotionFusionRecipe(string name, Type resultType, Func<ICombatState, Player, CardModel?> factory, params Type[] ingredientTypes)
    {
        Name = name;
        ResultType = resultType;
        Factory = factory;
        IngredientTypes = ingredientTypes;
    }

    public bool CanCraft(IReadOnlyList<CardModel> selectedCards)
    {
        if (selectedCards.Count < RequiredCount) return false;

        var available = selectedCards.Select(c => c.Id).ToList();
        foreach (var ingredient in IngredientTypes)
        {
            var ingredientId = ModelDb.GetId(ingredient);
            var idx = available.IndexOf(ingredientId);
            if (idx < 0) return false;
            available.RemoveAt(idx);
        }
        return true;
    }
}