using HarmonyLib;
using ManosabaLin.Characters.Common;
using ManosabaLin.ManosabaLinCode.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace ManosabaLin.ManosabaLinCode.Characters.Common.patches;

internal static class RedirectMoveTargetContext
{
    private static readonly AsyncLocal<Creature?> CurrentOwner = new();
    private static readonly AsyncLocal<Creature?> CurrentRedirectTarget = new();

    public static Creature? Owner
    {
        get => CurrentOwner.Value;
        set => CurrentOwner.Value = value;
    }

    public static Creature? RedirectTarget
    {
        get => CurrentRedirectTarget.Value;
        set => CurrentRedirectTarget.Value = value;
    }
}

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.PerformMove))]
internal static class MonsterModelPerformMovePatch
{
    private static void Prefix(MonsterModel __instance, out Creature? __state)
    {
        __state = RedirectMoveTargetContext.Owner;
        RedirectMoveTargetContext.Owner = __instance.Creature;
        RedirectMoveTargetContext.RedirectTarget = LymPower.GetRedirectChosenMoveTarget(__instance.Creature);
    }

    private static void Postfix(ref Task __result, Creature? __state)
    {
        __result = RestoreOwnerAfterMove(__result, __state);
    }

    private static async Task RestoreOwnerAfterMove(Task moveTask, Creature? previousOwner)
    {
        try
        {
            await moveTask;
        }
        finally
        {
            RedirectMoveTargetContext.Owner = previousOwner;
        }
    }
}

[HarmonyPatch(typeof(MoveState), nameof(MoveState.PerformMove))]
internal static class MoveStatePerformMovePatch
{
    private static void Prefix(ref IEnumerable<Creature> targets)
    {
        var chosenTarget = LymPower.GetRedirectChosenMoveTarget(RedirectMoveTargetContext.Owner);
        if (chosenTarget is null) return;

        targets = new[] { chosenTarget };
    }
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock),
    new[] { typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardPlay), typeof(bool) })]
internal static class RedirectGainBlockPatch
{
    private static void Prefix(ref Creature creature)
    {
        var redirectTarget = RedirectMoveTargetContext.RedirectTarget;
        if (redirectTarget is not null && redirectTarget.IsAlive)
        {
            creature = redirectTarget;
        }
    }
}

[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.Apply),
    new[] { typeof(PlayerChoiceContext), typeof(PowerModel), typeof(Creature), typeof(decimal), typeof(Creature), typeof(CardModel), typeof(bool) })]
internal static class RedirectPowerApplyPatch
{
    private static void Prefix(ref Creature target)
    {
        var redirectTarget = RedirectMoveTargetContext.RedirectTarget;
        if (redirectTarget is not null && redirectTarget.IsAlive)
        {
            target = redirectTarget;
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.TitleLocString), MethodType.Getter)]
internal static class MoveChoiceCardTitleLocStringPatch
{
    private static bool Prefix(CardModel __instance, ref LocString __result)
    {
        if (__instance is MoveChoiceCard { IsConfigured: true } moveChoiceCard)
        {
            __result = moveChoiceCard.TitleOverride;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
internal static class MoveChoiceCardTitlePatch
{
    private static bool Prefix(CardModel __instance, ref string __result)
    {
        if (__instance is MoveChoiceCard { IsConfigured: true } moveChoiceCard)
        {
            __result = moveChoiceCard.DisplayTitle;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class MoveChoiceCardDescriptionPatch
{
    private static bool Prefix(CardModel __instance, ref LocString __result)
    {
        if (__instance is MoveChoiceCard { IsConfigured: true } moveChoiceCard)
        {
            __result = moveChoiceCard.DescriptionOverride;
            return false;
        }
        return true;
    }
}
