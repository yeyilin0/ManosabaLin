using System.Reflection;
using Godot;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Common.GameActions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Runs;

namespace ManosabaLin.ManosabaLinCode.Characters.Hiro.Powers;

internal enum RedirectTargetScope
{
    Enemy,
    Player
}

internal interface IRedirectPower
{
    PowerModel Power { get; }
    Creature? ChosenMoveTarget { get; }
}

[RegisterPower]
public class LymPower : ManosabaPowerTemplate, IRedirectPower
{
    private static readonly FieldInfo SingleTargetField = GetAttackCommandField("_singleTarget");
    private static readonly FieldInfo CombatStateField = GetAttackCommandField("_combatState");
    private static readonly FieldInfo TargetSideField = GetAttackCommandField("<TargetSide>k__BackingField");
    private static readonly FieldInfo IsRandomlyTargetedField = GetAttackCommandField("<IsRandomlyTargeted>k__BackingField");

    private Creature? _chosenMoveTarget;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override bool ShouldReceiveCombatHooks => true;

    PowerModel IRedirectPower.Power => this;
    Creature? IRedirectPower.ChosenMoveTarget => ChosenMoveTarget;

    internal Creature? ChosenMoveTarget => IsValidMoveTarget(_chosenMoveTarget, RedirectTargetScope.Enemy) ? _chosenMoveTarget : null;

    public override async Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker != Owner) return;
        if (ChosenMoveTarget is not { } chosenMoveTarget) return;

        RedirectCommandTarget(command, chosenMoveTarget);
        await Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy)
            await PowerCmd.Remove(this);
    }

    internal void ChooseMoveTarget(PlayerChoiceContext choiceContext, Player player)
    {
        _chosenMoveTarget = null;

        if (!IsLocalPlayer(player)) return;

        TaskHelper.RunSafely(ChooseMoveTargetLocal(player));
    }

    private async Task ChooseMoveTargetLocal(Player player)
    {
        var target = await ChooseLocalTarget(TargetType.AnyEnemy);
        if (target is null) return;
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new LymPowerChosenAction(player, Owner, target));
    }

    public async Task HandleGameAction(Creature target)
    {
        _chosenMoveTarget = target;
        await RefreshOwnerIntentTarget();
    }

    private async Task<Creature?> ChooseLocalTarget(TargetType targetType)
    {
        var targetManager = NTargetManager.Instance;
        if (targetManager is null) return null;

        var ownerNode = Owner.GetCreatureNode();
        if (ownerNode is null) return null;

        targetManager.StartTargeting(
            targetType,
            ownerNode,
            TargetMode.ClickMouseToTarget,
            () => Owner is not { IsAlive: true },
            IsAllowedTargetNode);

        return GetCreatureFromTargetNode(await targetManager.SelectionFinished());
    }

    internal static bool IsLocalPlayer(Player player)
    {
        return LocalContext.IsMe(player);
    }

    internal static Creature? GetRedirectChosenMoveTarget(Creature? owner)
    {
        if (owner is not { IsAlive: true }) return null;
        return owner.Powers
            .OfType<IRedirectPower>()
            .Select(p => p.ChosenMoveTarget)
            .FirstOrDefault(t => t is not null);
    }

    private async Task RefreshOwnerIntentTarget()
    {
        if (ChosenMoveTarget is not { } chosenMoveTarget) return;

        var ownerNode = Owner.GetCreatureNode();
        if (ownerNode is null) return;

        await ownerNode.UpdateIntent(new[] { chosenMoveTarget });
    }

    private static void RedirectCommandTarget(AttackCommand command, Creature target)
    {
        SingleTargetField.SetValue(command, target);
        CombatStateField.SetValue(command, null);
        TargetSideField.SetValue(command, target.Side);
        IsRandomlyTargetedField.SetValue(command, false);
    }

    private static bool IsAllowedTargetNode(Node node)
        => IsValidMoveTarget(GetCreatureFromTargetNode(node), RedirectTargetScope.Enemy);

    private static Creature? GetCreatureFromTargetNode(Node? node)
        => node is NCreature { Entity: { } creature } ? creature : null;

    internal static bool IsValidMoveTarget(Creature? creature, RedirectTargetScope scope)
    {
        if (creature is not { IsAlive: true, IsHittable: true }) return false;
        return scope switch
        {
            RedirectTargetScope.Enemy => creature.IsEnemy,
            RedirectTargetScope.Player => creature is { Side: CombatSide.Player, IsPlayer: true },
            _ => false
        };
    }

    private static FieldInfo GetAttackCommandField(string fieldName)
    {
        return typeof(AttackCommand).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(AttackCommand).FullName, fieldName);
    }
}
