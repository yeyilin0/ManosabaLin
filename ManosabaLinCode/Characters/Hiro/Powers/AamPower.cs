using System.Reflection;
using Godot;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.ManosabaLinCode.Characters.Hiro.Powers;

[RegisterPower]
public class AamPower : ManosabaPowerTemplate, IRedirectPower
{
    private static readonly FieldInfo SingleTargetField = GetAttackCommandField("_singleTarget");
    private static readonly FieldInfo CombatStateField = GetAttackCommandField("_combatState");
    private static readonly FieldInfo TargetSideField = GetAttackCommandField("<TargetSide>k__BackingField");
    private static readonly FieldInfo IsRandomlyTargetedField = GetAttackCommandField("<IsRandomlyTargeted>k__BackingField");

    private string? _chosenMoveStateId;
    private Creature? _chosenMoveTarget;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override bool ShouldReceiveCombatHooks => true;

    PowerModel IRedirectPower.Power => this;
    Creature? IRedirectPower.ChosenMoveTarget => ChosenMoveTarget;

    internal Creature? ChosenMoveTarget => LymPower.IsValidMoveTarget(_chosenMoveTarget, RedirectTargetScope.Player) ? _chosenMoveTarget : null;

    public override async Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker != Owner) return;
        if (ChosenMoveTarget is not { } chosenMoveTarget) return;

        RedirectCommandTarget(command, chosenMoveTarget);
        await Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Enemy)
            await PowerCmd.Remove(this);
    }

    internal async Task ChooseMoveAndTarget(PlayerChoiceContext choiceContext, Player player)
    {
        _chosenMoveStateId = null;
        _chosenMoveTarget = null;

        if (!LymPower.IsLocalPlayer(player)) return;
        if (Owner is not { IsAlive: true }) return;

        // Step 1: Choose enemy move
        if (!await ChooseMove(choiceContext, player)) return;

        // Step 2: Choose friendly target
        _chosenMoveTarget = await ChooseLocalTarget();
        await RefreshOwnerIntent();
    }

    private async Task<bool> ChooseMove(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner.Monster is not { } monster) return false;

        var moves = RedirectMoveChoiceScreen.GetChoosableMoves(monster);
        var chosenMove = await RedirectMoveChoiceScreen.Choose(
            choiceContext,
            monster,
            moves,
            player,
            player.Character.CardPool);

        if (chosenMove is null) return false;

        _chosenMoveStateId = chosenMove.Value.Move.StateId;
        monster.SetMoveImmediate(chosenMove.Value.Move, forceTransition: true);
        return true;
    }

    private async Task<Creature?> ChooseLocalTarget()
    {
        var targetManager = NTargetManager.Instance;
        if (targetManager is null) return null;

        var ownerNode = Owner.GetCreatureNode();
        if (ownerNode is null) return null;

        targetManager.StartTargeting(
            TargetType.AnyPlayer,
            ownerNode,
            TargetMode.ClickMouseToTarget,
            () => Owner is not { IsAlive: true },
            IsAllowedTargetNode);

        return GetCreatureFromTargetNode(await targetManager.SelectionFinished());
    }

    private async Task RefreshOwnerIntent()
    {
        ApplyChosenMove();

        var ownerNode = Owner.GetCreatureNode();
        if (ownerNode is null) return;

        var targets = ChosenMoveTarget is { } chosenMoveTarget
            ? new[] { chosenMoveTarget }
            : Array.Empty<Creature>();

        await ownerNode.UpdateIntent(targets);
    }

    private void ApplyChosenMove()
    {
        if (string.IsNullOrWhiteSpace(_chosenMoveStateId)) return;
        if (Owner.Monster is not { } monster) return;

        var moveStateMachine = monster.MoveStateMachine;
        if (moveStateMachine is null) return;

        if (moveStateMachine.States.TryGetValue(_chosenMoveStateId, out var state) && state is MoveState move)
            monster.SetMoveImmediate(move, forceTransition: true);
    }

    private static void RedirectCommandTarget(AttackCommand command, Creature target)
    {
        SingleTargetField.SetValue(command, target);
        CombatStateField.SetValue(command, null);
        TargetSideField.SetValue(command, target.Side);
        IsRandomlyTargetedField.SetValue(command, false);
    }

    private static bool IsAllowedTargetNode(Node node)
        => LymPower.IsValidMoveTarget(GetCreatureFromTargetNode(node), RedirectTargetScope.Player);

    private static Creature? GetCreatureFromTargetNode(Node? node)
        => node is NCreature { Entity: { } creature } ? creature : null;

    private static FieldInfo GetAttackCommandField(string fieldName)
    {
        return typeof(AttackCommand).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(AttackCommand).FullName, fieldName);
    }
}
