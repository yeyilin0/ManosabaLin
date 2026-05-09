using System.Reflection;
using System.Threading;
using Godot;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.ManosabaLinCode.Characters.Hiro.Powers;

[RegisterPower]
public class LymPower : ManosabaPowerTemplate
{
    private static readonly FieldInfo SingleTargetField = GetAttackCommandField("_singleTarget");
    private static readonly FieldInfo CombatStateField = GetAttackCommandField("_combatState");
    private static readonly FieldInfo TargetSideField = GetAttackCommandField("<TargetSide>k__BackingField");
    private static readonly FieldInfo IsRandomlyTargetedField = GetAttackCommandField("<IsRandomlyTargeted>k__BackingField");

    private static readonly AsyncLocal<Creature?> CurrentOwnerContext = new();

    private Creature? _chosenMoveTarget;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override bool ShouldReceiveCombatHooks => true;

    internal Creature? ChosenMoveTarget => IsValidMoveTarget(_chosenMoveTarget) ? _chosenMoveTarget : null;

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

    internal async Task ChooseMoveTarget(PlayerChoiceContext choiceContext)
    {
        var targetManager = NTargetManager.Instance;
        if (targetManager is null) return;

        _chosenMoveTarget = null;
        await choiceContext.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);

        try
        {
            var ownerNode = Owner.GetCreatureNode();
            if (ownerNode is null) return;

            targetManager.StartTargeting(
                TargetType.AnyEnemy,
                ownerNode,
                TargetMode.ClickMouseToTarget,
                () => Owner is not { IsAlive: true },
                IsAllowedTargetNode);

            _chosenMoveTarget = GetCreatureFromTargetNode(await targetManager.SelectionFinished());
            await RefreshOwnerIntentTarget();
        }
        finally
        {
            await choiceContext.SignalPlayerChoiceEnded();
        }
    }

    internal static Creature? GetChosenMoveTarget(Creature? owner)
    {
        if (owner is not { IsAlive: true }) return null;

        return owner.Powers
            .OfType<LymPower>()
            .Select(p => p.ChosenMoveTarget)
            .FirstOrDefault(t => t is not null);
    }

    internal static Creature? GetCurrentOwner() => CurrentOwnerContext.Value;

    internal static void SetCurrentOwner(Creature? owner) => CurrentOwnerContext.Value = owner;

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
        => IsValidMoveTarget(GetCreatureFromTargetNode(node));

    private static Creature? GetCreatureFromTargetNode(Node? node)
        => node is NCreature { Entity: { } creature } ? creature : null;

    private static bool IsValidMoveTarget(Creature? creature)
        => creature is { IsEnemy: true, IsAlive: true, IsHittable: true };

    private static FieldInfo GetAttackCommandField(string fieldName)
    {
        return typeof(AttackCommand).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(AttackCommand).FullName, fieldName);
    }
}