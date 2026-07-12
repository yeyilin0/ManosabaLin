using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace ManosabaLin.Characters.Sherrylin;

internal static class CaseFilePileHelper
{
    public static async Task<CardPileAddResult?> AddToCaseFilePile(
        CardModel card,
        Player player,
        CardPilePosition position = CardPilePosition.Top,
        PlayerChoiceContext? choiceContext = null)
    {
        if (ShouldBlockUnique(card, player))
        {
            if (card.Pile == null)
                card.RemoveFromState();
            return null;
        }

        var result = await CardPileCmd.Add(card, MainFile.CaseFilePile, position);

        if (choiceContext != null && IsBasicEmotion(card))
        {
            var emotionPower = player.Creature.GetPower<EmotionPower>();
            if (emotionPower != null)
                await emotionPower.AfterBasicEmotionAddedToCaseFile(choiceContext);
        }

        return result;
    }

    public static async Task<CardPileAddResult?> MoveToCombatHand(
        CardModel caseFileCard,
        Player player,
        Player? creator = null,
        CardPilePosition position = CardPilePosition.Top)
    {
        var combatState = player.Creature.CombatState;
        if (combatState == null) return null;

        if (combatState.ContainsCard(caseFileCard))
            return await CardPileCmd.Add(caseFileCard, PileType.Hand, position);

        var combatCard = combatState.CloneCard(caseFileCard);
        caseFileCard.RemoveFromState();
        return await CardPileCmd.AddGeneratedCardToCombat(combatCard, PileType.Hand, creator ?? player, position);
    }

    public static void Remove(CardModel caseFileCard)
    {
        caseFileCard.RemoveFromState();
    }

    private static bool ShouldBlockUnique(CardModel card, Player player)
    {
        if (!card.HasComponent<UniqueComponent>()) return false;

        var caseFilePile = MainFile.CaseFilePile.GetPile(player);
        return caseFilePile.Cards.Any(existing =>
            !ReferenceEquals(existing, card) && existing.Id.Entry == card.Id.Entry);
    }

    private static bool IsBasicEmotion(CardModel card)
    {
        return card is EmotionAnger or EmotionDisgust or EmotionSadness
            or EmotionFear or EmotionJoy or EmotionSurprise;
    }
}
