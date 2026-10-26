using HarmonyLib;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Ema.Cards;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Patches;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Patches;

/// <summary>
///     花朵绽放的「变形 / 生成」拦截补丁。
///     <para>
///         引擎没有任何「变形 / 生成前置替换」钩子（RitsuLib 的 <c>ModCardTransformRegistry</c>
///         只能在变形完成后收到通知，无法改变替换目标），因此这里对两个命令级入口做前缀拦截：
///     </para>
///     <para>· <see cref="CardCmd.Transform" /> —— 拦截变形，可改为艾玛的 1 费消耗【疏远】牌；</para>
///     <para>· <see cref="CardPileCmd.AddGeneratedCardsToCombat" /> —— 拦截生成，可改为艾玛的 1 费消耗【亲近】牌。</para>
///     <para>
///         两个入口都是 async 方法：前缀无法 await，因此前缀只负责「把返回值换成自己启动的任务并跳过原方法」，
///         真正的玩家选择在返回的任务里 await；补丁内部再次调用原命令时用
///         <see cref="FlowerBloomTracker.Intercepting" /> 递归保护放行。
///     </para>
/// </summary>
[HarmonyPatch]
internal static class FlowerBloomInterceptPatches
{
    private const string ContinueKey = FlowerBloom.LocEntry + ".continueTitle";
    private const string TransformPromptKey = FlowerBloom.LocEntry + ".transformPrompt";
    private const string TransformReplaceKey = FlowerBloom.LocEntry + ".transformSubstituteTitle";
    private const string GeneratePromptKey = FlowerBloom.LocEntry + ".generatePrompt";
    private const string GenerateReplaceKey = FlowerBloom.LocEntry + ".generateSubstituteTitle";

    private static LocString Loc(string key) => new("cards", key);

    // ---------------------------------------------------------------- 变形拦截

    [HarmonyPatch(
        typeof(CardCmd),
        nameof(CardCmd.Transform),
        [typeof(IEnumerable<CardTransformation>), typeof(Rng), typeof(CardPreviewStyle)])]
    [HarmonyPrefix]
    private static bool TransformPrefix(
        IEnumerable<CardTransformation> transformations,
        Rng? rng,
        CardPreviewStyle style,
        ref Task<IEnumerable<CardPileAddResult>> __result)
    {
        if (!FlowerBloomTracker.Any || FlowerBloomTracker.Intercepting) return true;

        var list = transformations as IReadOnlyList<CardTransformation> ?? transformations.ToArray();
        if (list.Count == 0) return true;
        if (!list.Any(static t => FlowerBloomTracker.IsWatched(t.Original.Owner?.Creature))) return true;

        __result = InterceptTransformAsync(list, rng, style);
        return false;
    }

    private static async Task<IEnumerable<CardPileAddResult>> InterceptTransformAsync(
        IReadOnlyList<CardTransformation> transformations,
        Rng? rng,
        CardPreviewStyle style)
    {
        var context = CardPlayContext.Current;
        var rebuilt = new List<CardTransformation>(transformations.Count);

        foreach (var transformation in transformations)
        {
            var original = transformation.Original;
            var owner = original.Owner;

            if (context is null || owner is null || !FlowerBloomTracker.IsWatched(owner.Creature)
                || !IsSafeContext(context))
            {
                rebuilt.Add(transformation);
                continue;
            }

            // 选「继续执行」= true（左），选「改为疏远牌」= false（右）
            var keepOriginal = await YesNoChoiceScreen.Pick(
                context, owner, Loc(TransformPromptKey), Loc(ContinueKey), Loc(TransformReplaceKey));

            var replacement = keepOriginal
                ? null
                : CreateBudgetBondCard(original.CombatState ?? owner.Creature.CombatState, owner, affinity: false);

            rebuilt.Add(replacement is null ? transformation : new CardTransformation(original, replacement));
        }

        FlowerBloomTracker.Intercepting = true;
        try
        {
            return await CardCmd.Transform(rebuilt, rng, style);
        }
        finally
        {
            FlowerBloomTracker.Intercepting = false;
        }
    }

    // ---------------------------------------------------------------- 生成拦截

    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardsToCombat))]
    [HarmonyPrefix]
    private static bool GeneratePrefix(
        IEnumerable<CardModel> cards,
        PileType newPileType,
        Player? creator,
        CardPilePosition position,
        ref Task<IReadOnlyList<CardPileAddResult>> __result)
    {
        if (!FlowerBloomTracker.Any || FlowerBloomTracker.Intercepting) return true;

        if (creator is not { } player) return true;
        if (!FlowerBloomTracker.IsWatched(player.Creature)) return true;

        // 只在卡牌结算过程中拦截：此时才有可暂停动作队列的选择上下文，
        // 也避免回合开始 / 遗物发牌等时机弹出选择界面把流程打断。
        // ⚠️ 还必须验证该上下文属于「当前正在执行的动作」：引擎的
        // SignalPlayerChoiceBegun 在 CurrentlyRunningAction 不匹配时只会打日志并跳过暂停，
        // 此时选择界面照样弹出，而随后的 SignalPlayerChoiceEnded 必然 NRE
        // （2026-10-19 联机实测：技能药水生成卡时读到别的动作残留的 context）。
        if (CardPlayContext.Current is not { } currentContext || !IsSafeContext(currentContext))
            return true;

        var list = cards as IReadOnlyList<CardModel> ?? cards.ToArray();
        if (list.Count == 0) return true;

        __result = InterceptGenerateAsync(list, newPileType, player, position);
        return false;
    }

    private static async Task<IReadOnlyList<CardPileAddResult>> InterceptGenerateAsync(
        IReadOnlyList<CardModel> cards,
        PileType newPileType,
        Player creator,
        CardPilePosition position)
    {
        var context = CardPlayContext.Current;
        var final = new List<CardModel>(cards);

        if (context is not null)
        {
            var keepOriginal = await YesNoChoiceScreen.Pick(
                context, creator, Loc(GeneratePromptKey), Loc(ContinueKey), Loc(GenerateReplaceKey));

            if (!keepOriginal)
            {
                var combatState = creator.Creature.CombatState;
                var replaced = new List<CardModel>(cards.Count);
                foreach (var original in cards)
                    replaced.Add(CreateBudgetBondCard(combatState, original.Owner ?? creator, affinity: true)
                                 ?? original);

                final = replaced;
            }
        }

        FlowerBloomTracker.Intercepting = true;
        try
        {
            return await CardPileCmd.AddGeneratedCardsToCombat(final, newPileType, creator, position);
        }
        finally
        {
            FlowerBloomTracker.Intercepting = false;
        }
    }

    // ---------------------------------------------------------------- 替换卡构造

    /// <summary>
    ///     该上下文是否真的可以安全地弹出玩家选择。
    ///     <para>
    ///         引擎 <see cref="GameActionPlayerChoiceContext.SignalPlayerChoiceBegun" /> 要求
    ///         <see cref="ActionExecutor.CurrentlyRunningAction" /> 正是该上下文所属的动作，
    ///         否则只打一条 Error 日志并跳过 <c>PauseActionForPlayerChoice</c>——此时选择界面
    ///         照样弹出，而随后的 <c>SignalPlayerChoiceEnded</c> 必然 NRE。
    ///     </para>
    ///     <para>
    ///         宁可安静放行（本次不拦截），也不能把动作队列拖崩。
    ///         非联机 / 无执行器时（单机或测试桩）无法核对，按放行处理。
    ///     </para>
    /// </summary>
    private static bool IsSafeContext(PlayerChoiceContext context)
    {
        if (context is not GameActionPlayerChoiceContext actionContext) return false;

        var executor = RunManager.Instance.ActionExecutor;
        return executor is null || ReferenceEquals(executor.CurrentlyRunningAction, actionContext.Action);
    }

    /// <summary>
    ///     构造艾玛的 1 费消耗【亲近】/【疏远】牌：无论原卡多少费，生成后费用一律调整为 1，并获得【消耗】。
    /// </summary>
    private static CardModel? CreateBudgetBondCard(ICombatState? combatState, Player owner, bool affinity)
    {
        if (combatState is null) return null;

        CardModel card = affinity
            ? combatState.CreateCard<Lyqinjin>(owner)
            : combatState.CreateCard<Lyshuyuan>(owner);

        card.EnergyCost.SetThisCombat(1);
        card.AddKeyword(CardKeyword.Exhaust);
        return card;
    }
}
