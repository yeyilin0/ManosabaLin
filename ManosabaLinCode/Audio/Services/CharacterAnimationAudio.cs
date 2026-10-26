using Godot;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace ManosabaLin.Audio.Services;

/// <summary>
/// Follows the real character sprite, including both card-driven and RitsuLib-driven
/// playback. Cross-fade ghosts never own an audio controller.
/// </summary>
public sealed partial class CharacterAnimationAudio : Node
{
    private const string NodeName = "CharacterAnimationAudio";
    private const string AudioRoot = MainFile.ResPath + "/audio/character_animation/";
    private const float InterruptFadeSeconds = .055f;
    private static readonly StringName Idle = "default";
    private static readonly StringName Hurt = "hurt";
    private static readonly StringName Hit = "hit";
    private static readonly Dictionary<string, AudioStream> Streams = new();
    private static readonly HashSet<string> ReportedMissing = new();

    private readonly record struct Cue(StringName Animation, int Frame, string File, float Gain = .85f,
        bool EndsPreparation = false);

    private sealed class Voice(AudioStreamPlayer player)
    {
        public AudioStreamPlayer Player { get; } = player;
        public float Gain;
        public float Fade = 1f;
        public bool Fading;
    }

    private AnimatedSprite2D _sprite = null!;
    private Cue[] _cues = [];
    private AudioStream?[] _sounds = [];
    private readonly Voice[] _voices = new Voice[4];
    private StringName _animation = "";
    private int _lastFrame = -1;
    private uint _played;

    // Frame indices are zero-based and refer to the displayed SpriteFrames order,
    // not the atlas coordinates. See tools/audio/character_animation_cues.md.
    private static Cue[] Profile(CharacterModel character) => character switch
    {
        Characters.Emalin.Emalin => [
            new("attack", 6, "ema_attack"),
            new("hit", 7, "ema_hit"),
            new("cast", 6, "ema_cast_gather", .72f),
            new("cast", 10, "ema_cast_release", EndsPreparation: true)],
        Characters.Hiro.Hiro => [
            new("attack", 7, "hiro_attack"),
            new("hit", 3, "hiro_hit", .78f),
            new("cast", 8, "hiro_cast_gather", .72f),
            new("cast", 11, "hiro_cast_release", EndsPreparation: true)],
        Characters.Sherrylin.Sherrylin => [
            new("attack", 7, "sherry_attack"),
            new("hit", 2, "sherry_hit", .78f),
            new("cast", 4, "sherry_cast_gather", .72f),
            new("cast", 12, "sherry_cast_release", EndsPreparation: true)],
        Characters.Yalisalin.Yalisalin => [
            new("attack", 6, "arisa_attack"),
            new("hit", 3, "arisa_hit", .78f),
            new("cast", 6, "arisa_cast_ignite", .72f),
            new("cast", 10, "arisa_cast_flare", EndsPreparation: true)],
        Characters.Ananlin.Ananlin => [
            new("attack", 6, "anan_attack"),
            new("hit", 2, "anan_hit", .75f),
            new("cast", 1, "anan_cast_write", .80f),
            new("cast", 11, "anan_cast_release", EndsPreparation: true)],
        _ => []
    };

    internal static IEnumerable<string> AssetPaths(CharacterModel character) =>
        Profile(character).Select(cue => AudioRoot + cue.File + ".wav").Distinct();

    internal static void Attach(AnimatedSprite2D sprite, CharacterModel character)
    {
        if (TestMode.IsOn || sprite.GetNodeOrNull<Node>(NodeName) != null)
            return;
        var cues = Profile(character);
        if (cues.Length == 0)
            return;
        // Created with the visuals, before a card can be played. Keeping strong
        // references here avoids decoding/allocating streams at an impact frame.
        var audio = new CharacterAnimationAudio { Name = NodeName, _sprite = sprite, _cues = cues };
        audio._sounds = new AudioStream?[cues.Length];
        for (var i = 0; i < cues.Length; i++)
        {
            var path = AudioRoot + cues[i].File + ".wav";
            try
            {
                if (!Streams.TryGetValue(path, out var stream))
                {
                    stream = ResourceLoader.Load<AudioStream>(path);
                    if (stream != null) Streams[path] = stream;
                    else if (ReportedMissing.Add(path))
                        Log.Warn($"Character animation sound could not be loaded: {path}");
                }
                audio._sounds[i] = stream;
            }
            catch (Exception ex)
            {
                if (ReportedMissing.Add(path))
                    Log.Warn($"Character animation sound unavailable: {path}: {ex.Message}");
            }
        }
        sprite.AddChild(audio);
    }

    public override void _Ready()
    {
        for (var i = 0; i < _voices.Length; i++)
        {
            var player = new AudioStreamPlayer { Name = $"Voice{i}", Bus = "Master", MaxPolyphony = 1 };
            AddChild(player);
            _voices[i] = new Voice(player);
        }
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_sprite))
            return;
        var animation = _sprite.Animation == Hurt ? Hit : _sprite.Animation;
        var frame = _sprite.Frame;
        if (animation != _animation || frame < _lastFrame)
        {
            // A normal return to idle keeps the short natural tail. An interrupt
            // (hit, another action, death) releases the previous action quickly.
            if (animation != Idle) FadeVoices();
            _animation = animation;
            _played = 0;
        }
        _lastFrame = frame;
        var gain = SfxGain();
        if (_sprite.IsPlaying() && _sprite.IsVisibleInTree())
        {
            for (var i = 0; i < _cues.Length; i++)
            {
                var cue = _cues[i];
                var bit = 1u << i;
                if (cue.Animation != animation || frame < cue.Frame || (_played & bit) != 0)
                    continue;
                _played |= bit;
                if (cue.EndsPreparation) FadeVoices();
                // Muted or hidden cues are never queued for later playback.
                if (gain > 0f && _sounds[i] is { } sound)
                    Play(sound, cue.Gain);
            }
        }
        else if (!_sprite.IsVisibleInTree())
        {
            FadeVoices();
            _played = uint.MaxValue;
        }
        foreach (var voice in _voices)
        {
            if (!voice.Player.Playing) continue;
            if (voice.Fading)
            {
                voice.Fade = Mathf.Max(0f, voice.Fade - (float)delta / InterruptFadeSeconds);
                if (voice.Fade <= 0f)
                {
                    voice.Player.Stop();
                    continue;
                }
            }
            voice.Player.VolumeDb = Mathf.LinearToDb(Mathf.Max(.00001f, gain * voice.Gain * voice.Fade));
        }
    }

    private void Play(AudioStream sound, float gain)
    {
        // Four persistent voices bound overlap even under rapid card playback.
        var voice = _voices[0];
        foreach (var candidate in _voices)
        {
            if (!candidate.Player.Playing)
            {
                voice = candidate;
                break;
            }
            if (candidate.Fade < voice.Fade) voice = candidate;
        }
        voice.Player.Stop();
        voice.Player.Stream = sound;
        voice.Gain = gain;
        voice.Fade = 1f;
        voice.Fading = false;
        voice.Player.VolumeDb = Mathf.LinearToDb(Mathf.Max(.00001f, SfxGain() * gain));
        voice.Player.Play();
    }

    private void FadeVoices()
    {
        foreach (var voice in _voices)
            if (voice.Player.Playing) voice.Fading = true;
    }

    private static float SfxGain()
    {
        var saves = SaveManager.Instance;
        if (saves.PrefsSave.MuteInBackground && !DisplayServer.WindowIsFocused()) return 0f;
        return Mathf.Pow(Mathf.Clamp(saves.SettingsSave.VolumeSfx, 0f, 1f), 2f);
    }

    public override void _ExitTree()
    {
        foreach (var voice in _voices)
            if (voice != null && GodotObject.IsInstanceValid(voice.Player)) voice.Player.Stop();
    }
}
