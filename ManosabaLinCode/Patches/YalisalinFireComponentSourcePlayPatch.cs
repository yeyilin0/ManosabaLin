using HarmonyLib;
using ManosabaLin.Characters.Yalisalin.Components;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using System.Threading.Tasks;

namespace ManosabaLin.Patches;

/// <summary>
///     余火源牌的「手动打出」按玩家的设计口径结算：<b>不消耗能量、不算卡牌打出</b>。
///     <para>
///         触发链路：<c>PlayCardAction.ExecuteAction</c> → <c>CardModel.SpendResources</c>（扣费）
///         → <c>CardModel.OnPlayWrapper</c>（<c>Hook.BeforeCardPlayed</c> →
///         <c>CombatHistory.CardPlayStarted</c> → <c>OnPlay</c> → 余火 resolver）。
///         余火 resolver 跑在 <c>OnPlay</c> 里，扣费与历史记录都早于它，无法事后挽回，
///         只能从这三个源头拦截。
///     </para>
///     <para>
///         判据 <see cref="YalisalinFireComponentRules.CardModifiers" />（类本身/组件/capability 三路全覆盖）：
///         任何带余火的牌手动打出时免费 —— 类本身实现接口的（不需要的善意等）、默认自带 capability 的
///         （坏掉的信任/静止也会痛），以及被「请别看见我/坏掉的信任」添上余火的普通牌都算。
///         自动打出（引擎 <c>CardCmd.AutoPlay</c> 口径，本就 EnergySpent=0）与
///         「余火选择后自动打出的所选牌」（resolver 的 free=false 分支，真扣费）都不受影响
///         —— 所选牌才算打出并消耗能量。
///     </para>
///     <para>
///         ⚠️ 2026-10-19 用户实测回归：判据原来只写 <c>card is IYalisalinFireComponentModifier</c>，
///         漏掉 capability 一路 —— 被添火的普通打击源牌费用照扣，选张牌总共花两费。
///     </para>
///     <para>
///         <c>Hook.BeforeCardPlayed</c> / <c>Hook.AfterCardPlayed</c> 成对拦截，
///         让「打出前/后」类能力（Normality 上限、Feral、各类打出后触发）都不为源牌计数；
///         两者实现上都只是往 hook listeners 分发，没有引擎硬状态依赖。
///         所选牌与被烧牌的自动打出走各自的 isAutoPlay 路径，不受影响。
///     </para>
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]
internal static class YalisalinFireComponentSourcePlayPatch
{
    [HarmonyPrefix]
    private static bool SpendResourcesPrefix(CardModel __instance, ref Task<(int, int)> __result)
    {
        if (!IsManualFireComponentSourceCard(__instance))
            return true;

        // X 费源牌（capability 挂载后任意牌都可能带余火）仍需捕获 X 值，
        // 与 CardCmd.AutoPlay 的免费口径一致（取当前能量），否则 X 牌效果按 0 结算。
        if (__instance.EnergyCost.CostsX)
            __instance.EnergyCost.CapturedXValue = __instance.Owner.PlayerCombatState?.Energy ?? 0;

        __result = Task.FromResult((0, 0));
        return false;
    }

    internal static bool IsManualFireComponentSourceCard(CardModel card)
    {
        // 必须走 CardModifiers 三路（类本身/组件/capability）：被添火的普通牌与
        // 默认自带 capability 的卡（坏掉的信任/静止也会痛）类本身不实现接口，
        // 只用 `card is ...` 会漏掉（2026-10-19 用户实测：源牌费用照扣）。
        return YalisalinFireComponentRules.CardModifiers(card).Any()
            && !YalisalinFireComponentResolver.SuppressedCards.Contains(card);
    }

    internal static bool IsManualFireComponentSourcePlay(CardPlay cardPlay)
    {
        return !cardPlay.IsAutoPlay
            && YalisalinFireComponentRules.CardModifiers(cardPlay.Card).Any();
    }
}

/// <summary>不算卡牌打出：余火源牌手动打出时不进战斗历史（CardPlaysStarted 查询不到）。</summary>
[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayStarted))]
internal static class YalisalinFireComponentCardPlayStartedPatch
{
    [HarmonyPrefix]
    private static bool Prefix(CardPlay cardPlay)
    {
        return !YalisalinFireComponentSourcePlayPatch.IsManualFireComponentSourcePlay(cardPlay);
    }
}

/// <summary>与 <see cref="YalisalinFireComponentCardPlayStartedPatch" /> 对称：不记收尾。</summary>
[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayFinished))]
internal static class YalisalinFireComponentCardPlayFinishedPatch
{
    [HarmonyPrefix]
    private static bool Prefix(CardPlay cardPlay)
    {
        return !YalisalinFireComponentSourcePlayPatch.IsManualFireComponentSourcePlay(cardPlay);
    }
}

/// <summary>
///     不算卡牌打出：「每打出 N 张」类能力（Normality 上限、Feral 等）不为余火源牌计数/触发。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
internal static class YalisalinFireComponentBeforeCardPlayedPatch
{
    [HarmonyPrefix]
    private static bool Prefix(CardPlay cardPlay, ref Task __result)
    {
        if (!YalisalinFireComponentSourcePlayPatch.IsManualFireComponentSourcePlay(cardPlay))
            return true;

        __result = Task.CompletedTask;
        return false;
    }
}

/// <summary>
///     不算卡牌打出（打出后一侧）：「打出后」类能力/遗物同样不为余火源牌触发。
///     引擎原注释说它承担「完成致死的卡的结算」，但看实现只是往
///     <c>combatState.IterateHookListeners()</c> 分发 AfterCardPlayed/AfterCardPlayedLate，
///     没有引擎硬状态依赖；所选牌与被烧的自动打出走各自的 isAutoPlay 路径，不受影响。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
internal static class YalisalinFireComponentAfterCardPlayedPatch
{
    [HarmonyPrefix]
    private static bool Prefix(CardPlay cardPlay, ref Task __result)
    {
        if (!YalisalinFireComponentSourcePlayPatch.IsManualFireComponentSourcePlay(cardPlay))
            return true;

        __result = Task.CompletedTask;
        return false;
    }
}
