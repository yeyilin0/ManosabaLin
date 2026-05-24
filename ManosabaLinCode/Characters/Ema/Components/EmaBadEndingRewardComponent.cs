using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using MinionLib.RightClick;

namespace ManosabaLin.Characters.Emalin.Components;

public sealed partial class EmaBadEndingRewardComponent : CardComponent
{
    [ComponentState] private List<SerializableCard> SavedCards { get; set; }

    [ComponentState] private List<int> UseLeft { get; set; }

    [LocArg] private int CardCount => SavedCards.Count;

    public EmaBadEndingRewardComponent(CardModel card)
    {
        var serializable = card.ToSerializable();

        var use = card.Rarity switch
        {
            CardRarity.Rare => 1,
            CardRarity.Uncommon => 2,
            _ => throw new AbandonedMutexException($"Unexpected rarity {card.Rarity} for card {card.Id}")
        };

        SavedCards = [serializable];
        UseLeft = [use];
    }

    public override bool TryMergeWith(ICardComponent incoming, ApplyComponentOptions options,
        out ICardComponent? merged)
    {
        if (incoming is not EmaBadEndingRewardComponent other)
        {
            merged = null;
            return false;
        }

        SavedCards.AddRange(other.SavedCards);
        UseLeft.AddRange(other.UseLeft);
        merged = this;
        return true;
    }

    private IEnumerable<CardModel> UseOnce()
    {
        var newSavedCard = new List<SerializableCard>();
        var newUseLeft = new List<int>();
        var result = new List<CardModel>();
        for (var i = 0; i < CardCount; i++)
        {
            result.Add(CardModel.FromSerializable(SavedCards[i]));
            if (UseLeft[i] > 1)
            {
                newSavedCard.Add(SavedCards[i]);
                newUseLeft.Add(UseLeft[i] - 1);
            }
        }

        SavedCards = newSavedCard;
        UseLeft = newUseLeft;
        return result;
    }

    public override async Task OnPlayPostfix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var cards = UseOnce();
        foreach (var card in cards)
        {
            Card!.CombatState!.AddCard(card, Card.Owner);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Play, Card.Owner, CardPilePosition.Top);
            await CardCmd.AutoPlay(choiceContext, card, null);
            if (card.Pile is { IsCombatPile: true })
                await CardPileCmd.RemoveFromCombat(card);
        }

        if (CardCount == 0)
            ComponentsCard!.RefRemoveComponent(this);
    }

    public override bool CanHandleRightClick(RightClickContext context) => true;

    public override async Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        if (CardCount == 0) return;
        var cards = SavedCards
            .Select(CardModel.FromSerializable)
            .ToList();
        var prompt = new LocString("cards", $"{ComponentId}.rightClickPrompt");
        prompt.Add(nameof(CardCount), CardCount);
        await CardSelectCmd.FromSimpleGrid(choiceContext, cards, Card!.Owner,
            new CardSelectorPrefs(prompt, 0)
            {
                Cancelable = true
            });
    }
}
