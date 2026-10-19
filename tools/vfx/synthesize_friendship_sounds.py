"""Original Friendship audio: a gentle two-note meeting, clasp, bloom and petals.

No reference-video audio is copied. Deterministic, 44.1 kHz stereo PCM16.
The opening matches a 4.2 s animation; passive feedback stays deliberately quiet.
"""
from pathlib import Path
import wave

import numpy as np


SR = 44100
OUT = Path(__file__).resolve().parents[2] / "ManosabaLin/scenes/Sherrylin/friendship/audio"
RNG = np.random.default_rng(831104)


def smoothstep(x):
    x = np.clip(x, 0.0, 1.0)
    return x * x * (3.0 - 2.0 * x)


def air_noise(length, low=600.0, high=5400.0):
    """Smooth band limits, with a downward spectral slope to avoid fizzy hiss."""
    frequencies = np.fft.rfftfreq(length, 1 / SR)
    band = (1.0 - np.exp(-((frequencies / low) ** 4)))
    band *= np.exp(-((frequencies / high) ** 4))
    band *= (np.maximum(frequencies, 180.0) / 180.0) ** -0.4
    result = np.fft.irfft(np.fft.rfft(RNG.normal(size=length)) * band, n=length)
    return result / max(np.sqrt(np.mean(result * result)), 1e-9)


def bell(frequency, length, decay=0.75, attack=0.018):
    """Soft struck-glass tone; upper inharmonic partials disappear first."""
    time = np.arange(length) / SR
    tone = np.zeros(length)
    for ratio, amplitude, tail in [
        (1.0, 1.0, 1.0), (2.002, 0.19, 0.64),
        (2.757, 0.07, 0.43), (4.014, 0.035, 0.30),
    ]:
        if frequency * ratio > 7500:
            continue
        phase = RNG.uniform(-0.06, 0.06)
        tone += amplitude * np.sin(2 * np.pi * frequency * ratio * time + phase) * np.exp(-time / (decay * tail))
    return tone * smoothstep(time / attack)


def add_panned(stereo, mono, onset, gain=1.0, pan=0.0):
    start = round(onset * SR)
    count = min(len(mono), len(stereo) - start)
    if count <= 0:
        return
    angle = (np.clip(pan, -1, 1) + 1) * np.pi / 4
    stereo[start:start + count, 0] += mono[:count] * gain * np.cos(angle)
    stereo[start:start + count, 1] += mono[:count] * gain * np.sin(angle)


def soft_room(dry, wet=0.16):
    """Diffuse short space from non-periodic, independently spread reflections."""
    result = dry.copy()
    # Different prime-like delays prevent the regular flutter/engine quality of
    # repeated taps. No feedback means quiet tails cannot build into a drone.
    for index, milliseconds in enumerate([29, 43, 67, 89, 113, 149, 181, 227, 281, 337]):
        delay = round(milliseconds * SR / 1000)
        amplitude = wet * np.exp(-milliseconds / 170) / 2.5
        source = dry[:, ::-1] if index % 2 else dry
        result[delay:] += source[:-delay] * amplitude
    return result


def save(name, stereo, peak_target):
    time = np.arange(len(stereo)) / SR
    duration = len(stereo) / SR
    stereo = stereo - np.mean(stereo, axis=0)
    stereo *= (smoothstep(time / 0.007) * smoothstep((duration - time) / 0.15))[:, None]
    # Normalize with linear gain only: no saturation distortion or hard limiting.
    peak = np.max(np.abs(stereo))
    stereo *= peak_target / max(peak, 1e-9)
    pcm = np.round(stereo * 32767).astype("<i2")
    with wave.open(str(OUT / name), "wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(SR)
        output.writeframes(pcm.tobytes())
    rms = np.sqrt(np.mean(stereo * stereo))
    print(f"{name}: {duration:.3f}s, peak={np.max(np.abs(stereo)):.6f}, "
          f"RMS={rms:.6f} ({20*np.log10(max(rms,1e-9)):.2f} dBFS), "
          f"clipped_samples={np.count_nonzero(np.abs(pcm.astype(np.int32)) >= 32767)}")


def make_opening():
    length = round(4.2 * SR)
    time = np.arange(length) / SR
    result = np.zeros((length, 2))
    # Two colors approach gently from opposite sides: D5 and A5.
    add_panned(result, bell(587.33, round(1.7 * SR), 0.56, 0.027), 0.12, 0.145, -0.40)
    add_panned(result, bell(880.00, round(1.6 * SR), 0.62, 0.032), 0.31, 0.125, 0.40)
    entry = smoothstep(time / 0.28) * smoothstep((1.20 - time) / 0.62)
    for channel in range(2):
        result[:, channel] += air_noise(length, 850, 5100) * entry * 0.012

    # A luminous major-sixth harmony settles as the hands finish clasping.
    for onset, frequency, gain, pan in [
        (1.50, 587.33, 0.12, -0.16), (1.52, 739.99, 0.095, 0.12),
        (1.54, 880.00, 0.08, -0.25), (1.56, 987.77, 0.055, 0.27),
        (1.57, 1174.66, 0.035, 0.00),
    ]:
        add_panned(result, bell(frequency, length, 0.82, 0.075), onset, gain, pan)

    # A slowly breathing, non-pitched rush blooms and releases into petals.
    swell = smoothstep((time - 2.10) / 0.75) * smoothstep((3.85 - time) / 0.95)
    drift = 0.90 + 0.055 * np.sin(time * 2.7) + 0.045 * np.sin(time * 5.1 + 0.7)
    for channel in range(2):
        broad = air_noise(length, 520, 4700)
        fine = air_noise(length, 1800, 6900)
        result[:, channel] += (broad * 0.018 + fine * 0.005) * swell * drift
    for onset, frequency, pan in [
        (2.74, 1174.66, -0.3), (2.89, 1479.98, 0.3),
        (3.07, 1760.0, -0.45), (3.28, 1975.53, 0.4),
        (3.47, 1479.98, -0.2), (3.68, 1174.66, 0.25),
    ]:
        add_panned(result, bell(frequency, round(0.85 * SR), 0.22, 0.020), onset, 0.018, pan)
    # Sparse soft petal brushes add organic movement without clicky percussion.
    for onset, duration, pan in [(3.03, .16, -.5), (3.21, .19, .4), (3.46, .14, -.2), (3.73, .17, .3)]:
        count = round(duration * SR)
        brush_time = np.linspace(0, 1, count)
        brush = air_noise(count, 1800, 6000) * np.sin(np.pi * brush_time) ** 2
        add_panned(result, brush, onset, .007, pan)
    return soft_room(result, 0.20)


def make_pulse():
    length = round(0.60 * SR)
    time = np.arange(length) / SR
    result = np.zeros((length, 2))
    add_panned(result, bell(1174.66, length, 0.115, 0.013), 0.0, 0.080, -0.1)
    add_panned(result, bell(1760.00, length, 0.080, 0.015), 0.016, 0.019, 0.15)
    veil = smoothstep(time / 0.04) * np.exp(-time / 0.085)
    for channel in range(2):
        result[:, channel] += air_noise(length, 1800, 5500) * veil * .004
    return soft_room(result, 0.12)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    save("open_chime.wav", make_opening(), 0.50)
    save("pulse.wav", make_pulse(), 0.19)


if __name__ == "__main__":
    main()
