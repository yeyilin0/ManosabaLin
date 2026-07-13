using System.Reflection;
using HarmonyLib;
using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ManosabaLin.Characters.Ananlin.Relics;

internal static class AnanlinSilenceIntentManager
{
    private const string EnergyMoveId = "MANOSABA_LIN_ANANLIN_SILENT_ENERGY_MOVE";
    private const string DrawMoveId = "MANOSABA_LIN_ANANLIN_SILENT_DRAW_MOVE";
    private const string BlockMoveId = "MANOSABA_LIN_ANANLIN_SILENT_BLOCK_MOVE";
    private const string VigorMoveId = "MANOSABA_LIN_ANANLIN_SILENT_VIGOR_MOVE";
    private const int BaseEnergy = 1;
    private const int BaseDraw = 1;
    private const int BaseBlock = 6;
    private const int BaseVigor = 2;

    private enum ReplacementIntentKind
    {
        Energy,
        Draw,
        Block,
        Vigor
    }

    private static readonly ReplacementIntentKind[] ReplacementIntentCycle =
    [
        ReplacementIntentKind.Energy,
        ReplacementIntentKind.Draw,
        ReplacementIntentKind.Block,
        ReplacementIntentKind.Vigor
    ];

    private static readonly FieldInfo? OnPerformField = AccessTools.Field(typeof(MoveState), "_onPerform");
    private static readonly Dictionary<Creature, MoveState> PendingBuffMoves = [];
    private static readonly Dictionary<Creature, HashSet<string>> UsedMovesByPhase = [];
    private static readonly Dictionary<Creature, string> PhaseKeys = [];
    private static readonly Dictionary<MoveState, MoveState> BaseMovesByReplacement = [];
    private static readonly Dictionary<Player, HashSet<ReplacementIntentKind>> UsedReplacementIntentsByPlayer = [];
    private static readonly Dictionary<Player, int> RewritesThisCombatByPlayer = [];

    [ThreadStatic] private static bool _isApplyingReplacement;

    private sealed record ReplacementIntentChoice(ReplacementIntentKind Kind, MoveState Move);

    internal static async Task<int> Trigger(PlayerChoiceContext choiceContext, Player owner)
    {
        var combatState = owner.Creature.CombatState;
        if (combatState is null) return 0;

        var enemies = combatState.Enemies
            .Where(static c => c.IsAlive && c.Monster?.NextMove is not null)
            .ToArray();
        var replacementTargets = new List<Creature>();

        foreach (var enemy in enemies)
        {
            if (enemy.Monster is not { } monster) continue;
            if (IsReplacementMove(monster.NextMove)) continue;

            var currentMove = ResolveBaseMove(monster.NextMove);
            if (currentMove is null || !CanForceNow(monster, currentMove)) continue;

            MarkForced(monster, currentMove);
            monster.SetMoveImmediate(currentMove, forceTransition: true);
            await monster.PerformMove();

            if (enemy.IsAlive)
                replacementTargets.Add(enemy);
        }

        if (replacementTargets.Count == 0) return 0;

        var selectedBuff = await ChoosePlayerBuffIntent(choiceContext, owner);
        if (selectedBuff is null) return 0;

        var rewriteCount = 0;
        foreach (var enemy in replacementTargets.Where(static e => e.IsAlive))
        {
            if (enemy.Monster is not { } monster) continue;
            if (ApplyReplacementMove(monster, selectedBuff.Move))
                rewriteCount++;
        }

        if (rewriteCount > 0)
        {
            MarkReplacementIntentUsed(owner, selectedBuff.Kind);
            RewritesThisCombatByPlayer[owner] = GetRewritesThisCombat(owner) + rewriteCount;
            if (owner.Creature.GetPower<AnanlinSealedPagePower>() is { } sealedPage)
                await sealedPage.AfterSilenceRightClickRewrite(choiceContext);
        }

        RecordIntentRewrites(combatState, rewriteCount);
        return rewriteCount;
    }

    internal static async Task<bool> ForceBrainwash(
        PlayerChoiceContext choiceContext,
        Player owner,
        Func<Task<bool>>? beforeApply = null)
    {
        var combatState = owner.Creature.CombatState;
        if (combatState is null) return false;

        var targets = GetBrainwashTargets(owner);
        if (targets.Count == 0) return false;

        var selectedBuff = await ChoosePlayerBuffIntent(choiceContext, owner);
        if (selectedBuff is null) return false;

        if (beforeApply is not null && !await beforeApply())
            return false;

        var rewriteCount = 0;
        foreach (var target in targets.Where(CanBrainwashTarget))
        {
            if (target.Monster is not { } monster) continue;
            if (ApplyReplacementMove(monster, selectedBuff.Move))
                rewriteCount++;
        }

        if (rewriteCount <= 0) return false;

        MarkReplacementIntentUsed(owner, selectedBuff.Kind);
        RewritesThisCombatByPlayer[owner] = GetRewritesThisCombat(owner) + rewriteCount;
        RecordIntentRewrites(combatState, rewriteCount);
        return true;
    }

    internal static int GetRewritesThisCombat(Player owner)
    {
        return RewritesThisCombatByPlayer.GetValueOrDefault(owner);
    }

    internal static bool CanForceBrainwash(Player owner)
    {
        return GetBrainwashTargets(owner).Any()
            && GetAvailableReplacementIntentKinds(owner).Any();
    }

    internal static bool CanTrigger(Player owner)
    {
        var combatState = owner.Creature.CombatState;
        if (combatState is null) return false;

        foreach (var enemy in combatState.Enemies.Where(static c => c.IsAlive))
        {
            if (enemy.Monster is not { NextMove: { } nextMove } monster) continue;
            if (IsReplacementMove(nextMove)) continue;

            var currentMove = ResolveBaseMove(nextMove);
            if (currentMove is not null && CanForceNow(monster, currentMove))
                return true;
        }

        return false;
    }

    private static IReadOnlyList<Creature> GetBrainwashTargets(Player owner)
    {
        var combatState = owner.Creature.CombatState;
        if (combatState is null) return Array.Empty<Creature>();

        return combatState.Enemies
            .Where(CanBrainwashTarget)
            .ToArray();
    }

    private static bool CanBrainwashTarget(Creature enemy)
    {
        return enemy is { IsAlive: true, Monster: { NextMove: { } nextMove } }
            && !IsReplacementMove(nextMove);
    }

    internal static bool TryApplyPendingBuffMove(MonsterModel monster)
    {
        if (_isApplyingReplacement) return false;
        var creature = monster.Creature;
        if (!PendingBuffMoves.Remove(creature, out var buffMove)) return false;

        return ApplyReplacementMove(monster, buffMove);
    }

    private static bool ApplyReplacementMove(MonsterModel monster, MoveState buffMove)
    {
        if (_isApplyingReplacement) return false;
        if (IsReplacementMove(monster.NextMove)) return false;

        var baseMove = ResolveBaseMove(monster.NextMove);
        if (baseMove is null) return false;

        var replacement = CloneMoveWithPerform(
            buffMove,
            async targets =>
            {
                await buffMove.PerformMove(targets);
                monster.SetMoveImmediate(baseMove, forceTransition: true);
            },
            $"{buffMove.StateId}_{baseMove.StateId}");

        BaseMovesByReplacement[replacement] = baseMove;

        _isApplyingReplacement = true;
        try
        {
            monster.SetMoveImmediate(replacement, forceTransition: true);
        }
        finally
        {
            _isApplyingReplacement = false;
        }

        return true;
    }

    internal static void ClearForNewCombat()
    {
        PendingBuffMoves.Clear();
        UsedMovesByPhase.Clear();
        PhaseKeys.Clear();
        BaseMovesByReplacement.Clear();
        UsedReplacementIntentsByPlayer.Clear();
        RewritesThisCombatByPlayer.Clear();
    }

    internal static bool TryForgetRecordedAttack(Creature target)
    {
        if (target.Monster is not { } monster) return false;

        var phaseKey = GetPhaseKey(monster);
        if (!PhaseKeys.TryGetValue(target, out var knownPhase) || knownPhase != phaseKey)
            return false;
        if (!UsedMovesByPhase.TryGetValue(target, out var usedMoves) || usedMoves.Count == 0)
            return false;

        var currentBaseMove = ResolveBaseMove(monster.NextMove);
        if (currentBaseMove is not null
            && HasAttackIntent(currentBaseMove)
            && usedMoves.Remove(currentBaseMove.StateId))
        {
            return true;
        }

        var recordedAttackId = monster.MoveStateMachine?.States.Values
            .OfType<MoveState>()
            .Where(HasAttackIntent)
            .Select(static move => move.StateId)
            .Where(usedMoves.Contains)
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();

        return recordedAttackId is not null && usedMoves.Remove(recordedAttackId);
    }

    internal static void RecordIntentRewrites(ICombatState? combatState, int count)
    {
        if (combatState is null || count <= 0) return;

        foreach (var player in combatState.Players)
        {
            player.Creature.GetPower<AnanlinJudgmentEvePower>()?.RecordRewrites(count);
            player.Creature.GetPower<AnanlinSilentAmplificationPower>()?.RecordRewrites(count);
        }
    }

    private static async Task<ReplacementIntentChoice?> ChoosePlayerBuffIntent(PlayerChoiceContext choiceContext, Player owner)
    {
        if (owner.Creature.CombatState is not { } combatState) return null;

        var availableKinds = GetAvailableReplacementIntentKinds(owner).ToArray();
        if (availableKinds.Length == 0) return null;

        var options = availableKinds
            .Select(kind => CreateReplacementOptionCard(kind, combatState, owner))
            .ToArray();

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            options,
            owner,
            new CardSelectorPrefs(
                new LocString("relics", "MANOSABA_LIN_RELIC_ANANS_SKETCHBOOK.selectionScreenPrompt"),
                1,
                1))).FirstOrDefault();

        var selectedKind = GetReplacementIntentKind(selected);
        if (selectedKind is null) return null;

        return new ReplacementIntentChoice(selectedKind.Value, CreateReplacementMove(owner, selectedKind.Value));
    }

    private static IEnumerable<ReplacementIntentKind> GetAvailableReplacementIntentKinds(Player owner)
    {
        if (!UsedReplacementIntentsByPlayer.TryGetValue(owner, out var used))
            UsedReplacementIntentsByPlayer[owner] = used = [];

        if (used.Count >= ReplacementIntentCycle.Length)
            used.Clear();

        return ReplacementIntentCycle.Where(kind => !used.Contains(kind));
    }

    private static void MarkReplacementIntentUsed(Player owner, ReplacementIntentKind kind)
    {
        if (!UsedReplacementIntentsByPlayer.TryGetValue(owner, out var used))
            UsedReplacementIntentsByPlayer[owner] = used = [];

        if (used.Count >= ReplacementIntentCycle.Length)
            used.Clear();

        used.Add(kind);
    }

    private static CardModel CreateReplacementOptionCard(
        ReplacementIntentKind kind,
        ICombatState combatState,
        Player owner)
    {
        CardModel card = kind switch
        {
            ReplacementIntentKind.Energy => combatState.CreateCard<AnanlinSilenceIntentEnergyOption>(owner),
            ReplacementIntentKind.Draw => combatState.CreateCard<AnanlinSilenceIntentDrawOption>(owner),
            ReplacementIntentKind.Block => combatState.CreateCard<AnanlinSilenceIntentBlockOption>(owner),
            ReplacementIntentKind.Vigor => combatState.CreateCard<AnanlinSilenceIntentVigorOption>(owner),
            _ => combatState.CreateCard<AnanlinSilenceIntentBlockOption>(owner)
        };

        ApplyReplacementOptionValue(card, kind, GetReplacementValueMultiplier(owner));
        return card;
    }

    private static void ApplyReplacementOptionValue(CardModel card, ReplacementIntentKind kind, int multiplier)
    {
        switch (kind)
        {
            case ReplacementIntentKind.Energy:
                card.DynamicVars.Energy.BaseValue = BaseEnergy * multiplier;
                break;
            case ReplacementIntentKind.Draw:
                card.DynamicVars.Cards.BaseValue = BaseDraw * multiplier;
                break;
            case ReplacementIntentKind.Block:
                card.DynamicVars.Block.BaseValue = BaseBlock * multiplier;
                break;
            case ReplacementIntentKind.Vigor:
                card.DynamicVars["VigorPower"].BaseValue = BaseVigor * multiplier;
                break;
        }
    }

    private static ReplacementIntentKind? GetReplacementIntentKind(CardModel? card)
    {
        return card switch
        {
            AnanlinSilenceIntentEnergyOption => ReplacementIntentKind.Energy,
            AnanlinSilenceIntentDrawOption => ReplacementIntentKind.Draw,
            AnanlinSilenceIntentBlockOption => ReplacementIntentKind.Block,
            AnanlinSilenceIntentVigorOption => ReplacementIntentKind.Vigor,
            _ => null
        };
    }

    private static MoveState CreateReplacementMove(Player owner, ReplacementIntentKind kind)
    {
        var multiplier = GetReplacementValueMultiplier(owner);
        return kind switch
        {
            ReplacementIntentKind.Energy => new MoveState(
                EnergyMoveId,
                async _ => await PowerCmd.Apply<EnergyNextTurnPower>(
                    new ThrowingPlayerChoiceContext(),
                    owner.Creature,
                    BaseEnergy * multiplier,
                    owner.Creature,
                    null),
                new BuffIntent()),
            ReplacementIntentKind.Draw => new MoveState(
                DrawMoveId,
                async _ => await CardPileCmd.Draw(
                    new ThrowingPlayerChoiceContext(),
                    BaseDraw * multiplier,
                    owner),
                new BuffIntent()),
            ReplacementIntentKind.Block => new MoveState(
                BlockMoveId,
                async _ => await CreatureCmd.GainBlock(
                    owner.Creature,
                    BaseBlock * multiplier,
                    ValueProp.Move,
                    null),
                new DefendIntent()),
            ReplacementIntentKind.Vigor => new MoveState(
                VigorMoveId,
                async _ => await PowerCmd.Apply<VigorPower>(
                    new ThrowingPlayerChoiceContext(),
                    owner.Creature,
                    BaseVigor * multiplier,
                    owner.Creature,
                    null),
                new BuffIntent()),
            _ => new MoveState(
                BlockMoveId,
                async _ => await CreatureCmd.GainBlock(owner.Creature, BaseBlock * multiplier, ValueProp.Move, null),
                new DefendIntent())
        };
    }

    private static int GetReplacementValueMultiplier(Player owner)
    {
        return owner.Creature.GetPower<AnanlinSilentAmplificationPower>()?.ReplacementValueMultiplier ?? 1;
    }

    private static bool CanForceNow(MonsterModel monster, MoveState move)
    {
        var phaseKey = GetPhaseKey(monster);
        if (!PhaseKeys.TryGetValue(monster.Creature, out var knownPhase) || knownPhase != phaseKey)
        {
            PhaseKeys[monster.Creature] = phaseKey;
            UsedMovesByPhase[monster.Creature] = [];
        }

        var used = UsedMovesByPhase[monster.Creature];
        var phaseMoves = GetPhaseMoveIds(monster);
        if (phaseMoves.Count > 0 && used.Count >= phaseMoves.Count)
            used.Clear();

        return !used.Contains(move.StateId);
    }

    private static void MarkForced(MonsterModel monster, MoveState move)
    {
        if (!UsedMovesByPhase.TryGetValue(monster.Creature, out var used))
            UsedMovesByPhase[monster.Creature] = used = [];

        used.Add(move.StateId);
    }

    private static string GetPhaseKey(MonsterModel monster)
    {
        var moveIds = GetPhaseMoveIds(monster);
        return $"{monster.Id.Entry}:{string.Join("|", moveIds.Order(StringComparer.Ordinal))}";
    }

    private static HashSet<string> GetPhaseMoveIds(MonsterModel monster)
    {
        var machine = monster.MoveStateMachine;
        if (machine is null) return [];

        return machine.States.Values
            .OfType<MoveState>()
            .Where(static m => m.IsMove && m.ShouldAppearInLogs && m.StateId != MonsterModel.stunnedMoveId)
            .Select(static m => m.StateId)
            .ToHashSet();
    }

    private static MoveState? ResolveBaseMove(MoveState? move)
    {
        if (move is null) return null;
        return BaseMovesByReplacement.GetValueOrDefault(move) ?? move;
    }

    private static bool IsReplacementMove(MoveState? move)
    {
        return move is not null && BaseMovesByReplacement.ContainsKey(move);
    }

    private static bool HasAttackIntent(MoveState move)
    {
        return move.Intents.Any(static intent => intent is AttackIntent);
    }

    private static MoveState CloneMoveWithPerform(
        MoveState source,
        Func<IReadOnlyList<Creature>, Task> perform,
        string suffix)
    {
        var clone = new MoveState(
            $"{source.StateId}_{suffix}",
            perform,
            source.Intents.ToArray())
        {
            FollowUpState = source.FollowUpState,
            FollowUpStateId = source.FollowUpStateId,
            MustPerformOnceBeforeTransitioning = source.MustPerformOnceBeforeTransitioning
        };

        if (OnPerformField?.GetValue(source) is not null)
            return clone;

        return clone;
    }
}
