"""Render original Good Ending VFX audio; no reference-video audio is used.

The class/asset folder is named bad_ending for the game's existing card ID.
opening.wav is a 6 s timeline: courtroom, sword formation, strike at 2.45 s,
bone reaction at 3.05 s, outward water/glass arcs and an airy release.
support.wav (0.7 s) and card.wav (0.9 s) accompany real power/card events.

Run with the repository's available numpy Python runtime. This script renders
44.1 kHz stereo PCM16 assets only; it does not launch playback, Godot or tests.
"""

from pathlib import Path
import json
import wave

import numpy as np


SR = 44100
OUT = Path(__file__).resolve().parents[2] / "ManosabaLin/scenes/Emalin/bad_ending/audio"
RNG = np.random.default_rng(1015305)
TAU = np.pi * 2.0


def ease(value):
    value = np.clip(value, 0.0, 1.0)
    return value * value * (3.0 - 2.0 * value)


def time_axis(seconds):
    return np.arange(round(seconds * SR)) / SR


def air(length, low=850.0, high=5700.0):
    """Soft band-limited turbulence; no pitched rumble or oscillator tremolo."""
    frequencies = np.fft.rfftfreq(length, 1.0 / SR)
    spectrum = 1.0 - np.exp(-((frequencies / low) ** 4))
    spectrum *= np.exp(-((frequencies / high) ** 4))
    spectrum *= (np.maximum(frequencies, 150.0) / 150.0) ** -0.4
    signal = np.fft.irfft(np.fft.rfft(RNG.normal(size=length)) * spectrum, n=length)
    return signal / max(float(np.sqrt(np.mean(signal * signal))), 1e-9)


def glass(frequency, seconds, decay=0.5, attack=0.025, shimmer=0.10):
    """Asymmetric glass modes: short upper modes, a gently relaxing core."""
    time = time_axis(seconds)
    signal = np.zeros(len(time))
    for ratio, level, life in [(1.0, 1.0, 1.0), (2.009, shimmer, 0.48),
                               (2.763, shimmer * 0.34, 0.29), (4.107, shimmer * 0.12, 0.18)]:
        if frequency * ratio > 7400.0:
            continue
        cents = 2.4 * np.exp(-time / 0.15) / ratio
        phase = np.cumsum(frequency * ratio * 2.0 ** (cents / 1200.0)) * TAU / SR
        signal += np.sin(phase) * level * np.exp(-time / (decay * life))
    signal *= ease(time / attack) * ease((seconds - time) / min(0.22, seconds * 0.2))
    return signal


def place(stereo, mono, onset, gain=1.0, pan=0.0):
    start = round(onset * SR)
    count = min(len(mono), len(stereo) - start)
    if start < 0 or count <= 0:
        return
    position = pan[:count] if isinstance(pan, np.ndarray) else pan
    angle = (np.clip(position, -1.0, 1.0) + 1.0) * np.pi / 4.0
    stereo[start:start + count, 0] += mono[:count] * gain * np.cos(angle)
    stereo[start:start + count, 1] += mono[:count] * gain * np.sin(angle)


def current(stereo, onset, duration, pan_from, pan_to, gain, low=850.0, high=5700.0):
    time = time_axis(duration)
    u = time / duration
    # Unequal slow swells suggest a continuous soft pull rather than a motor.
    contour = np.sin(np.pi * u) ** 1.7
    contour *= 0.84 + 0.10 * np.sin(u * 4.3 + 0.3) + 0.06 * np.sin(u * 11.1 + 1.8)
    pan = pan_from + (pan_to - pan_from) * ease(u)
    place(stereo, air(len(time), low, high) * contour, onset, gain, pan)


def grain(stereo, onset, duration, gain, pan, low, high):
    time = time_axis(duration)
    envelope = ease(time / min(0.004, duration * 0.14))
    envelope *= np.exp(-time / (duration * 0.22)) * ease((duration - time) / (duration * 0.3))
    place(stereo, air(len(time), low, high) * envelope, onset, gain, pan)


def room(dry, wet=0.22):
    """Non-looping reflections, with high-frequency absorption on late returns."""
    result = dry.copy()
    for index, delay_ms in enumerate([29, 53, 89, 127, 173, 227, 283, 347, 419, 503, 613, 739]):
        shift = round(delay_ms * SR / 1000.0)
        if shift >= len(dry):
            continue
        source = dry[:, ::-1] if index % 2 else dry
        width = 3 + index * 2
        softened = np.zeros_like(source)
        for offset in range(width):
            if offset == 0:
                softened += source
            else:
                softened[offset:] += source[:-offset]
        softened /= width
        result[shift:] += softened[:-shift] * wet * np.exp(-delay_ms / 380.0) / 2.7
    return result


def sword_strike(stereo):
    # A short blade-through-light contact, a resistant scrape, then a clean
    # glass core. No wet wound sound or heavy cinematic sub-bass.
    current(stereo, 2.33, 0.16, 0.34, -0.05, 0.074, 1200, 6500)
    grain(stereo, 2.45, 0.085, 0.18, -0.04, 1100, 7200)
    grain(stereo, 2.486, 0.12, 0.045, -0.08, 430, 3200)
    place(stereo, glass(587.330, 0.68, 0.18, 0.004, 0.21), 2.45, 0.16, -0.02)
    place(stereo, glass(1568.00, 0.46, 0.105, 0.005, 0.17), 2.456, 0.061, 0.13)
    for onset, length, gain in [(2.466, 0.021, 0.020), (2.498, 0.034, 0.017),
                                (2.535, 0.026, 0.013), (2.574, 0.041, 0.008)]:
        grain(stereo, onset, length, gain, -0.09, 1800, 6200)


def bone_reaction(stereo):
    # Irregular little fibrous clicks grow into a second, distinct ivory hit.
    # Broad noise and inharmonic glass avoid repeated creaks or musical beeps.
    current(stereo, 2.60, 0.48, -0.28, 0.18, 0.027, 520, 3900)
    for onset, length, gain, pan in [
        (2.62, 0.043, 0.021, -0.28), (2.708, 0.050, 0.025, -0.10),
        (2.762, 0.036, 0.021, 0.15), (2.831, 0.063, 0.029, -0.18),
        (2.905, 0.052, 0.035, 0.21), (2.964, 0.039, 0.026, 0.05),
    ]:
        grain(stereo, onset, length, gain, pan, 760, 5100)
    grain(stereo, 3.05, 0.10, 0.145, 0.14, 700, 6500)
    place(stereo, glass(783.991, 0.78, 0.24, 0.003, 0.29), 3.05, 0.128, 0.15)
    place(stereo, glass(1760.00, 0.55, 0.16, 0.006, 0.22), 3.063, 0.063, -0.13)
    for onset, hz, gain, pan in [(3.092, 2489.016, 0.020, -0.32),
                                (3.127, 2093.005, 0.016, 0.39),
                                (3.203, 1396.913, 0.014, -0.19)]:
        place(stereo, glass(hz, 0.51, 0.13, 0.008, 0.18), onset, gain, pan)


def make_opening():
    result = np.zeros((round(6.0 * SR), 2))
    # A tiny room opening, not continuous background music. The distant window
    # harmonics are staggered and quiet enough to leave the action readable.
    current(result, 0.00, 0.97, -0.53, 0.28, 0.015, 630, 4000)
    current(result, 0.19, 0.91, 0.68, -0.22, 0.009, 1750, 5800)
    for onset, hz, gain, pan in [(0.06, 587.330, 0.034, -0.49),
                                (0.29, 880.000, 0.019, 0.38),
                                (0.53, 1396.913, 0.012, -0.23)]:
        place(result, glass(hz, 1.4, 0.44, 0.07), onset, gain, pan)

    # Streams converge at the right hand, slowing as the crystal spine forms.
    for onset, span, left, right, gain in [
        (1.13, 0.94, -0.75, 0.13, 0.025), (1.24, 0.87, 0.80, 0.17, 0.020),
        (1.39, 0.79, -0.23, 0.12, 0.014), (1.61, 0.58, 0.55, 0.11, 0.010),
    ]:
        current(result, onset, span, left, right, gain, 1200, 6300)
    for onset, hz, gain, pan in [(1.29, 440.000, 0.026, 0.17),
                                (1.53, 659.255, 0.028, 0.15),
                                (1.74, 1174.659, 0.023, 0.13),
                                (1.95, 1760.000, 0.018, 0.11),
                                (2.13, 2349.318, 0.013, 0.09)]:
        place(result, glass(hz, 0.92, 0.25, 0.018, 0.18), onset, gain, pan)
    # Minute crystal catches occur independently of the note sequence.
    for onset, pan in [(1.42, -0.33), (1.705, 0.41), (1.845, -0.13), (2.055, 0.18)]:
        grain(result, onset, 0.028, 0.005, pan, 2600, 7200)

    sword_strike(result)
    bone_reaction(result)

    # Curved arcs emerge at different times and move away from the center.
    # Their noise bands overlap like a fluid wash, never a rhythmic engine.
    for onset, span, left, right, gain, low in [
        (3.25, 0.78, -0.04, -0.85, 0.044, 650),
        (3.37, 0.91, 0.08, 0.87, 0.039, 850),
        (3.58, 0.73, -0.09, -0.65, 0.027, 1250),
        (3.73, 0.90, 0.11, 0.76, 0.023, 1600),
        (4.09, 0.69, 0.04, -0.79, 0.014, 2200),
        (4.26, 0.78, -0.04, 0.69, 0.011, 2700),
    ]:
        current(result, onset, span, left, right, gain, low, 6800)
    for onset, hz, gain, pan in [(3.34, 1174.659, 0.032, -0.12),
                                (3.46, 1396.913, 0.021, 0.24),
                                (3.68, 1760.000, 0.018, -0.37),
                                (3.89, 2349.318, 0.012, 0.55),
                                (4.17, 2093.005, 0.010, -0.70)]:
        place(result, glass(hz, 1.2, 0.38, 0.027), onset, gain, pan)

    # Unresolved, restrained D-minor colour beneath the closing tableau. The
    # high modes disappear first; silence follows instead of an ambient loop.
    for onset, hz, gain, pan in [(3.49, 293.665, 0.023, -0.10),
                                (3.55, 440.000, 0.026, 0.12),
                                (3.62, 698.456, 0.022, -0.16),
                                (3.69, 880.000, 0.011, 0.19)]:
        place(result, glass(hz, 2.5, 0.85, 0.13, 0.055), onset, gain, pan)
    current(result, 4.54, 1.30, -0.60, 0.15, 0.006, 1500, 4600)
    current(result, 4.69, 1.17, 0.59, -0.17, 0.005, 2000, 5700)
    return room(result, 0.24)


def make_support():
    result = np.zeros((round(0.7 * SR), 2))
    current(result, 0.0, 0.39, -0.27, 0.28, 0.008, 1800, 5900)
    place(result, glass(1174.659, 0.7, 0.17, 0.015, 0.08), 0.018, 0.059, -0.12)
    place(result, glass(1760.000, 0.60, 0.14, 0.025, 0.06), 0.080, 0.021, 0.17)
    grain(result, 0.028, 0.022, 0.004, 0.02, 2600, 6100)
    return room(result, 0.13)


def make_card():
    result = np.zeros((round(0.9 * SR), 2))
    current(result, 0.00, 0.51, -0.44, 0.14, 0.021, 1000, 5100)
    current(result, 0.20, 0.51, 0.34, -0.07, 0.010, 2300, 6600)
    # Airy paper fibres, then a glass glint as the actual card settles.
    for onset, span, gain in [(0.06, 0.047, 0.008), (0.117, 0.035, 0.006),
                              (0.193, 0.060, 0.005), (0.277, 0.040, 0.004)]:
        grain(result, onset, span, gain, -0.11, 1850, 4800)
    place(result, glass(880.000, 0.70, 0.20, 0.025, 0.09), 0.18, 0.050, 0.06)
    place(result, glass(1396.913, 0.57, 0.17, 0.023, 0.06), 0.29, 0.027, -0.13)
    place(result, glass(1760.000, 0.43, 0.13, 0.017, 0.06), 0.43, 0.012, 0.20)
    return room(result, 0.16)


def save(filename, stereo, duration, peak):
    time = np.arange(len(stereo)) / SR
    stereo -= np.mean(stereo, axis=0)
    stereo *= (ease(time / 0.009) * ease((duration - time) / min(0.26, duration * 0.2)))[:, None]
    stereo *= peak / max(float(np.max(np.abs(stereo))), 1e-9)
    # Fixed linear attenuation keeps transient shape and guarantees headroom;
    # no saturation, clipped transients, limiter pumping or automatic gain.
    pcm = np.round(stereo * 32767.0).astype("<i2")
    pcm[0] = 0
    pcm[-1] = 0
    with wave.open(str(OUT / filename), "wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(SR)
        output.writeframes(pcm.tobytes())
    print(json.dumps({"file": str(OUT / filename), "seconds": duration,
                      "sample_rate": SR, "channels": 2, "pcm_bits": 16,
                      "headroom_peak": peak, "pcm_bytes": pcm.nbytes}, ensure_ascii=False))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    save("opening.wav", make_opening(), 6.0, 0.62)
    save("support.wav", make_support(), 0.7, 0.25)
    save("card.wav", make_card(), 0.9, 0.30)


if __name__ == "__main__":
    main()
