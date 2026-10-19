"""Original True Ending sound design; no audio from the video reference.

5.0 s: two colored nuclei / a turning veil / embrace / flowers / outward release.
0.8 s: quiet memory response. 1.5 s: a completed flower cluster.
Deterministic synthesis with numpy; 44.1 kHz stereo PCM16, no dependencies beyond
numpy. Run from any directory. The written WAVs are read back and verified.
"""
from pathlib import Path
import json
import wave

import numpy as np


SR = 44100
OUT = Path(__file__).resolve().parents[2] / "ManosabaLin/scenes/Emalin/true_ending/audio"
RNG = np.random.default_rng(1031413)


def smoothstep(x):
    x = np.clip(x, 0.0, 1.0)
    return x * x * (3.0 - 2.0 * x)


def air(length, low=700.0, high=5700.0):
    """Rounded, non-pitched noise with soft spectral edges and no bass rumble."""
    frequencies = np.fft.rfftfreq(length, 1.0 / SR)
    band = (1.0 - np.exp(-((frequencies / low) ** 4)))
    band *= np.exp(-((frequencies / high) ** 4))
    band *= (np.maximum(frequencies, 100.0) / 100.0) ** -0.45
    mono = np.fft.irfft(np.fft.rfft(RNG.normal(size=length)) * band, n=length)
    return mono / max(np.sqrt(np.mean(mono * mono)), 1e-9)


def glass(frequency, seconds, decay=0.65, attack=0.025, softness=1.0):
    """Soft glass with tiny asymmetric pitch drift; upper partials settle first."""
    time = np.arange(round(seconds * SR)) / SR
    tone = np.zeros(len(time))
    ratios = [(1.0, 1.0, 1.0), (2.004, .15, .56), (2.756, .043, .39), (4.015, .018, .22)]
    for ratio, amplitude, falloff in ratios:
        if frequency * ratio > 7200.0:
            continue
        # A few cents of one-way relaxation avoids a static oscillator tone.
        cents = (2.0 / ratio) * np.exp(-time / .16)
        phase = np.cumsum(frequency * ratio * 2.0 ** (cents / 1200.0)) * (2.0 * np.pi / SR)
        envelope = np.exp(-time / (decay * falloff))
        tone += np.sin(phase) * amplitude * envelope
    tone *= smoothstep(time / attack)
    tone *= smoothstep((seconds - time) / max(.06, min(.25, seconds * .2)))
    if softness > 0.0:
        friction = air(len(time), 2100, 5300)
        tone += friction * smoothstep(time / .017) * np.exp(-time / .055) * .006 * softness
    return tone


def place(stereo, mono, onset, gain=1.0, pan=0.0):
    start = round(onset * SR)
    count = min(len(mono), len(stereo) - start)
    if start < 0 or count <= 0:
        return
    if isinstance(pan, np.ndarray):
        angle = (np.clip(pan[:count], -1.0, 1.0) + 1.0) * np.pi / 4
    else:
        angle = (np.clip(pan, -1.0, 1.0) + 1.0) * np.pi / 4
    stereo[start:start + count, 0] += mono[:count] * gain * np.cos(angle)
    stereo[start:start + count, 1] += mono[:count] * gain * np.sin(angle)


def diffuse_space(dry, wet=.20):
    """Irregular reflections with different widths; no repeating feedback loop."""
    result = dry.copy()
    for index, ms in enumerate([23, 41, 73, 101, 139, 173, 211, 263, 307, 359, 421, 487]):
        delay = round(ms * SR / 1000.0)
        if delay >= len(result):
            continue
        source = dry[:, ::-1] if index % 2 else dry
        # A small rolling average removes the hard edge from late reflections.
        smoothing = 3 + 2 * (index // 3)
        softened = np.zeros_like(source)
        for offset in range(smoothing):
            if offset == 0:
                softened += source
            else:
                softened[offset:] += source[:-offset]
        softened /= smoothing
        result[delay:] += softened[:-delay] * wet * np.exp(-ms / 310.0) / 2.8
    return result


def add_brush(result, onset, duration, pan_from, pan_to, gain=.005):
    length = round(duration * SR)
    u = np.linspace(0.0, 1.0, length)
    envelope = np.sin(np.pi * u) ** 2
    uneven = .88 + .075 * np.sin(u * 7.0 + .2) + .045 * np.sin(u * 16.3)
    breath = air(length, 1350, 5900) * envelope * uneven
    pan = pan_from + (pan_to - pan_from) * smoothstep(u)
    place(result, breath, onset, gain, pan)


def make_opening():
    duration = 5.0
    length = round(SR * duration)
    time = np.arange(length) / SR
    result = np.zeros((length, 2))

    # Pale pink on the left, dark red on the right. Warm red lives in the middle
    # register, never in a sub-bass / engine band. Both travel towards the center.
    left = glass(739.989, 1.75, .71, .047)
    right = glass(493.883, 1.76, .79, .060)
    place(result, left, .10, .108, -.65 * (1.0 - smoothstep(np.arange(len(left)) / SR / 1.45)))
    place(result, right, .28, .101, .64 * (1.0 - smoothstep(np.arange(len(right)) / SR / 1.42)))

    # Several broad independent swells overlap, with smooth, non-repeating pan.
    # They form a continuous current without a mechanical tremolo or pulse.
    for onset, span, pan_a, pan_b, gain in [
        (.00, 1.23, -.82, .08, .013), (.18, 1.32, .79, -.10, .011),
        (.67, 1.33, -.28, .54, .010), (.93, 1.18, .48, -.36, .009),
    ]:
        add_brush(result, onset, span, pan_a, pan_b, gain)

    # Fine glints travel through the turning veil, becoming slightly clearer
    # close to the embrace. The phrase is intentionally sparse and off-grid.
    for onset, hz, gain, pan in [
        (.79, 987.767, .026, -.42), (1.09, 1108.731, .022, .39),
        (1.39, 1479.978, .018, -.16), (1.64, 1975.533, .009, .12),
    ]:
        place(result, glass(hz, 1.23, .37, .032), onset, gain, pan)

    # A warm E major add-six harmony: soft attacks and staggered 20–40 ms entries
    # create an embrace, not a struck impact. No voice or sustained heavy bass.
    for onset, hz, gain, pan in [
        (1.80, 329.628, .055, -.08), (1.83, 493.883, .071, .15),
        (1.87, 659.255, .091, -.13), (1.90, 830.609, .059, .23),
        (1.93, 1108.731, .031, -.26), (1.96, 1318.510, .021, .06),
    ]:
        place(result, glass(hz, 2.7, 1.03, .125, .5), onset, gain, pan)

    # One tear followed by separately glowing sleeve flowers: a tiny clear
    # droplet, then airy high notes with slight asymmetry in timing and stereo.
    for onset, hz, gain, pan in [
        (2.22, 1975.533, .020, -.12), (2.37, 2637.020, .012, -.06),
        (2.49, 1318.510, .019, .29), (2.72, 1661.219, .016, .41),
        (2.95, 1975.533, .013, .23), (3.09, 2637.020, .008, .44),
    ]:
        place(result, glass(hz, 1.30, .31, .017), onset, gain, pan)

    # The light opens into the whole screen. Midrange air and a very quiet fine
    # sheen swell independently, with the width growing out from the center.
    swell = smoothstep((time - 2.89) / .60) * smoothstep((4.76 - time) / 1.05)
    texture = .86 + .075 * np.sin(time * 2.91 + .1) + .035 * np.sin(time * 6.71)
    for channel in range(2):
        wide = air(length, 710, 4750)
        fine = air(length, 2450, 6850)
        result[:, channel] += (wide * .023 + fine * .006) * swell * texture
    for onset, hz, gain, pan in [
        (3.24, 1318.510, .036, -.28), (3.30, 1661.219, .028, .24),
        (3.36, 1975.533, .019, -.14), (3.47, 2637.020, .009, .47),
    ]:
        place(result, glass(hz, 1.70, .61, .13), onset, gain, pan)

    # Unequal petal sweeps and sparse pinpricks carry the release to a silent
    # persistent rim. No periodic loop or constant noise remains after 5 s.
    for onset, span, pan_a, pan_b, gain in [
        (3.55, .31, -.18, -.78, .009), (3.76, .40, .14, .71, .010),
        (3.97, .26, -.32, -.65, .008), (4.18, .37, .25, .83, .007),
        (4.46, .29, -.20, -.63, .005), (4.60, .31, .17, .57, .004),
    ]:
        add_brush(result, onset, span, pan_a, pan_b, gain)
    for onset, hz, pan, gain in [(3.87, 2217.462, -.53, .009), (4.17, 1975.533, .49, .008), (4.51, 1661.219, -.31, .005)]:
        place(result, glass(hz, .82, .19, .026), onset, gain, pan)
    return diffuse_space(result, .25)


def make_memory():
    result = np.zeros((round(.8 * SR), 2))
    place(result, glass(1318.510, .8, .18, .021), .015, .070, -.18)
    place(result, glass(1975.533, .71, .15, .025), .066, .025, .19)
    add_brush(result, .03, .40, -.14, .26, .003)
    return diffuse_space(result, .17)


def make_complete():
    result = np.zeros((round(1.5 * SR), 2))
    add_brush(result, .00, .72, -.62, .00, .012)
    add_brush(result, .07, .75, .62, .00, .011)
    for onset, hz, gain, pan in [
        (.12, 659.255, .080, -.26), (.18, 830.609, .064, .21),
        (.25, 987.767, .050, -.15), (.33, 1318.510, .047, .12),
        (.41, 1661.219, .027, -.22), (.48, 1975.533, .017, .30),
    ]:
        place(result, glass(hz, 1.5, .47, .056), onset, gain, pan)
    add_brush(result, .61, .59, -.20, -.62, .006)
    add_brush(result, .70, .62, .20, .63, .005)
    return diffuse_space(result, .23)


def save_verified(filename, stereo, peak_target, duration):
    assert stereo.shape == (round(SR * duration), 2)
    assert np.isfinite(stereo).all()
    time = np.arange(len(stereo)) / SR
    stereo -= np.mean(stereo, axis=0)
    fade = smoothstep(time / .012) * smoothstep((duration - time) / min(.24, duration * .20))
    stereo *= fade[:, None]
    stereo *= peak_target / max(np.max(np.abs(stereo)), 1e-9)
    # Linear gain only; no clipping, saturation or loudness-pumping limiter.
    pcm = np.round(stereo * 32767.0).astype("<i2")
    pcm[0] = 0
    pcm[-1] = 0
    with wave.open(str(OUT / filename), "wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(SR)
        output.writeframes(pcm.tobytes())
    with wave.open(str(OUT / filename), "rb") as recorded:
        assert recorded.getnchannels() == 2
        assert recorded.getsampwidth() == 2
        assert recorded.getframerate() == SR
        assert recorded.getnframes() == round(SR * duration)
        readback = np.frombuffer(recorded.readframes(recorded.getnframes()), dtype="<i2").reshape(-1, 2)
    assert np.array_equal(readback, pcm)
    assert np.max(np.abs(readback.astype(np.int32))) < 32767
    values = readback / 32768.0
    rms = float(np.sqrt(np.mean(values * values)))
    stats = {
        "file": filename, "seconds": duration, "sample_rate": SR,
        "channels": 2, "peak": round(float(np.max(np.abs(values))), 6),
        "rms_dbfs": round(20.0 * np.log10(max(rms, 1e-12)), 2),
        "dc_mean": [round(float(x), 8) for x in np.mean(values, axis=0)],
        "first_sample": readback[0].tolist(), "last_sample": readback[-1].tolist(),
        "clipped_samples": int(np.count_nonzero(np.abs(readback.astype(np.int32)) >= 32767)),
    }
    print(json.dumps(stats, ensure_ascii=False))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    save_verified("opening.wav", make_opening(), .52, 5.0)
    save_verified("memory.wav", make_memory(), .22, .8)
    save_verified("complete.wav", make_complete(), .42, 1.5)


if __name__ == "__main__":
    main()
