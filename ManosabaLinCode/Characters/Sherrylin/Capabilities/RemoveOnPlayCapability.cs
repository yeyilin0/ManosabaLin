using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Sherrylin.Capabilities;

[RegisterModelCapability]
public class RemoveOnPlayCapability : OneShotCardPlayCapability, ICardPlayResultContributor
{
    public static readonly IHoverTip Tip = new HoverTip(
        new LocString("cards", "ManosabaLin.RemoveOnPlayCapability.hovertip.title"),
        new LocString("cards", "ManosabaLin.RemoveOnPlayCapability.hovertip.description"));

    public PileType? GetResultPileTypeForCardPlay(CardModel card) => PileType.None;

    protected override async Task OnOwnerCardPlayedOnce(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 不需要 RemoveFromCombat，GetResultPileTypeForCardPlay 已经处理了
    }
}