using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Runs;
using ManosabaLin.Characters.Hiro.Monsters;

namespace ManosabaLin.Patches;

internal static class FusionStandManager
{
    private static readonly Dictionary<MonsterModel, int> TurnCounts = [];

    internal static event Action<MonsterModel>? StandActivated;

    private static int _combatHash;

    internal const string FusedMoveSuffix = "_FUSION_STAND";

    internal static bool IsActiveForCurrentCombat()
    {
        return RunManager.Instance?.DebugOnlyGetState() != null;
    }

    internal static void ClearForNewCombat(int combatHash)
    {
        if (combatHash == _combatHash) return;

        _combatHash = combatHash;
        TurnCounts.Clear();
        FusionStandIntentPatch.ClearForNewCombat();
    }

    internal static bool EnsureStand(MonsterModel monster)
    {
        var combat = monster.Creature?.CombatState;
        if (combat == null) return false;
        if (!IsActiveForCurrentCombat()) return false;

        ClearForNewCombat(combat.GetHashCode());
        StandActivated?.Invoke(monster);
        return true;
    }

    internal static MoveState? PickStandMove(MonsterModel monster)
    {
        var standMoves = GetStandMoves(monster);
        if (standMoves.Count == 0) return null;

        var turn = TurnCounts.TryGetValue(monster, out var currentTurn) ? currentTurn : 0;
        TurnCounts[monster] = turn + 1;

        var runSeed = RunManager.Instance?.DebugOnlyGetState()?.Rng.StringSeed ?? "0";
        var monsterId = ((AbstractModel)monster).Id.Entry;
        var seed = MegaCrit.Sts2.Core.Helpers.StringHelper.GetDeterministicHashCode(
            $"{runSeed}_{monsterId}_FUSION_STAND_T{turn}");

        return standMoves[new Random(seed).Next(standMoves.Count)];
    }

    private static IReadOnlyList<MoveState> GetStandMoves(MonsterModel monster)
    {
        if (monster is not GuardThreeMonster guardThree)
            return [];

        return guardThree.CreatePhaseTwoStandMoves()
            .Where(move => move.Intents.Count > 0)
            .ToList();
    }
}
