// UncontrolledJusticePower.cs
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Hiro.Monsters;
using System.Threading.Tasks;
using HarmonyLib;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class UncontrolledJusticePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner) return true;
        if (Owner.Monster is not GuardThreeMonster) return true;
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner) return;
        if (Owner.Monster is not GuardThreeMonster) return;

        await GuardThreeCombatSingleton.HandlePhaseOneResurrection(creature, this);
    }
}

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.PerformMove))]
public static class UncontrolledJusticeDoubleTriggerPatch
{
    private const int MaxJusticeStacks = 5;

    [ThreadStatic] private static bool _isExtraTrigger;

    [HarmonyPrefix]
    public static void Prefix(MonsterModel __instance, out MoveState? __state)
    {
        __state = null;
        if (_isExtraTrigger) return;

        var power = __instance.Creature.GetPower<UncontrolledJusticePower>();
        if (power == null) return;

        var rng = __instance.RunRng.CombatTargets;
        var chance = Math.Clamp(power.Amount, 0, MaxJusticeStacks) / (float)MaxJusticeStacks;
        if (rng.NextFloat() >= chance) return;

        __state = __instance.NextMove;
    }

    [HarmonyPostfix]
    public static void Postfix(MonsterModel __instance, MoveState? __state, ref Task __result)
    {
        if (__state == null) return;
        if (__state.Id.Contains("PHASE2")) return;

        __result = PerformExtraMoveAfterOriginal(__result, __instance, __state);
    }

    private static async Task PerformExtraMoveAfterOriginal(
        Task originalMoveTask,
        MonsterModel monster,
        MoveState extraMove)
    {
        await originalMoveTask;
        if (monster.Creature.IsDead) return;
        if (monster.Creature.GetPower<UncontrolledJusticePower>() == null) return;

        _isExtraTrigger = true;
        try
        {
            monster.SetMoveImmediate(extraMove, forceTransition: true);
            await monster.PerformMove();
        }
        finally
        {
            _isExtraTrigger = false;
        }
    }
}
