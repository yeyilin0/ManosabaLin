using HarmonyLib;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace ManosabaLin.Patches;

// 锁定的旧识疑影：由卡片自身声明"不可被选择"，不再逐个 Patch CardSelectCmd.From* 入口。
// 浏览（查看弃牌堆/牌组）时卡正常可见，但任何选择操作都无法选中它。
// 改为 Patch 引擎选择系统的两个点击汇聚点（覆盖所有现有及未来新增的选卡入口）：
//   1. NCardGrid.OnHolderPressed —— 网格中所有卡片的 Pressed 信号都汇聚到这里，再由它
//      emit HolderPressed 通知选择屏幕。Prefix 拦截锁定卡的单击（返回 false），使其
//      可见但无法被选中；右键查看详情（HolderAltPressed）不受影响。
//   2. NChooseACardSelectionScreen.SelectHolder —— 非网格的"从给定列表选卡"屏幕
//      （奖励/事件类）的点击确认入口，拦截对锁定卡的选择。
// 过滤判定由卡片自带：SamePlaceTruth.IsSelectionLocked（card is SamePlaceTruth && LockedInDiscard）。
[HarmonyPatch]
internal static class SamePlaceTruthSelectionLockPatch
{
    [HarmonyPatch(typeof(NCardGrid), "OnHolderPressed")]
    [HarmonyPrefix]
    private static bool OnHolderPressedPrefix(NCardHolder holder)
    {
        return !SamePlaceTruth.IsSelectionLocked(holder.CardModel);
    }

    [HarmonyPatch(typeof(NChooseACardSelectionScreen), "SelectHolder")]
    [HarmonyPrefix]
    private static bool SelectHolderPrefix(NCardHolder cardHolder)
    {
        // 注意：Harmony 按参数名匹配原方法参数，v0.111.0 中
        // SelectHolder 的参数名为 cardHolder（此前为 holder）。
        return !SamePlaceTruth.IsSelectionLocked(cardHolder.CardModel);
    }
}

// 打出封锁（与选卡无关）：锁定中的旧识疑影不可手动打出。
// CardModel.CanPlay 等是引擎非虚方法，无法在卡上 override，只能 Patch 引擎打出判定点。
[HarmonyPatch]
internal static class SamePlaceTruthCanPlayLockPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.CanPlay),
            [typeof(UnplayableReason).MakeByRefType(), typeof(AbstractModel).MakeByRefType()]);
    }

    [HarmonyPostfix]
    private static void Postfix(
        CardModel __instance,
        ref bool __result,
        ref UnplayableReason reason,
        ref AbstractModel? preventer)
    {
        if (!SamePlaceTruth.IsSelectionLocked(__instance))
        {
            return;
        }

        __result = false;
        reason |= UnplayableReason.BlockedByCardLogic;
        preventer ??= __instance;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlay), [])]
internal static class SamePlaceTruthCanPlayNoOutLockPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref bool __result)
    {
        if (SamePlaceTruth.IsSelectionLocked(__instance))
        {
            __result = false;
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlayTargeting), [typeof(Creature)])]
internal static class SamePlaceTruthCanPlayTargetingLockPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref bool __result)
    {
        if (SamePlaceTruth.IsSelectionLocked(__instance))
        {
            __result = false;
        }
    }
}
