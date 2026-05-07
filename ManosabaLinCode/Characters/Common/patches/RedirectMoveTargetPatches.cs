using HarmonyLib;
using ManosabaLin.ManosabaLinCode.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ManosabaLin.ManosabaLinCode.Characters.Common.patches;

internal static class RedirectMoveTargetContext
{
    private static readonly AsyncLocal<Creature?> CurrentOwner = new();

    public static Creature? Owner
    {
        get => CurrentOwner.Value;
        set => CurrentOwner.Value = value;
    }
}

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.PerformMove))]
internal static class MonsterModelPerformMovePatch
{
    private static void Prefix(MonsterModel __instance, out Creature? __state)
    {
        __state = RedirectMoveTargetContext.Owner;
        RedirectMoveTargetContext.Owner = __instance.Creature;
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
        var chosenTarget = LymPower.GetChosenMoveTarget(RedirectMoveTargetContext.Owner);
        if (chosenTarget is null)
        {
            return;
        }

        targets = new[] { chosenTarget };
    }
}