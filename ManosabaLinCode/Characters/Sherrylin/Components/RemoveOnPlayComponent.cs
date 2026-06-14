using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MinionLib.Component;
using MinionLib.Component.Core;

namespace ManosabaLin.Characters.Sherrylin.Components;

/// <summary>
/// 打出后移除：打出该卡后将其从战斗中永久移除。
/// </summary>
public sealed partial class RemoveOnPlayComponent : CardComponent
{
    public static readonly IHoverTip Tip = new HoverTip(
        new LocString("cards", "ManosabaLin.RemoveOnPlayComponent.hovertip.title"),
        new LocString("cards", "ManosabaLin.RemoveOnPlayComponent.hovertip.description"));

    public override IEnumerable<IHoverTip> HoverTips => [Tip];
    public override PileType? GetResultPileTypeForCardPlay() => PileType.None;

    public override async Task OnPlayPostfix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var card = Card;
        if (card == null) return;
        await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
    }
}
