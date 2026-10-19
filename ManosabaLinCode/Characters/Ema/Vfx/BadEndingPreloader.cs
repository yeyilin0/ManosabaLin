using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.TestSupport;

namespace ManosabaLin.Characters.Ema.Vfx;

/// <summary>
/// Submits the courtroom, figures, impacts, outer arcs and support effects while the
/// loading screen is still active. Merely caching their textures leaves first-draw
/// pipeline work until the card is played or the persistent effect first activates.
/// </summary>
internal static class BadEndingPreloader
{
    private static readonly double[] SampleTimes = [.6, 1.4, 2.25, 2.6, 3.1, 3.8, 4.8, 5.7, 6.7];
    private static Task? _pending;
    private static WarmKey? _ready;
    private static bool _reportedFailure;

    private readonly record struct WarmKey(ulong SceneId, Viewport.Msaa Msaa, bool Hdr, bool Transparent,
        Viewport.DefaultCanvasItemTextureFilter Filter, Viewport.DefaultCanvasItemTextureRepeat Repeat);

    internal static async Task AfterAssetsLoaded(Task original)
    {
        // Never hide a real asset-loading failure behind this optional optimization.
        await original;
        if (TestMode.IsOn || !PreloadManager.Enabled) return;
        Task? ownedWarmup = null;
        try
        {
            if (DisplayServer.GetName() == "headless" || !RenderingServer.RenderLoopEnabled) return;
            if (_pending is { IsCompleted: false }) await _pending;
            var game = NGame.Instance;
            if (game == null || !GodotObject.IsInstanceValid(game) || !game.IsInsideTree() ||
                !PreloadManager.Cache.ContainsKey(BadEndingVfx.ScenePath)) return;

            var scene = PreloadManager.Cache.GetScene(BadEndingVfx.ScenePath);
            var target = game.GetViewport();
            var key = new WarmKey(scene.GetInstanceId(), target.Msaa2D, target.UseHdr2D, target.TransparentBg,
                target.CanvasItemDefaultTextureFilter, target.CanvasItemDefaultTextureRepeat);
            if (_ready == key) return;

            // The engine awaits this task before creating the combat room. A changed
            // cached scene/render profile warms again; an ordinary new combat does not.
            ownedWarmup = WarmAndRemember(scene, target, key);
            _pending = ownedWarmup;
            await ownedWarmup;
        }
        catch (Exception ex)
        {
            // A suspended renderer or malformed optional VFX must not block a run.
            if (_reportedFailure) return;
            _reportedFailure = true;
            Log.Warn($"Bad ending drawing prewarm skipped; normal playback remains available: {ex.Message}");
        }
        finally
        {
            if (ownedWarmup != null && ReferenceEquals(_pending, ownedWarmup)) _pending = null;
        }
    }

    private static async Task WarmAndRemember(PackedScene scene, Viewport target, WarmKey key)
    {
        await WarmScene(scene, target);
        // Publish success before any concurrent loading wrapper finishes awaiting us.
        _ready = key;
    }

    private static async Task WarmScene(PackedScene scene, Viewport target)
    {
        // Invisible CanvasItems do not warm their draw pipelines. Render the real
        // stage to a small, unattached texture with the main viewport's canvas profile.
        var viewport = new SubViewport
        {
            Name = "BadEndingPrewarm",
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
                throw new InvalidOperationException("Bad ending prewarm script contract unavailable.");
            stage.Set("audio_enabled", false);
            stage.Call("prepare_idle");
            target.AddChild(viewport);
            foreach (var seconds in SampleTimes)
            {
                if (!GodotObject.IsInstanceValid(stage) || !stage.IsInsideTree())
                    throw new InvalidOperationException("Bad ending prewarm scene left the tree.");
                // The final sample also submits all four support-effect variants.
                stage.Call("prewarm_at", seconds);
                await WaitForSignal(stage.GetTree(), SceneTree.SignalName.ProcessFrame);
                await WaitForSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                await WaitForSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            }
        }
        finally
        {
            // No readback or retained scene nodes. Shared shader/material/texture
            // resources remain owned by the scene in the engine's RunSet asset cache.
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
internal static class BadEndingRunPreloadPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref Task __result) =>
        __result = BadEndingPreloader.AfterAssetsLoaded(__result);
}

// Covers restored/development combat and render-profile changes. In a normal run,
// the scene ID/profile key makes this return without creating another render tree.
[HarmonyPatch(typeof(PreloadManager), nameof(PreloadManager.LoadRoomCombatAssets))]
internal static class BadEndingCombatPreloadPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref Task __result) =>
        __result = BadEndingPreloader.AfterAssetsLoaded(__result);
}
