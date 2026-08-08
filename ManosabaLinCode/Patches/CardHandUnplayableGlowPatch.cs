using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using System.Collections.Concurrent;
using System.Reflection;

namespace ManosabaLin.Patches;

[HarmonyPatch(typeof(NHandCardHolder))]
internal static class CardHandUnplayableGlowPatch
{
    private const string ManosabaCardIdPrefix = "MANOSABA_LIN_CARD_";
    private static readonly FieldInfo? FlashTweenField = AccessTools.Field(typeof(NHandCardHolder), "_flashTween");
    private static readonly ConcurrentDictionary<ulong, CancellationTokenSource> TokensByHolderId = new();

    [HarmonyPatch(nameof(NHandCardHolder._Ready))]
    [HarmonyPostfix]
    private static void ReadyPostfix(NHandCardHolder __instance)
    {
        if (!GodotObject.IsInstanceValid(__instance) || !__instance.IsInsideTree() || __instance.GetTree() == null)
            return;

        var id = __instance.GetInstanceId();
        if (!TokensByHolderId.TryAdd(id, new()))
            return;

        var cts = TokensByHolderId[id];
        TaskHelper.RunSafely(RunHideLoop(__instance, id, cts.Token));
    }

    [HarmonyPatch(nameof(NHandCardHolder._ExitTree))]
    [HarmonyPrefix]
    private static void ExitTreePrefix(NHandCardHolder __instance)
    {
        StopLoop(__instance.GetInstanceId());
    }

    [HarmonyPatch(nameof(NHandCardHolder.UpdateCard))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void UpdateCardPostfix(NHandCardHolder __instance)
    {
        if (ShouldSuppressGlow(__instance))
            HideGlow(__instance);
    }

    [HarmonyPatch(nameof(NHandCardHolder.Flash))]
    [HarmonyPrefix]
    private static bool FlashPrefix(NHandCardHolder __instance)
    {
        if (!ShouldSuppressGlow(__instance))
            return true;

        HideGlow(__instance);
        return false;
    }

    [HarmonyPatch(nameof(NHandCardHolder.Flash))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void FlashPostfix(NHandCardHolder __instance)
    {
        if (ShouldSuppressGlow(__instance))
            HideGlow(__instance);
    }

    private static bool ShouldSuppressGlow(NHandCardHolder holder)
    {
        if (!TryGetCard(holder, out var card))
            return false;

        return IsManosabaCard(card) && !card.CanPlay();
    }

    private static bool TryGetCard(NHandCardHolder holder, out CardModel card)
    {
        card = null!;

        try
        {
            if (!GodotObject.IsInstanceValid(holder) ||
                holder.CardNode is not { } cardNode ||
                !GodotObject.IsInstanceValid(cardNode) ||
                cardNode.Model is not { } model)
                return false;

            card = model;
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private static bool IsManosabaCard(CardModel card)
    {
        return card.Id.Entry.StartsWith(ManosabaCardIdPrefix, StringComparison.Ordinal);
    }

    private static void HideGlow(NHandCardHolder holder)
    {
        HideHighlight(holder);
        HideFlash(holder);
    }

    private static void HideHighlight(NHandCardHolder holder)
    {
        try
        {
            var highlight = holder.CardNode?.CardHighlight;
            if (highlight == null || !GodotObject.IsInstanceValid(highlight))
                return;

            highlight.AnimHideInstantly();
            var color = highlight.Modulate;
            highlight.Modulate = new Color(color.R, color.G, color.B, 0f);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static void HideFlash(NHandCardHolder holder)
    {
        try
        {
            if (FlashTweenField?.GetValue(holder) is Tween tween &&
                GodotObject.IsInstanceValid(tween))
                tween.Kill();

            var flash = holder.GetNodeOrNull<Control>("Flash");
            if (flash == null || !GodotObject.IsInstanceValid(flash))
                return;

            var color = flash.Modulate;
            flash.Modulate = new Color(color.R, color.G, color.B, 0f);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static async Task RunHideLoop(NHandCardHolder holder, ulong id, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && GodotObject.IsInstanceValid(holder))
            {
                if (!holder.IsInsideTree())
                    break;

                if (ShouldSuppressGlow(holder))
                    HideGlow(holder);

                var tree = holder.GetTree();
                if (tree == null || !GodotObject.IsInstanceValid(tree))
                    break;

                await AwaitProcessFrameAsync(tree, holder, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            StopLoop(id);
        }
    }

    private static void StopLoop(ulong id)
    {
        if (!TokensByHolderId.TryRemove(id, out var cts))
            return;

        cts.Cancel();
        cts.Dispose();
    }

    private static async Task AwaitProcessFrameAsync(SceneTree? tree, GodotObject owner, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        ThrowIfInvalid(owner, token);

        if (tree == null || !GodotObject.IsInstanceValid(tree))
        {
            await Task.Yield();
            token.ThrowIfCancellationRequested();
            ThrowIfInvalid(owner, token);
            return;
        }

        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = token.CanBeCanceled
            ? token.Register(static state => ((TaskCompletionSource)state!).TrySetCanceled(), source)
            : default;

        Callable.From(() =>
        {
            try
            {
                if (token.IsCancellationRequested || !GodotObject.IsInstanceValid(owner))
                {
                    source.TrySetCanceled(token);
                    return;
                }

                source.TrySetResult();
            }
            catch (Exception ex)
            {
                source.TrySetException(ex);
            }
        }).CallDeferred();

        await source.Task;
        token.ThrowIfCancellationRequested();
        ThrowIfInvalid(owner, token);
    }

    private static void ThrowIfInvalid(GodotObject owner, CancellationToken token)
    {
        if (!GodotObject.IsInstanceValid(owner))
            throw new OperationCanceledException("Godot owner was deleted while awaiting a callback.", token);
    }
}
