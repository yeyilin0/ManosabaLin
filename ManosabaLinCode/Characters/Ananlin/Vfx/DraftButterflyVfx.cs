using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Saves;

namespace ManosabaLin.Characters.Ananlin.Vfx;

public readonly record struct DraftButterflyMagic(int Index, Color Color);

/// <summary>One colored line butterfly per recorded magic; actual FinishedDraft preview on completion.</summary>
public partial class DraftButterflyVfx : Node, IDisposable
{
    public const string ScenePath = "res://ManosabaLin/scenes/Ananlin/draft_butterflies/draft_butterflies.tscn";
    private readonly TaskCompletionSource _finished = new();
    private double _elapsed;
    private bool _disposed;
    private CanvasLayer? _effect;
    public Task Finished => _finished.Task;

    private static readonly Regex Markup = new(@"\[(?:color=(?<hex>#[0-9a-f]{6,8})|(?<name>blue|red|purple|gold|green|pink|orange|aqua))\]", RegexOptions.IgnoreCase);

    public static Color ReadMagicColor(PowerModel power, string? fallbackCardKey = null)
    {
        var match = Markup.Match(power.Description.GetRawText());
        if (!match.Success && fallbackCardKey != null)
        {
            // Noah has no color in its power text; its card marks the magic by name.
            var text = new LocString("cards", fallbackCardKey).GetRawText();
            var title = power.Title.GetRawText();
            foreach (Match candidate in Markup.Matches(text))
            {
                var end = text.IndexOf("[/", candidate.Index + candidate.Length, StringComparison.Ordinal);
                var segment = text.Substring(candidate.Index, (end < 0 ? text.Length : end) - candidate.Index);
                if (segment.Contains(title, StringComparison.Ordinal))
                {
                    match = candidate;
                    break;
                }
            }
        }
        if (match.Groups["hex"].Success)
            return new Color(match.Groups["hex"].Value);
        return match.Groups["name"].Value.ToLowerInvariant() switch
        {
            "blue" => StsColors.blue,
            "red" => StsColors.red,
            "purple" => StsColors.purple,
            "gold" => StsColors.gold,
            "green" => StsColors.green,
            "pink" => StsColors.pink,
            "orange" => StsColors.orange,
            "aqua" => StsColors.aqua,
            _ => new Color("6666cc")
        };
    }

    public static DraftButterflyVfx? Play(Creature caster, IReadOnlyList<DraftButterflyMagic> magics, CardModel? finishedCard)
    {
        if (TestMode.IsOn || caster.CombatState?.IsLiveCombat() != true || magics.Count == 0)
            return null;
        var room = NCombatRoom.Instance;
        if (room == null || !GodotObject.IsInstanceValid(room) || !room.IsInsideTree())
            return null;
        var playback = new DraftButterflyVfx { Name = "DraftButterflyPlayback" };
        NCard? previewCard = null;
        try
        {
            var scene = ResourceLoader.Load<PackedScene>(ScenePath)
                ?? throw new InvalidOperationException("Butterfly scene missing.");
            var effect = scene.Instantiate<CanvasLayer>();
            playback._effect = effect;
            effect.Set("sfx_gain", Mathf.Pow(SaveManager.Instance.SettingsSave.VolumeSfx, 2f));
            playback.AddChild(effect);
            if (!effect.HasSignal("finished")) throw new InvalidOperationException("Butterfly script failed to load.");
            effect.Set("magic_indices", magics.Select(m => m.Index).ToArray());
            effect.Set("magic_colors", magics.Select(m => m.Color).ToArray());
            effect.Set("complete", finishedCard != null);
            effect.Connect("finished", Callable.From(() =>
            {
                playback._finished.TrySetResult();
                if (GodotObject.IsInstanceValid(playback) && !playback.IsQueuedForDeletion())
                    playback.QueueFree();
            }));
            effect.TreeExiting += () => playback._finished.TrySetResult();
            if (finishedCard != null)
            {
                var center = new Node2D { Name = "CompletionCard" };
                effect.AddChild(center);
                var node = NCard.Create(finishedCard);
                if (node != null)
                {
                    center.AddChild(node);
                    node.MouseFilter = Control.MouseFilterEnum.Ignore;
                    previewCard = node;
                }
            }
            room.AddChild(playback);
            // NCard comes from a pool: reused nodes are already ready and will not
            // emit Ready again. Always refresh after attachment, fresh or pooled.
            if (previewCard != null)
            {
                previewCard.Model = finishedCard;
                previewCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
                previewCard.Position = -previewCard.Size * .5f;
            }
            return playback;
        }
        catch (Exception ex)
        {
            Log.Warn($"Draft butterfly VFX unavailable: {ex.Message}");
            playback.Dispose();
            return null;
        }
    }

    public override void _Process(double delta)
    {
        if (_effect != null && GodotObject.IsInstanceValid(_effect))
            _effect.Set("sfx_gain", Mathf.Pow(SaveManager.Instance.SettingsSave.VolumeSfx, 2f));
        _elapsed += delta;
        if (_elapsed > 6.25 && !_finished.Task.IsCompleted)
        {
            Log.Warn("Draft butterfly VFX exceeded its timeline; returning to card resolution.");
            Dispose();
        }
    }
    public override void _ExitTree() => _finished.TrySetResult();

    public new void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _finished.TrySetResult();
        if (GodotObject.IsInstanceValid(this) && !IsQueuedForDeletion())
        {
            if (IsInsideTree()) QueueFree();
            else Free();
        }
        GC.SuppressFinalize(this);
    }
}
