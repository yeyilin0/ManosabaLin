using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.TestSupport;

namespace ManosabaLin.Characters.Sherrylin.Vfx;

/// <summary>Submit the opening, support pulses and retained border during loading.</summary>
internal static class FriendshipPreloader
{
    private static Task? _pending;
    private static WarmKey? _ready;
    private static bool _reportedFailure;
    private readonly record struct WarmKey(ulong OpeningId, ulong FieldId, Viewport.Msaa Msaa,
        bool Hdr, bool Transparent, Viewport.DefaultCanvasItemTextureFilter Filter,
        Viewport.DefaultCanvasItemTextureRepeat Repeat);

    internal static async Task AfterAssetsLoaded(Task original)
    {
        await original;
        if (TestMode.IsOn || !PreloadManager.Enabled) return;
        Task? ownedWarmup = null;
        try
        {
            if (DisplayServer.GetName() == "headless" || !RenderingServer.RenderLoopEnabled) return;
            if (_pending is { IsCompleted: false }) await _pending;
            var game = NGame.Instance;
            if (game == null || !GodotObject.IsInstanceValid(game) || !game.IsInsideTree() ||
                !PreloadManager.Cache.ContainsKey(FriendshipVfx.ScenePath) ||
                !PreloadManager.Cache.ContainsKey(FriendshipFieldVfx.ScenePath)) return;
            var opening = PreloadManager.Cache.GetScene(FriendshipVfx.ScenePath);
            var field = PreloadManager.Cache.GetScene(FriendshipFieldVfx.ScenePath);
            var target = game.GetViewport();
            var key = new WarmKey(opening.GetInstanceId(), field.GetInstanceId(), target.Msaa2D,
                target.UseHdr2D, target.TransparentBg, target.CanvasItemDefaultTextureFilter,
                target.CanvasItemDefaultTextureRepeat);
            if (_ready == key) return;
            ownedWarmup = WarmAndRemember(opening, field, target, key);
            _pending = ownedWarmup;
            await ownedWarmup;
        }
        catch (Exception ex)
        {
            if (_reportedFailure) return;
            _reportedFailure = true;
            Log.Warn($"Friendship drawing prewarm skipped; gameplay continues normally: {ex.Message}");
        }
        finally
        {
            if (ownedWarmup != null && ReferenceEquals(_pending, ownedWarmup)) _pending = null;
        }
    }

    private static async Task WarmAndRemember(PackedScene opening, PackedScene field, Viewport target, WarmKey key)
    {
        var viewport = new SubViewport
        {
            Name = "FriendshipPrewarm",
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
            target.AddChild(viewport);
            for (var mode = 0; mode <= 4; mode++)
            {
                var stage = opening.Instantiate<Node2D>();
                // setup must precede _Ready because the opening builds its own
                // shader passes while the four short pulses only build the flow.
                if (!stage.HasMethod("setup") || !stage.HasMethod("seek_preview"))
                {
                    stage.Free();
                    throw new InvalidOperationException("Friendship prewarm scene contract unavailable.");
                }
                stage.Set("autoplay", false);
                stage.Set("audio_enabled", false);
                stage.Call("setup", new Vector2(.22f, .55f), new Vector2(.75f, .5f), mode);
                viewport.AddChild(stage);
                double[] samples = mode == 0 ? [.65, 1.85, 3.1, 3.8] : [.55];
                foreach (var seconds in samples)
                {
                    stage.Call("seek_preview", seconds);
                    await SubmitFrame(stage);
                }
                viewport.RemoveChild(stage);
                stage.Free();
            }
            var layer = field.Instantiate<CanvasLayer>();
            viewport.AddChild(layer);
            var perimeter = layer.GetNode<Node2D>("Stage");
            if (!perimeter.HasMethod("prewarm_at"))
                throw new InvalidOperationException("Friendship field prewarm contract unavailable.");
            perimeter.Call("prewarm_at", 2.0);
            await SubmitFrame(perimeter);
            // No image readback or retained viewports; actual shared shader and
            // texture resources remain owned by the engine's RunSet cache.
            _ready = key;
        }
        finally
        {
            if (GodotObject.IsInstanceValid(viewport)) viewport.Free();
        }
    }

    private static async Task SubmitFrame(Node stage)
    {
        await WaitForSignal(stage.GetTree(), SceneTree.SignalName.ProcessFrame);
        await WaitForSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        await WaitForSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }

    private static async Task WaitForSignal(GodotObject source, StringName signal)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = Callable.From(() => { completion.TrySetResult(); });
        var error = source.Connect(signal, callback, (uint)GodotObject.ConnectFlags.OneShot);
        if (error != Godot.Error.Ok) throw new InvalidOperationException($"Cannot await rendering: {error}.");
        try { await completion.Task.WaitAsync(TimeSpan.FromSeconds(3)); }
        finally
        {
            if (GodotObject.IsInstanceValid(source) && source.IsConnected(signal, callback))
                source.Disconnect(signal, callback);
        }
    }
}

[HarmonyPatch(typeof(PreloadManager), nameof(PreloadManager.LoadRunAssets))]
internal static class FriendshipRunPreloadPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref Task __result) => __result = FriendshipPreloader.AfterAssetsLoaded(__result);
}

[HarmonyPatch(typeof(PreloadManager), nameof(PreloadManager.LoadRoomCombatAssets))]
internal static class FriendshipCombatPreloadPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref Task __result) => __result = FriendshipPreloader.AfterAssetsLoaded(__result);
}
