using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace ManosabaLin.Patches;

/// <summary>
///     当前正在结算的卡牌所使用的玩家选择上下文。
///     <para>
///         引擎的 <c>CardCmd.Transform</c> / <c>CardPileCmd.AddGeneratedCardsToCombat</c> 都不接收
///         选择上下文，而「花朵绽放」需要在变形 / 生成的那一刻弹出玩家选择界面。
///         这里在 <see cref="CardModel.OnPlayWrapper" /> 前后登记上下文，
///         使补丁能在卡牌结算过程中取回正确的、可暂停动作队列的上下文。
///     </para>
///     <para>
///         ⚠️ 存储必须是 <see cref="AsyncLocal{T}" /> 而不能是进程级 static：联机时多个玩家的
///         卡牌结算在同一进程内并发，static 会互相污染（2026-10-19 实测：宿主用药水期间读到
///         另一名玩家打牌残留的 context，用它弹选择 ⇒ 引擎
///         <c>SignalPlayerChoiceBegun</c> 拒绝暂停（CurrentlyRunningAction 不匹配）但 UI 已弹出，
///         随后 <c>SignalPlayerChoiceEnded</c> 必 NRE）。
///         AsyncLocal 按 ExecutionContext 隔离：前缀赋值发生在 OnPlayWrapper 状态机
///         <c>Start</c> 捕获上下文之前 ⇒ 整个结算期（含所有 await 续体，哪怕续延到线程池线程）
///         都读到自己的 context；而后缀在调用方流里同步还原，嵌套结算也能正确回退。
///     </para>
/// </summary>
public static class CardPlayContext
{
    private static readonly AsyncLocal<PlayerChoiceContext?> CurrentContext = new();

    /// <summary>当前异步流正在结算的卡牌选择上下文；不在卡牌结算过程中时为 null。</summary>
    public static PlayerChoiceContext? Current
    {
        get => CurrentContext.Value;
        private set => CurrentContext.Value = value;
    }

    internal static void Set(PlayerChoiceContext? context) => Current = context;
}

/// <summary>
///     在卡牌结算期间登记 / 还原 <see cref="CardPlayContext" />。
///     <para>
///         <see cref="CardModel.OnPlayWrapper" /> 是 async 方法，前缀在状态机启动前运行：
///         前缀赋值会落入状态机 <c>Start</c> 时捕获的 ExecutionContext，因此整个结算期
///         （含 await 续体）都能读到新值；后缀则只需在调用方流里同步还原即可，
///         不需要再把返回 Task 包一层续体（旧实现的续体还原会落到执行流上，反而改不到调用方流）。
///         嵌套结算（自动打出）依靠保存 / 还原上一份值来正确回退。
///     </para>
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class CardPlayContextPatch
{
    [HarmonyPrefix]
    private static void Prefix(PlayerChoiceContext choiceContext, out PlayerChoiceContext? __state)
    {
        __state = CardPlayContext.Current;
        CardPlayContext.Set(choiceContext);
    }

    [HarmonyPostfix]
    private static void Postfix(PlayerChoiceContext? __state)
    {
        CardPlayContext.Set(__state);
    }
}
