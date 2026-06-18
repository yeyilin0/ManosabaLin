using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ManosabaLin.Characters.Sherrylin;

internal static class CaseFilePileHelper
{
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
}
