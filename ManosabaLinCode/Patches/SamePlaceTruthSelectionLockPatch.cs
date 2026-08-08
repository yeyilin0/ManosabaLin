using HarmonyLib;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Patches;

[HarmonyPatch]
internal static class SamePlaceTruthSelectionLockPatch
{
    [HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromChooseACardScreen),
        [typeof(PlayerChoiceContext), typeof(IReadOnlyList<CardModel>), typeof(Player), typeof(bool)])]
    [HarmonyPrefix]
    private static void FromChooseACardScreenPrefix(ref IReadOnlyList<CardModel> __1)
    {
        __1 = FilterCards(__1);
    }

    [HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromSimpleGrid),
        [typeof(PlayerChoiceContext), typeof(IReadOnlyList<CardModel>), typeof(Player), typeof(CardSelectorPrefs)])]
    [HarmonyPrefix]
    private static void FromSimpleGridPrefix(ref IReadOnlyList<CardModel> __1)
    {
        __1 = FilterCards(__1);
    }

    [HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromCombatPile),
        [typeof(PlayerChoiceContext), typeof(CardPile), typeof(Player), typeof(CardSelectorPrefs), typeof(Func<CardModel, bool>)])]
    [HarmonyPrefix]
    private static void FromCombatPilePrefix(ref Func<CardModel, bool>? __4)
    {
        __4 = CombineFilter(__4);
    }

    [HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromHand),
        [typeof(PlayerChoiceContext), typeof(Player), typeof(CardSelectorPrefs), typeof(Func<CardModel, bool>), typeof(AbstractModel)])]
    [HarmonyPrefix]
    private static void FromHandPrefix(ref Func<CardModel, bool>? __3)
    {
        __3 = CombineFilter(__3);
    }

    [HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromHandForDiscard),
        [typeof(PlayerChoiceContext), typeof(Player), typeof(CardSelectorPrefs), typeof(Func<CardModel, bool>), typeof(AbstractModel)])]
    [HarmonyPrefix]
    private static void FromHandForDiscardPrefix(ref Func<CardModel, bool>? __3)
    {
        __3 = CombineFilter(__3);
    }

    private static IReadOnlyList<CardModel> FilterCards(IReadOnlyList<CardModel> cards)
    {
        return cards.Any(SamePlaceTruth.IsSelectionLocked)
            ? cards.Where(static card => !SamePlaceTruth.IsSelectionLocked(card)).ToArray()
            : cards;
    }

    private static Func<CardModel, bool> CombineFilter(Func<CardModel, bool>? original)
    {
        return card => !SamePlaceTruth.IsSelectionLocked(card) && (original?.Invoke(card) ?? true);
    }
}

[HarmonyPatch]
internal static class SamePlaceTruthCanPlayLockPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.CanPlay),
            [typeof(UnplayableReason).MakeByRefType(), typeof(AbstractModel).MakeByRefType()]);
    }

    [HarmonyPostfix]
    private static void Postfix(
        CardModel __instance,
        ref bool __result,
        ref UnplayableReason reason,
        ref AbstractModel? preventer)
    {
        if (!SamePlaceTruth.IsSelectionLocked(__instance))
        {
            return;
        }

        __result = false;
        reason |= UnplayableReason.BlockedByCardLogic;
        preventer ??= __instance;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlay), [])]
internal static class SamePlaceTruthCanPlayNoOutLockPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref bool __result)
    {
        if (SamePlaceTruth.IsSelectionLocked(__instance))
        {
            __result = false;
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlayTargeting), [typeof(Creature)])]
internal static class SamePlaceTruthCanPlayTargetingLockPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref bool __result)
    {
        if (SamePlaceTruth.IsSelectionLocked(__instance))
        {
            __result = false;
        }
    }
}
