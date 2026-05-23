using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MinionLib.Component;
using MinionLib.Component.Core;

namespace ManosabaLin.Characters.Emalin.Components;

public sealed partial class EmaBadEndingRewardComponent : CardComponent
{
    private IHoverTip? _hovertip;
    [LocArg] private string? CardTitle { get; set; }

    [ComponentState] private int UseLeft { get; set; }

    [ComponentState]
    private SerializableCard SavedCard
    {
        get;
        set
        {
            field = value;
            var mutable = CardModel.FromSerializable(SavedCard);
            _hovertip = new CardHoverTip(mutable);
            CardTitle = mutable.Title;
        }
    }

    public override IEnumerable<IHoverTip> HoverTips => _hovertip == null ? [] : [_hovertip];

    public EmaBadEndingRewardComponent(CardModel card)
    {
        SavedCard = card.ToSerializable();

        UseLeft = card.Rarity switch
        {
            CardRarity.Rare => 1,
            CardRarity.Uncommon => 2,
            _ => throw new AbandonedMutexException($"Unexpected rarity {card.Rarity} for card {card.Id}")
        };
    }

    public override async Task OnPlayPostfix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var card = CardModel.FromSerializable(SavedCard);
        Card!.CombatState!.AddCard(card, Card.Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Play, Card.Owner, CardPilePosition.Top);
        await CardCmd.AutoPlay(choiceContext, card, null);
        if (card.Pile is { IsCombatPile: true })
            await CardPileCmd.RemoveFromCombat(card);
        UseLeft--;
        if(UseLeft <= 0)
            ComponentsCard!.RefRemoveComponent(this);
    }
}
