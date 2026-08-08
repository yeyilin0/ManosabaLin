using ManosabaLin.Characters.Yalisalin.Capabilities;
using ManosabaLin.Characters.Hiro.Cards;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Yalisalin.Components;

public static class YalisalinFireComponentRules
{
    public static IEnumerable<CardModel> AllCombatCards(Player player)
    {
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
        {
            foreach (var card in pileType.GetPile(player).Cards)
                yield return card;
        }
    }

    public static IEnumerable<CardModel> CardsWithoutFireComponent(
        Player player,
        Func<CardModel, bool>? filter = null)
    {
        return AllCombatCards(player)
            .Where(static card => !SamePlaceTruth.IsSelectionLocked(card))
            .Where(static card => !HasFireComponent(card))
            .Where(card => filter?.Invoke(card) ?? true);
    }

    public static bool HasFireComponent(CardModel? card)
    {
        return card != null && card.TryGetCapability<YalisalinFireComponentCapability>(out _);
    }

    public static bool TryAddFireComponent(CardModel? card)
    {
        if (card == null)
            return false;

        if (SamePlaceTruth.IsSelectionLocked(card))
            return false;

        if (HasFireComponent(card))
            return false;

        card.GetOrCreateCapability<YalisalinFireComponentCapability>();
        return true;
    }

    public static async Task<CardModel?> SelectHandCardWithoutFireComponent(
        PlayerChoiceContext choiceContext,
        Player owner,
        LocString prompt,
        CardModel source)
    {
        var candidates = PileType.Hand.GetPile(owner).Cards
            .Where(card => card != source)
            .Where(static card => !SamePlaceTruth.IsSelectionLocked(card))
            .Where(static card => !HasFireComponent(card))
            .ToArray();

        if (candidates.Length == 0)
            return null;

        if (candidates.Length == 1)
            return candidates[0];

        return (await CardSelectCmd.FromHand(
            choiceContext,
            owner,
            new CardSelectorPrefs(prompt, 1),
            card => candidates.Contains(card),
            source)).FirstOrDefault();
    }

    public static async Task<CardModel?> SelectDiscardCardWithoutFireComponent(
        PlayerChoiceContext choiceContext,
        Player owner,
        LocString prompt)
    {
        var candidates = PileType.Discard.GetPile(owner).Cards
            .Where(static card => !SamePlaceTruth.IsSelectionLocked(card))
            .Where(static card => !HasFireComponent(card))
            .ToArray();

        if (candidates.Length == 0)
            return null;

        if (candidates.Length == 1)
            return candidates[0];

        return (await CardSelectCmd.FromCombatPile(
            choiceContext,
            PileType.Discard.GetPile(owner),
            owner,
            new CardSelectorPrefs(prompt, 1),
            card => candidates.Contains(card))).FirstOrDefault();
    }

    public static CardModel? RandomCardWithoutFireComponent(Player owner, Func<CardModel, bool>? filter = null)
    {
        var candidates = CardsWithoutFireComponent(owner, filter).ToArray();
        return candidates.Length == 0
            ? null
            : owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
    }

    public static CardModel? RandomYalisalinCard(Player owner, CardType? requiredType = null)
    {
        if (owner.Creature.CombatState is not { } combatState)
            return null;

        var pool = ModelDb.CardPool<YalisalinCardPool>();
        var candidates = pool
            .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
            .Where(card => requiredType is null || card.Type == requiredType)
            .Where(card => card.Rarity is not CardRarity.Basic and not CardRarity.Token and not CardRarity.Status and not CardRarity.Curse)
            .Where(static card => card.CanBeGeneratedInCombat)
            .ToArray();

        var canonical = candidates.Length == 0
            ? null
            : owner.RunState.Rng.CombatCardGeneration.NextItem(candidates);

        return canonical is null ? null : combatState.CreateCard(canonical, owner);
    }

    public static void CopyUpgradeLevel(CardModel source, CardModel target)
    {
        for (var i = 0; i < source.CurrentUpgradeLevel; i++)
            CardCmd.Upgrade(target);
    }

    public static IEnumerable<IYalisalinFireComponentModifier> CardModifiers(CardModel card)
    {
        if (card is IYalisalinFireComponentModifier modifier)
            yield return modifier;

        if (card is IComponentsCardModel componentsCard)
        {
            foreach (var component in componentsCard.Components.OfType<IYalisalinFireComponentModifier>())
                yield return component;
        }

        foreach (var capability in card.Capabilities().All.OfType<IYalisalinFireComponentModifier>())
            yield return capability;
    }
}
