using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MinionLib.Component;
using MinionLib.Component.Core;

namespace ManosabaLin.Characters.Common.Components;

public sealed partial class GenerateComponent : CardComponent
{
    private IHoverTip? _hovertip;
    [LocArg] private string? CardTitle { get; set; }

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

    public GenerateComponent(CardModel card)
    {
        SavedCard = card.ToSerializable();
    }

    public override async Task OnPlayPostfix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var mutable = CardModel.FromSerializable(SavedCard);
        Card!.CombatState!.AddCard(mutable, Card.Owner);
        await CardPileCmd.AddGeneratedCardToCombat(mutable, PileType.Hand, Card.Owner);
        ComponentsCard!.RefRemoveComponent(this);
    }
}
