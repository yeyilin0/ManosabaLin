using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.TestSupport;

namespace ManosabaLin.Characters.Ema.Vfx;

/// <summary>
/// Asset preloading retains the scene's resources, but does not submit their first draw.
/// Do that during the loading transition, including the primitives first used after reunion.
/// </summary>
internal static class TrueEndingPreloader
{
    private static readonly double[] SampleTimes = [.65, 1.65, 2.48, 3.65, 4.55, 5.6];
    private static Task? _pending;
    private static WarmKey? _ready;
    private static bool _reportedFailure;

    private readonly record struct WarmKey(ulong SceneId, Viewport.Msaa Msaa, bool Hdr, bool Transparent,
        Viewport.DefaultCanvasItemTextureFilter Filter, Viewport.DefaultCanvasItemTextureRepeat Repeat);

    internal static async Task AfterAssetsLoaded(Task original)
    {
        // Keep the original loading failure/cancellation semantics intact.
        await original;
        if (TestMode.IsOn || !PreloadManager.Enabled) return;
        Task? ownedWarmup = null;
        try
        {
            if (DisplayServer.GetName() == "headless" || !RenderingServer.RenderLoopEnabled) return;
            if (_pending is { IsCompleted: false }) await _pending;
            var game = NGame.Instance;
            if (game == null || !GodotObject.IsInstanceValid(game) || !game.IsInsideTree() ||
                !PreloadManager.Cache.ContainsKey(TrueEndingVfx.ScenePath)) return;

            var scene = PreloadManager.Cache.GetScene(TrueEndingVfx.ScenePath);
            var target = game.GetViewport();
            var key = new WarmKey(scene.GetInstanceId(), target.Msaa2D, target.UseHdr2D, target.TransparentBg,
                target.CanvasItemDefaultTextureFilter, target.CanvasItemDefaultTextureRepeat);
            if (_ready == key) return;
            // Awaited by the engine, never scheduled from card play. No scene-tree references
            // survive this task; a new cached scene or render profile is warmed again.
            ownedWarmup = WarmAndRemember(scene, target, key);
            _pending = ownedWarmup;
            await ownedWarmup;
        }
        catch (Exception ex)
        {
            // A visual optimization must not prevent starting/resuming a run.
            if (_reportedFailure) return;
            _reportedFailure = true;
            Log.Warn($"True ending drawing prewarm skipped; normal playback remains available: {ex.Message}");
        }
        finally
        {
            if (ownedWarmup != null && ReferenceEquals(_pending, ownedWarmup)) _pending = null;
        }
    }

    private static async Task WarmAndRemember(PackedScene scene, Viewport target, WarmKey key)
    {
        await WarmScene(scene, target);
        // All callers awaiting _pending must see the successful key before it completes.
        _ready = key;
    }

    private static async Task WarmScene(PackedScene scene, Viewport target)
    {
        // A real offscreen render, not invisible CanvasItems or a transparent 1-pixel rect.
        // Match the render target options that affect canvas pipeline variants.
        var viewport = new SubViewport
        {
            Name = "TrueEndingPrewarm",
            Size = new Vector2I(320, 180),
            Disable3D = true,
            TransparentBg = target.TransparentBg,
            Msaa2D = target.Msaa2D,
            UseHdr2D = target.UseHdr2D,
            CanvasItemDefaultTextureFilter = target.CanvasItemDefaultTextureFilter,
            CanvasItemDefaultTextureRepeat = target.CanvasItemDefaultTextureRepeat,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            ProcessMode = Node.ProcessModeEnum.Always
        };
        try
        {
            var stage = scene.Instantiate<Node2D>();
            viewport.AddChild(stage);
            if (!stage.HasMethod("prewarm_at") || !stage.HasMethod("prepare_idle"))
                throw new InvalidOperationException("True ending prewarm script contract unavailable.");
            stage.Set("audio_enabled", false);
            stage.Call("prepare_idle");
            target.AddChild(viewport);
            foreach (var seconds in SampleTimes)
            {
                if (!GodotObject.IsInstanceValid(stage) || !stage.IsInsideTree())
                    throw new InvalidOperationException("True ending prewarm scene left the tree.");
                stage.Call("prewarm_at", seconds);
                // Allow deferred CanvasItem redraws and their first render submission to finish.
                await WaitForSignal(stage.GetTree(), SceneTree.SignalName.ProcessFrame);
                await WaitForSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                await WaitForSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            }
        }
        finally
        {
            // The texture is never attached to the main canvas. Resource dependencies remain
            // owned by the engine's RunSet cache; only this temporary render tree is released.
            if (GodotObject.IsInstanceValid(viewport)) viewport.Free();
        }
    }

    private static async Task WaitForSignal(GodotObject source, StringName signal)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = Callable.From(() => { completion.TrySetResult(); });
        var error = source.Connect(signal, callback, (uint)GodotObject.ConnectFlags.OneShot);
        if (error != Godot.Error.Ok)
            throw new InvalidOperationException($"Cannot await rendering: {error}.");
        try
        {
            // Rendering may stop when the window closes or the renderer is suspended.
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(3));
        }
        finally
        {
            if (GodotObject.IsInstanceValid(source) && source.IsConnected(signal, callback))
                source.Disconnect(signal, callback);
        }
    }
}

[HarmonyPatch(typeof(PreloadManager), nameof(PreloadManager.LoadRunAssets))]
internal static class TrueEndingRunPreloadPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref Task __result) =>
        __result = TrueEndingPreloader.AfterAssetsLoaded(__result);
}

// Also covers combat entered through development/restore paths and a changed render profile.
// The usual case returns immediately because the same cached scene was warmed at run load.
[HarmonyPatch(typeof(PreloadManager), nameof(PreloadManager.LoadRoomCombatAssets))]
internal static class TrueEndingCombatPreloadPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref Task __result) =>
        __result = TrueEndingPreloader.AfterAssetsLoaded(__result);
}
