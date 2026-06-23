using ManosabaLin.Characters.Common.Components.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MinionLib.Component.Core;

namespace ManosabaLin.Characters.Sherrylin.Components;

public sealed partial class LevitationComponent : KeywordLikeComponent
{
    public static readonly IHoverTip Tip = new HoverTip(
        new LocString("cards", "ManosabaLin.LevitationComponent.hovertip.title"),
        new LocString("cards", "ManosabaLin.LevitationComponent.hovertip.description"));
        
    public override async Task AfterCardPlayedPostfix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var card = Card;
        if (card == null) return;
        if (card.Pile?.Type != PileType.Hand) return;
        if (cardPlay.Card == card) return;
        if (cardPlay.Card.EnergyCost.Canonical < 2) return;

        await CardCmd.AutoPlay(choiceContext, card, cardPlay.Target);
    }
}
