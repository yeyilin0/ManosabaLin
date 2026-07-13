using ManosabaLin.Characters.Ananlin.Relics;
using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

internal static class AnanlinCardHelpers
{
    internal static AnansSketchbook? Sketchbook(this CardModel card)
    {
        return card.Owner.Relics.OfType<AnansSketchbook>().FirstOrDefault();
    }

    internal static async Task AddBlankPageToHand(this CardModel source, bool upgraded)
    {
        var blankPage = source.CombatState.CreateCard<BlankPage>(source.Owner);
        if (upgraded)
            CardCmd.Upgrade(blankPage);

        await CardPileCmd.AddGeneratedCardToCombat(blankPage, PileType.Hand, source.Owner);
    }

    internal static async Task AddBlankPageToDrawPile(this CardModel source, bool upgraded)
    {
        var blankPage = source.CombatState.CreateCard<BlankPage>(source.Owner);
        if (upgraded)
            CardCmd.Upgrade(blankPage);

        await CardPileCmd.AddGeneratedCardToCombat(blankPage, PileType.Draw, source.Owner, CardPilePosition.Random);
    }

    internal static async Task AddMarginPageToHand(this CardModel source, bool upgraded)
    {
        var marginPage = source.CombatState.CreateCard<MarginPage>(source.Owner);
        if (upgraded)
            CardCmd.Upgrade(marginPage);

        await CardPileCmd.AddGeneratedCardToCombat(marginPage, PileType.Hand, source.Owner);
    }

    internal static int PeaceOfMindAmount(this CardModel card)
    {
        return Math.Max(0, (int)(card.Owner.Creature.GetPower<AnanlinPeaceOfMindPower>()?.Amount ?? 0));
    }

    internal static bool HasLostPeaceOfMindThisTurn(this CardModel card)
    {
        return card.Sketchbook()?.PeaceLostThisTurn == true;
    }

    internal static async Task GainPeaceOfMind(
        this CardModel card,
        PlayerChoiceContext choiceContext,
        int amount = 1)
    {
        if (amount <= 0) return;

        await PowerCmd.Apply<AnanlinPeaceOfMindPower>(
            choiceContext,
            card.Owner.Creature,
            amount,
            card.Owner.Creature,
            card);
    }

    internal static Task AddSilence(
        this CardModel card,
        PlayerChoiceContext choiceContext,
        int amount)
    {
        return card.Sketchbook() is { } sketchbook
            ? sketchbook.AddSilence(choiceContext, amount, card)
            : PowerCmd.Apply<SilentPower>(
                choiceContext,
                card.Owner.Creature,
                amount,
                card.Owner.Creature,
                card);
    }

    internal static async Task<int> LosePeaceOfMind(
        this CardModel card,
        PlayerChoiceContext choiceContext,
        int amount = 1)
    {
        if (amount <= 0) return 0;
        var peace = card.Owner.Creature.GetPower<AnanlinPeaceOfMindPower>();
        if (peace is null || peace.Amount <= 0) return 0;

        var lost = Math.Min(amount, (int)peace.Amount);
        await PowerCmd.ModifyAmount(choiceContext, peace, -lost, card.Owner.Creature, card);
        return lost;
    }

    internal static async Task<int> PullMatchingCardsToHand(
        this CardModel source,
        PlayerChoiceContext choiceContext,
        int count,
        Func<CardModel, bool> predicate)
    {
        if (count <= 0) return 0;

        var added = 0;
        for (var i = 0; i < count; i++)
        {
            var card = FindMatchingCard(source.Owner, predicate);
            if (card is null) break;

            await CardPileCmd.Add(card, PileType.Hand);
            added++;
        }

        return added;
    }

    internal static bool IsPlayableCombatCard(CardModel card)
    {
        return card.Rarity is not CardRarity.Status
            and not CardRarity.Curse
            and not CardRarity.Quest
            and not CardRarity.Event;
    }

    internal static bool IsStatusOrCurse(CardModel card)
    {
        return card.Rarity is CardRarity.Status or CardRarity.Curse
            || card.Type is CardType.Status or CardType.Curse;
    }

    internal static bool IsStatus(CardModel card)
    {
        return card.Rarity == CardRarity.Status || card.Type == CardType.Status;
    }

    internal static bool IsAnanlinPoolCard(CardModel card)
    {
        return card.Pool.Id == ModelDb.GetId(typeof(AnanlinCardPool));
    }

    internal static void CopyUpgradeLevel(CardModel source, CardModel target)
    {
        for (var i = 0; i < source.CurrentUpgradeLevel; i++)
            CardCmd.Upgrade(target);
    }

    private static CardModel? FindMatchingCard(Player player, Func<CardModel, bool> predicate)
    {
        var hand = PileType.Hand.GetPile(player);
        if (hand.Cards.Count >= CardPile.MaxCardsInHand) return null;

        var candidates = PileType.Draw.GetPile(player).Cards
            .Concat(PileType.Discard.GetPile(player).Cards)
            .Where(predicate)
            .ToArray();
        return candidates.Length == 0
            ? null
            : player.RunState.Rng.CombatCardSelection.NextItem(candidates);
    }
}
