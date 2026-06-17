using ManosabaLin.Characters.Sherrylin.Cards.Emotions;

namespace ManosabaLin.Characters.Sherrylin;

/// <summary>
/// 情绪合成配方表。
/// </summary>
public static class EmotionFusionRecipeRegistry
{
    public static readonly IReadOnlyList<EmotionFusionRecipe> All =
    [
        // 2卡合成
        new("怅然", typeof(EmotionMelancholy),
            (cs, p) => cs.CreateCard<EmotionMelancholy>(p),
            typeof(EmotionJoy), typeof(EmotionSadness)),

        new("恼惧", typeof(EmotionIrritatedFear),
            (cs, p) => cs.CreateCard<EmotionIrritatedFear>(p),
            typeof(EmotionAnger), typeof(EmotionFear)),

        new("凄惶", typeof(EmotionDesolate),
            (cs, p) => cs.CreateCard<EmotionDesolate>(p),
            typeof(EmotionSadness), typeof(EmotionFear)),

        new("骇厌", typeof(EmotionHorrorDisgust),
            (cs, p) => cs.CreateCard<EmotionHorrorDisgust>(p),
            typeof(EmotionDisgust), typeof(EmotionSurprise)),

        new("雀跃", typeof(EmotionElation),
            (cs, p) => cs.CreateCard<EmotionElation>(p),
            typeof(EmotionJoy), typeof(EmotionSurprise)),

        // 8卡合成
        new("友谊", typeof(EmotionFriendship),
            (cs, p) => cs.CreateCard<EmotionFriendship>(p),
            typeof(EmotionJoy), typeof(EmotionSadness), typeof(EmotionAnger), typeof(EmotionFear),
            typeof(EmotionDisgust), typeof(EmotionSurprise), typeof(EmotionCuriosity), typeof(EmotionHelplessness)),
    ];

    /// <summary>
    /// 根据选中的卡查找匹配的配方
    /// </summary>
    public static EmotionFusionRecipe? FindRecipe(IReadOnlyList<CardModel> selectedCards)
    {
        return All.FirstOrDefault(recipe => recipe.CanCraft(selectedCards));
    }
}
