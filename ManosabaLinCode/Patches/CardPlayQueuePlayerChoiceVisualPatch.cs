using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Actions;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ManosabaLin.Patches;

[HarmonyPatch(typeof(NCardPlayQueue), nameof(NCardPlayQueue.ReAddCardAfterPlayerChoice))]
internal static class CardPlayQueuePlayerChoiceVisualPatch
{
    private static void Prefix(NCard card, GameAction action)
    {
        if (card.GetParent() is not null) return;

        var targetParent = action.State == GameActionState.Executing
            ? NCombatRoom.Instance?.Ui.PlayContainer
            : NCardPlayQueue.Instance;

        targetParent?.AddChildSafely(card);
    }
}
