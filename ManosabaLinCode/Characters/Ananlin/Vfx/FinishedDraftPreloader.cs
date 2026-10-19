using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.TestSupport;

namespace ManosabaLin.Characters.Ananlin.Vfx;

/// <summary>Draws the curtain, butterflies, tethers and impact strokes during asset loading.</summary>
internal static class FinishedDraftPreloader
{
    private static readonly double[] SampleTimes = [.8, 2.35, 3.1, 3.6];
    private static Task? _pending;
    private static WarmKey? _ready;
    private static bool _reportedFailure;

    private readonly record struct WarmKey(ulong SceneId, ulong CurtainId, Viewport.Msaa Msaa, bool Hdr,
        bool Transparent, Viewport.DefaultCanvasItemTextureFilter Filter,
        Viewport.DefaultCanvasItemTextureRepeat Repeat);

    internal static async Task AfterAssetsLoaded(Task original)
    {
        // Preserve failures/cancellation from the actual engine asset-loading task.
        await original;
        if (TestMode.IsOn || !PreloadManager.Enabled) return;
        Task? ownedWarmup = null;
        try
        {
            if (DisplayServer.GetName() == "headless" || !RenderingServer.RenderLoopEnabled) return;
            if (_pending is { IsCompleted: false }) await _pending;
            var game = NGame.Instance;
            if (game == null || !GodotObject.IsInstanceValid(game) || !game.IsInsideTree() ||
                !PreloadManager.Cache.ContainsKey(FinishedDraftVfx.ScenePath) ||
                !PreloadManager.Cache.ContainsKey(FinishedDraftVfx.CurtainScenePath)) return;

            var scene = PreloadManager.Cache.GetScene(FinishedDraftVfx.ScenePath);
            var curtain = PreloadManager.Cache.GetScene(FinishedDraftVfx.CurtainScenePath);
            var target = game.GetViewport();
            var key = new WarmKey(scene.GetInstanceId(), curtain.GetInstanceId(), target.Msaa2D,
                target.UseHdr2D, target.TransparentBg, target.CanvasItemDefaultTextureFilter,
                target.CanvasItemDefaultTextureRepeat);
            if (_ready == key) return;
            ownedWarmup = WarmAndRemember(scene, curtain, target, key);
            _pending = ownedWarmup;
            await ownedWarmup;
        }
        catch (Exception ex)
        {
            if (_reportedFailure) return;
            _reportedFailure = true;
            Log.Warn($"Finished draft drawing prewarm skipped; normal playback remains available: {ex.Message}");
        }
        finally
        {
            if (ownedWarmup != null && ReferenceEquals(_pending, ownedWarmup)) _pending = null;
        }
    }

    private static async Task WarmAndRemember(PackedScene scene, PackedScene curtain, Viewport target, WarmKey key)
    {
        await WarmScene(scene, curtain, target);
        _ready = key;
    }

    private static async Task WarmScene(PackedScene scene, PackedScene curtain, Viewport target)
    {
        // The render texture is never displayed. Invisible nodes would not compile
        // their canvas pipelines, so submit real draws to this small viewport.
        var viewport = new SubViewport
        {
            Name = "FinishedDraftPrewarm",
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
            var backdrop = curtain.Instantiate<Node2D>();
            viewport.AddChild(backdrop);
            var layer = scene.Instantiate<CanvasLayer>();
            viewport.AddChild(layer);
            var stage = layer.GetNode<Node2D>("Stage");
            if (!stage.HasMethod("prepare_idle") || !stage.HasMethod("prewarm_at"))
                throw new InvalidOperationException("Finished draft prewarm script contract unavailable.");
            stage.Set("backdrop", backdrop);
            stage.Set("audio_enabled", false);
            stage.Call("prepare_idle");
            target.AddChild(viewport);
            foreach (var seconds in SampleTimes)
            {
                if (!GodotObject.IsInstanceValid(stage) || !stage.IsInsideTree())
                    throw new InvalidOperationException("Finished draft prewarm scene left the tree.");
                stage.Call("prewarm_at", seconds);
                await WaitForSignal(stage.GetTree(), SceneTree.SignalName.ProcessFrame);
                await WaitForSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                await WaitForSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            }
        }
        finally
        {
            // Resource dependencies stay in RunSet. No scene nodes or GPU readbacks survive.
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
internal static class FinishedDraftRunPreloadPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref Task __result) =>
        __result = FinishedDraftPreloader.AfterAssetsLoaded(__result);
}

[HarmonyPatch(typeof(PreloadManager), nameof(PreloadManager.LoadRoomCombatAssets))]
internal static class FinishedDraftCombatPreloadPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref Task __result) =>
        __result = FinishedDraftPreloader.AfterAssetsLoaded(__result);
}
