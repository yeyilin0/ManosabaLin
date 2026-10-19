using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace ManosabaLin.Characters.Common.GameActions;

/// <summary>
///     强制结束某个玩家本回合的出牌阶段（不可撤销）。
///
///     与引擎的 <see cref="MegaCrit.Sts2.Core.GameActions.EndPlayerTurnAction" /> 的唯一区别：
///     <see cref="PlayerCmd.EndTurn" /> 传入 <c>canBackOut: false</c>，玩家不能把它撤销回「可操作」状态。
///     三水情报本回合满 5 层是 boss 的强制惩罚机制，可撤销会让机制失效，故必须用这一版。
///
///     ⭐ 为什么必须走 ActionQueue：
///     触发点在 <c>AfterPowerAmountChanged</c>，而该 hook 是由 <c>PowerCmd.Apply</c> 内部一个受**本机设置**
///     （<c>PrefsSave.FastMode</c>）影响的 <c>CustomScaledWait</c> 之后恢复执行的，且调用方是 fire-and-forget
///     （<c>_ = PowerCmd.Apply(...)</c>）⇒ 它**完全脱离 ActionQueue 的执行栈**，各端恢复时刻不同。
///     此时直接调 <c>PlayerCmd.EndTurn</c>（内部只改 <c>CombatManager.PlayersReadyToEndTurn</c> 集合并可能立即
///     触发回合结束信号）会让某端在「回合已结束」时另一端的出牌仍在队列里结算 ⇒ 状态分歧 ⇒ 掉线。
///     入队后由 host 权威广播顺序，两端队列一致。
/// </summary>
public sealed class ForceEndPlayerTurnAction(Player player, int turnNumber) : GameAction
{
    protected override Task ExecuteAction()
    {
        var currentTurnNumber = player.PlayerCombatState.TurnNumber;
        if (currentTurnNumber == turnNumber)
        {
            PlayerCmd.EndTurn(player, false);
        }
        else
        {
            Log.Info($"Ignoring forced end turn action. Current turn number: {currentTurnNumber} action turn number: {turnNumber}");
        }

        return Task.CompletedTask;
    }

    public override INetAction ToNetAction()
    {
        return new NetForceEndPlayerTurnAction { TurnNumber = turnNumber };
    }

    public override ulong OwnerId => player.NetId;

    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    public override string ToString()
    {
        return $"{"ForceEndPlayerTurnAction"} for player {player.NetId} turn {turnNumber}";
    }
}
