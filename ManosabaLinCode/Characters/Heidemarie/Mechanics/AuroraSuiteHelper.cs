using ManosabaLin.Characters.Heidemarie.Cards;

namespace ManosabaLin.Characters.Heidemarie.Mechanics;

internal static class AuroraSuiteHelper
{
    private static readonly PileType[] ConsumableAuroraSwordPiles =
    [
        PileType.Hand,
        PileType.Draw,
        PileType.Discard
    ];

    public static async Task<int> ExhaustAuroraSwordsFromCombatPiles(
        PlayerChoiceContext choiceContext,
        Player owner)
    {
        var auroraSwords = ConsumableAuroraSwordPiles
            .SelectMany(pileType => pileType.GetPile(owner).Cards)
            .Where(card => card.Owner == owner && card is IAuroraSwordCard)
            .ToArray();

        var exhausted = 0;
        foreach (var card in auroraSwords)
        {
            if (!IsConsumableAuroraSword(card, owner))
                continue;

            await CardCmd.Exhaust(choiceContext, card);
            if (card.Pile?.Type == PileType.Exhaust)
                exhausted++;
        }

        return exhausted;
    }

    public static async Task<int> AddCondensedAurorasToHand(
        Player owner,
        int count,
        AbstractModel? source = null)
    {
        if (count <= 0)
            return 0;

        var combatState = owner.Creature.CombatState;
        if (combatState == null)
            return 0;

        var generated = Enumerable.Range(0, count)
            .Select(_ => combatState.CreateCard<CondensedAurora>(owner))
            .ToArray();

        var results = await CardPileCmd.AddGeneratedCardsToCombat(
            generated,
            PileType.Hand,
            owner,
            CardPilePosition.Bottom);

        return results.Count(static result => result.success);
    }

    private static bool IsConsumableAuroraSword(CardModel card, Player owner)
    {
        return card.Owner == owner
            && card is IAuroraSwordCard
            && card.Pile?.Type is PileType.Hand or PileType.Draw or PileType.Discard;
    }
}
