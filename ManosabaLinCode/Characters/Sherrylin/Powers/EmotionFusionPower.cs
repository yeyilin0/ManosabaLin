using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class EmotionFusionPower : ManosabaActionTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override TargetType TargetType => TargetType.Self;
    public override bool DecrementAfterAct => true;

    private static readonly Dictionary<string, Func<ICombatState, Player, CardModel?>> FusionRecipes2 = new()
    {
        ["EmotionJoy+EmotionSadness"] = (cs, p) => cs.CreateCard<EmotionMelancholy>(p),
        ["EmotionAnger+EmotionFear"] = (cs, p) => cs.CreateCard<EmotionIrritatedFear>(p),
        ["EmotionSadness+EmotionFear"] = (cs, p) => cs.CreateCard<EmotionDesolate>(p),
        ["EmotionDisgust+EmotionSurprise"] = (cs, p) => cs.CreateCard<EmotionHorrorDisgust>(p),
        ["EmotionJoy+EmotionSurprise"] = (cs, p) => cs.CreateCard<EmotionElation>(p),
    };

    private static readonly HashSet<string> FriendshipIngredients =
    [
        "EmotionJoy", "EmotionSadness", "EmotionAnger", "EmotionFear",
        "EmotionDisgust", "EmotionSurprise", "EmotionCuriosity", "EmotionHelplessness"
    ];

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = Owner.Player;
        if (player == null) return;

        var caseFilePile = MainFile.CaseFilePile.GetPile(player);
        if (caseFilePile.Cards.Count < 2) return;

        var prefs = new CardSelectorPrefs(
            new LocString("cards", "MANOSABA_LIN_POWER_EMOTION_FUSION_POWER.selectionScreenPrompt"), 2);
        var selected = await CardSelectCmd.FromCombatPile(choiceContext, caseFilePile, player, prefs);
        var selectedList = selected.ToList();
        if (selectedList.Count < 2) return;

        var combatState = Owner.CombatState;
        if (combatState == null) return;

        var typeNames = selectedList.Select(c => c.GetType().Name).ToHashSet();

        // 8卡合成友谊
        if (selectedList.Count >= 8 && FriendshipIngredients.IsSubsetOf(typeNames))
        {
            foreach (var card in selectedList)
                await CardPileCmd.RemoveFromCombat(card);

            var result = combatState.CreateCard<EmotionFriendship>(player);
            if (result != null)
                await CardPileCmd.Add(result, MainFile.CaseFilePile, CardPilePosition.Top);
            return;
        }

        // 2卡合成
        if (selectedList.Count == 2)
        {
            var card1Type = selectedList[0].GetType().Name;
            var card2Type = selectedList[1].GetType().Name;
            var key1 = $"{card1Type}+{card2Type}";
            var key2 = $"{card2Type}+{card1Type}";

            Func<ICombatState, Player, CardModel?>? recipe = null;
            if (!FusionRecipes2.TryGetValue(key1, out recipe))
                FusionRecipes2.TryGetValue(key2, out recipe);

            if (recipe == null) return;

            await CardPileCmd.RemoveFromCombat(selectedList[0]);
            await CardPileCmd.RemoveFromCombat(selectedList[1]);

            var resultCard = recipe(combatState, player);
            if (resultCard != null)
                await CardPileCmd.Add(resultCard, MainFile.CaseFilePile, CardPilePosition.Top);
        }
    }
}
