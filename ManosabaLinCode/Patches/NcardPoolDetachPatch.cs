using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Pooling;

namespace ManosabaLin.Patches;

/// <summary>
///     NCard 节点池取出时确保节点已脱离旧父节点。
///     <para>
///     引擎 <see cref="NodePool{T}"/> 复用的是已挂树的 NCard（<c>NCard.OnFreedToPool</c>
///     不摘除 parent），而 <c>CardModel.PlayPowerCardFlyVfx</c> 与
///     <c>NCardPlayQueue.OnActionEnqueued</c> 等路径会把池取出的节点直接
///     <c>AddChildSafely</c> 到 CombatVfxContainer / PlayQueue —— Godot 的 add_child
///     对已有 parent 的节点直接报 <c>Can't add child ... already has a parent</c>
///     且不加入，表现层静默丢失（实机 godot.log 里 3 次，多人客户端频繁触发）。
///     池的语义本就是「取出即可自由挂载」，所以在这里统一摘除。
///     </para>
///     <para>
///     ⚠️ 只在主线程摘除（Godot 节点操作限制）；非主线程的异步竞态路径维持原状
///     （那些路径引擎自己会走 CallDeferred，属罕见竞态、无功能实害）。
///     </para>
/// </summary>
[HarmonyPatch(typeof(NodePool<NCard>), nameof(NodePool<NCard>.Get))]
internal static class NcardPoolDetachPatch
{
    private static void Postfix(NCard __result)
    {
        var parent = __result.GetParent();
        if (parent is not null && NGame.IsMainThread())
        {
            parent.RemoveChild(__result);
        }
    }
}
