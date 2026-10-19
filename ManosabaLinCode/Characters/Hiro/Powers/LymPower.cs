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

    internal static Creature? GetRedirectChosenMoveTarget(Creature? owner, RedirectTargetScope? scope = null)
    {
        if (owner is not { IsAlive: true }) return null;
        return owner.Powers
            .OfType<IRedirectPower>()
            .Select(p => p.ChosenMoveTarget)
            .FirstOrDefault(t => t is not null && (scope is null || IsValidMoveTarget(t, scope.Value)));
    }

    private async Task RefreshOwnerIntentTarget()
    {
        if (ChosenMoveTarget is not { } chosenMoveTarget) return;
        if (Owner is not { IsAlive: true, IsMonster: true }) return;
        if (!CombatManager.Instance.IsInProgress) return;

        // 意图刷新是纯表现，但 NCreature.UpdateIntent 会新建/重排 NIntent 子节点，必须在主线程跑。
        // 打牌的 async 续延整条链可能落在线程池线程上（godot.log 里 PlayCardAction 的栈底就是
        // ExecutionContext.RunInternal(ThreadPoolThread)）：离开主线程时 AddChildSafely 会把入队
        // 改走 CallDeferred，而紧随其后的 nIntent.UpdateIntent 已同步打到尚未 _Ready 的节点上，
        // _intentParticle/_valueLabel 还是 null ⇒ NRE，把整张打牌以 AggregateException 炸掉。
        // 续延具体落在哪个线程取决于 await 的交错，是竞态：实机日志里未完稿（已记录蕾雅魔法）
        // 就这样炸过一次，OnPlay 中断 ⇒ 卡片留在屏幕上、完稿也不生成。
        // 所以把解析与刷新一起派发回主线程，并按引擎同口径 try/catch 兜底
        // （引擎 PowerCmd.ModifyAmount 里刷意图时就 catch 了 ObjectDisposedException）：
        // 表现失败绝不应该中断打牌。
        var owner = Owner;
        Callable.From(() =>
        {
            try
            {
                if (owner is not { IsAlive: true }) return;
                var ownerNode = owner.GetCreatureNode();
                if (ownerNode is null || !GodotObject.IsInstanceValid(ownerNode)) return;
                ownerNode.UpdateIntent(new[] { chosenMoveTarget });
            }
            catch (Exception ex)
            {
                Log.Error($"LymPower intent refresh failed: {ex}");
            }
        }).CallDeferred();

        await Task.CompletedTask;
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
