using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Sherrylin.Capabilities;

[RegisterModelCapability]
public class RemoveOnPlayCapability : OneShotCardPlayCapability
{
    public static readonly IHoverTip Tip = new HoverTip(
        new LocString("cards", "ManosabaLin.RemoveOnPlayCapability.hovertip.title"),
        new LocString("cards", "ManosabaLin.RemoveOnPlayCapability.hovertip.description"));

    protected override Task OnOwnerCardPlayedOnce(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return Task.CompletedTask;
        var card = Owner;
        MegaCrit.Sts2.Core.Helpers.TaskHelper.RunSafely(RemoveNextFrame(card));
        return Task.CompletedTask;
    }

    private async Task RemoveNextFrame(CardModel card)
    {
        await Task.Yield();
        await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
    }
}