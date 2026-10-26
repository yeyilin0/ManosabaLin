"""Original short Foley/magic cues for character animation keyframes.

No speech, sampled video audio, motor-like periodic modulation, or shared
background bed. Attack/guard cues start immediately; ability cues are split so
the runtime can follow the actual sprite frames instead of wall-clock delays.

Requires numpy only. Outputs 44.1 kHz stereo PCM16 WAVs, a listening reel, and a
read-back quality report. Existing card-specific sound files are not touched.
"""
from pathlib import Path
import json
import math
import wave

import numpy as np


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ManosabaLin/audio/character_animation"
REVIEW = ROOT / ".local/character_animation_sfx"
SR = 44100
RNG = np.random.default_rng(20261020)
MANIFEST = []
REEL = []


def smooth(x):
    x = np.clip(x, 0, 1)
    return x * x * (3 - 2 * x)


def clock(seconds):
    return np.arange(round(seconds * SR), dtype=np.float64) / SR


def blank(seconds):
    return np.zeros((round(seconds * SR), 2), dtype=np.float64)


def noise(seconds, low=250, high=6000, slope=.25):
    """Non-periodic breath/cloth/fire, with rounded frequency boundaries."""
    n = round(seconds * SR)
    f = np.fft.rfftfreq(n, 1 / SR)
    band = (1 - np.exp(-(f / low) ** 4)) * np.exp(-(f / high) ** 4)
    band *= (np.maximum(f, 200) / 200) ** -slope
    x = np.fft.irfft(np.fft.rfft(RNG.normal(size=n)) * band, n)
    return x / max(np.sqrt(np.mean(x * x)), 1e-8)


def place(dst, source, onset=0, gain=1, pan=0):
    start = round(onset * SR)
    count = min(len(source), len(dst) - start)
    if count <= 0:
        return
    angle = (np.clip(pan, -.8, .8) + 1) * np.pi / 4
    if isinstance(angle, np.ndarray):
        angle = angle[:count]
    dst[start:start + count, 0] += source[:count] * gain * np.cos(angle)
    dst[start:start + count, 1] += source[:count] * gain * np.sin(angle)


def glass(seconds, hz, attack=.012, decay=.22, metal=False):
    t = clock(seconds)
    x = np.zeros_like(t)
    partials = (
        [(1, 1, 1), (2.46, .24, .41), (3.91, .12, .19), (5.24, .06, .08)]
        if metal else
        [(1, 1, 1), (2.003, .12, .55), (2.756, .035, .27)]
    )
    for ratio, amp, falloff in partials:
        if hz * ratio > 6400:
            continue
        # Small monotonic settling, not a vibrato or electronic charge sweep.
        freq = hz * ratio * (1 + .002 * np.exp(-t / .042))
        phase = 2 * np.pi * np.cumsum(freq) / SR
        x += np.sin(phase) * amp * np.exp(-t / (decay * falloff))
    return x * smooth(t / attack) * smooth((seconds - t) / .075)


def breath(seconds, peak=.12, width=.10, low=450, high=4800):
    t = clock(seconds)
    envelope = np.exp(-.5 * ((t - peak) / width) ** 2)
    envelope *= smooth(t / .006) * smooth((seconds - t) / .035)
    irregular = np.interp(t, np.linspace(0, seconds, 9), RNG.uniform(.72, 1.1, 9))
    return noise(seconds, low, high) * envelope * irregular


def paper(seconds, strokes):
    t = clock(seconds)
    x = np.zeros_like(t)
    for onset, span, gain in strokes:
        u = (t - onset) / span
        pressure = smooth(u / .14) * smooth((1 - u) / .22)
        roughness = np.interp(t, np.linspace(0, seconds, 43), RNG.uniform(.3, 1, 43))
        x += noise(seconds, 950, 5900, .08) * pressure * roughness * gain
    return x


def fire(seconds, swelling=False):
    t = clock(seconds)
    if swelling:
        env = smooth(t / .10) * np.exp(-t / .46)
    else:
        env = smooth(t / .007) * np.exp(-t / .17)
    uneven = np.interp(t, np.linspace(0, seconds, 14), RNG.uniform(.4, 1, 14))
    x = noise(seconds, 190, 3900, .38) * env * uneven
    # Tiny uneven flame crackles with soft attacks; no metronomic pulse train.
    for onset in RNG.uniform(.025, min(seconds - .04, .48), 10):
        d = t - onset
        crack = smooth(d / .002) * np.exp(-np.maximum(d, 0) / RNG.uniform(.006, .018))
        x += noise(seconds, 1800, 6500, .08) * crack * RNG.uniform(.035, .085)
    return x


def body(seconds, strength=.7):
    t = clock(seconds)
    # Short drum/cloth body, damped before it can turn into a sustained bass.
    freq = 165 + 65 * np.exp(-t / .016)
    phase = 2 * np.pi * np.cumsum(freq) / SR
    x = np.sin(phase) * np.exp(-t / .028) * strength
    x += noise(seconds, 160, 1900) * np.exp(-t / .032) * .44
    return x * smooth(t / .003) * smooth((seconds - t) / .025)


def space(x, wet=.12):
    """Sparse small-room diffusion; no long train of gameplay-cluttering echoes."""
    out = x.copy()
    for ms, gain in [(19, .27), (37, .19), (61, .13), (89, .08), (127, .04)]:
        d = round(ms * SR / 1000)
        if d >= len(x):
            continue
        out[d:] += x[:-d, ::-1] * wet * gain
    return out


def save(name, audio, description, rms_db=-23, wet=.12):
    audio = space(audio, wet)
    audio -= audio.mean(axis=0)
    t = np.arange(len(audio)) / SR
    audio *= (smooth(t / .002) * smooth((len(audio) / SR - t) / .035))[:, None]
    # Keep deliberate spectral dynamics; only a static gain, never a pumping
    # compressor or a clipper. Leave >2 dB peak headroom even in the loud cues.
    active = np.max(np.abs(audio), axis=1) > .0001
    rms = np.sqrt(np.mean(audio[active] ** 2)) if np.any(active) else 1
    gain = min(10 ** (rms_db / 20) / max(rms, 1e-8), .77 / max(np.max(np.abs(audio)), 1e-8))
    audio *= gain
    pcm = np.round(audio * 32767).astype("<i2")
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / f"{name}.wav"
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(SR)
        wav.writeframes(pcm.tobytes())
    with wave.open(str(path), "rb") as wav:
        recovered = np.frombuffer(wav.readframes(wav.getnframes()), "<i2").reshape(-1, 2) / 32768
        assert wav.getframerate() == SR and wav.getnchannels() == 2
        assert wav.getsampwidth() == 2
    peak = float(np.max(np.abs(recovered)))
    first = float(np.max(np.abs(recovered[0])))
    last = float(np.max(np.abs(recovered[-1])))
    assert peak < 10 ** (-2 / 20) and len(recovered) <= SR * 1.8
    assert first == 0 and last < .0001
    MANIFEST.append({
        "file": str(path.relative_to(ROOT)).replace("\\", "/"),
        "cue": name,
        "description": description,
        "duration_seconds": round(len(recovered) / SR, 4),
        "peak_dbfs": round(20 * math.log10(max(peak, 1e-9)), 2),
        "rms_dbfs": round(20 * math.log10(max(np.sqrt(np.mean(recovered ** 2)), 1e-9)), 2),
        "clipped_samples": int(np.sum(np.abs(recovered) >= .999)),
        "first_sample": first,
        "last_sample": last,
        "bytes": path.stat().st_size,
    })
    REEL.append((name, recovered))


def ema():
    x = blank(.70)
    place(x, breath(.43, .075, .075, 700, 5600), gain=.23, pan=-.08)
    place(x, glass(.60, 1174.66, decay=.17), .025, .18, .12)
    place(x, glass(.50, 1760, decay=.12), .06, .055, -.14)
    save("ema_attack", x, "粉白魔法释放：柔亮晶铃、短促流光", -23)

    x = blank(.62)
    place(x, body(.18, .28), gain=.12)
    place(x, breath(.33, .045, .060, 650, 5100), gain=.18)
    place(x, glass(.56, 1396.91, decay=.15), .008, .22, -.08)
    place(x, glass(.48, 2093, decay=.10), .025, .06, .12)
    save("ema_hit", x, "粉色法盾格挡：轻脆魔法接触与柔软短共鸣", -23)

    # Current cast is 24 frames at 24 fps: the circle-to-hand interval is only
    # four frames. Compose a short gesture instead of pitching up a long swell.
    x = blank(.34)
    t = clock(.29)
    place(x, breath(.29, .09, .075, 800, 5100), gain=.15, pan=.16 * np.sin(t * 2.1))
    for onset, hz, amp, pan in [(0, 587.33, .13, -.16), (.04, 880, .10, .13), (.09, 1174.66, .08, -.08)]:
        place(x, glass(.24, hz, attack=.012, decay=.11), onset, amp, pan)
    save("ema_cast_gather", x, "脚下法阵展开，粉白光柔和上升汇聚", -25, .18)

    x = blank(.83)
    place(x, breath(.53, .12, .10, 900, 5600), gain=.19)
    for onset, hz, amp, pan in [(0, 1174.66, .21, -.08), (.025, 1479.98, .11, .13), (.065, 1760, .055, -.15)]:
        place(x, glass(.74, hz, decay=.23), onset, amp, pan)
    save("ema_cast_release", x, "伸手汇光成形：温暖明亮的水晶散光", -23)


def hiro():
    x = blank(.59)
    place(x, breath(.43, .065, .045, 350, 6200), gain=.31, pan=.10)
    place(x, noise(.29, 1100, 6500, .1) * np.exp(-clock(.29) / .037), .025, .055)
    place(x, glass(.43, 731, decay=.055, metal=True), .03, .10)
    save("hiro_attack", x, "利剑挥斩：利落破风与极短金属刃鸣", -22)

    x = blank(.56)
    place(x, body(.15, .4), gain=.13)
    place(x, noise(.24, 1200, 6300, .1) * np.exp(-clock(.24) / .021), gain=.15)
    place(x, glass(.48, 917, attack=.003, decay=.12, metal=True), gain=.23)
    save("hiro_hit", x, "举剑格挡：清楚金属碰击、迅速收束刃鸣", -23)

    x = blank(.28)
    place(x, breath(.24, .07, .06, 340, 4600), gain=.18)
    for onset, hz, amp in [(0, 466.16, .09), (.025, 698.46, .07), (.06, 932.33, .05)]:
        place(x, glass(.21, hz, attack=.012, decay=.09, metal=True), onset, amp, .10 - onset * .35)
    save("hiro_cast_gather", x, "竖剑凝神：沿剑身上升的暗红灰白气流与轻金属谐振", -25)

    x = blank(.79)
    place(x, breath(.52, .075, .085, 400, 5500), gain=.23)
    place(x, glass(.72, 932.33, decay=.22, metal=True), .015, .19)
    place(x, glass(.62, 1396.91, decay=.14), .04, .05, .12)
    save("hiro_cast_release", x, "剑身光芒亮起：饱满刃鸣与向外流散的短光气", -23)


def sherry():
    x = blank(.53)
    place(x, breath(.28, .045, .035, 280, 4600), gain=.22)
    place(x, body(.30), .022, .31)
    place(x, breath(.36, .085, .060, 1000, 5800), .025, .08)
    save("sherry_attack", x, "怪力直拳：拳风、紧实冲击、短空气冲击环", -21.5, .05)

    x = blank(.43)
    place(x, body(.27, .48), gain=.27)
    place(x, breath(.30, .028, .037, 500, 4200), gain=.11)
    save("sherry_hit", x, "受击格挡：有重量的短接触声与衣袖摩擦", -23.5, .05)

    x = blank(.46)
    place(x, breath(.40, .19, .11, 350, 4700), gain=.22)
    place(x, glass(.36, 659.26, attack=.025, decay=.12), .03, .065, -.14)
    place(x, glass(.32, 987.77, attack=.025, decay=.11), .10, .046, .14)
    save("sherry_cast_gather", x, "双拳蓄力：收束空气压力与青蓝细光，不含持续低频轰鸣", -24.5)

    x = blank(.77)
    place(x, body(.22, .45), gain=.20)
    place(x, breath(.48, .078, .08, 320, 5300), gain=.25)
    place(x, glass(.64, 1318.51, decay=.18), .022, .065, .11)
    save("sherry_cast_release", x, "青蓝拳光放射：短压力爆开与明亮空气尾音", -22)


def arisa():
    x = blank(.64)
    place(x, breath(.33, .055, .045, 260, 5000), gain=.22)
    place(x, fire(.55), .016, .28)
    place(x, body(.19, .35), .035, .14)
    save("arisa_attack", x, "火焰挥拳：破风引燃、贴手火焰翻卷与短爆裂", -22, .055)

    x = blank(.64)
    place(x, body(.20, .36), gain=.20)
    place(x, fire(.56), .006, .23)
    place(x, breath(.32, .055, .046, 1300, 6000), .015, .06)
    save("arisa_hit", x, "交叉火焰格挡：触碰闷击、火舌向两侧散开", -23, .055)

    x = blank(.32)
    place(x, fire(.26), gain=.24, pan=-.28)
    place(x, fire(.25), .025, .22, .28)
    save("arisa_cast_ignite", x, "张开双掌燃起火焰：左右错开点火、细密自然噼啪", -24, .08)

    x = blank(.84)
    place(x, fire(.70, swelling=True), gain=.28)
    place(x, breath(.62, .11, .12, 240, 4800), gain=.23)
    place(x, body(.20, .22), .03, .07)
    save("arisa_cast_flare", x, "红光和双手火焰升腾：加宽的热气翻卷与短焰浪", -23, .07)


def anan():
    x = blank(.69)
    place(x, paper(.40, [(0, .14, .22), (.11, .16, .14)]), gain=.38)
    place(x, breath(.43, .065, .06, 1300, 5800), .07, .16)
    place(x, glass(.53, 1567.98, decay=.14), .08, .10)
    save("anan_attack", x, "书页释放文字光：轻纸擦声、清亮短飞光", -24)

    x = blank(.46)
    place(x, body(.20, .25), gain=.16)
    place(x, paper(.35, [(0, .10, .29), (.08, .12, .18)]), gain=.35)
    place(x, breath(.26, .027, .031, 800, 4600), gain=.08)
    save("anan_hit", x, "抱书退守的轻接触声与纸页衣袖摩擦", -24.5, .05)

    x = blank(.48)
    place(x, paper(.43, [(0, .08, .28), (.10, .10, .23), (.22, .075, .26), (.31, .075, .18)]), gain=.43)
    save("anan_cast_write", x, "低头在笔记本上书写：有停笔和轻重变化的纸上笔触", -27, .035)

    x = blank(.99)
    place(x, paper(.31, [(0, .22, .23)]), gain=.32)
    place(x, breath(.68, .13, .14, 1000, 5400), .018, .15)
    for onset, hz, amp, pan in [(0.025, 783.99, .10, -.15), (.09, 1174.66, .10, .11), (.17, 1567.98, .07, -.08)]:
        place(x, glass(.78, hz, attack=.023, decay=.24), onset, amp, pan)
    save("anan_cast_release", x, "摊书射出紫色文字光：纸页轻翻、细亮层叠泛音与柔光", -24, .15)


def main():
    for make in (ema, hiro, sherry, arisa, anan):
        make()
    REVIEW.mkdir(parents=True, exist_ok=True)
    reel = []
    cue_list = []
    current = 0.0
    for name, sound in REEL:
        gap = np.zeros((round(.38 * SR), 2))
        cue_list.append({"cue": name, "at_seconds": round(current, 3)})
        reel.extend([sound, gap])
        current += (len(sound) + len(gap)) / SR
    preview = REVIEW / "character_animation_sfx_preview.wav"
    with wave.open(str(preview), "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(SR)
        wav.writeframes(np.round(np.concatenate(reel) * 32767).astype("<i2").tobytes())
    report = {
        "source": "Original deterministic synthesis; no voices or audio sampled from reference videos.",
        "sample_rate": SR, "channels": 2, "bits": 16,
        "count": len(MANIFEST), "cues": MANIFEST, "preview_timestamps": cue_list,
        "total_asset_bytes": sum(cue["bytes"] for cue in MANIFEST),
    }
    (REVIEW / "audio_manifest.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps({"count": len(MANIFEST), "bytes": report["total_asset_bytes"], "preview": str(preview)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
