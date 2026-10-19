"""Original procedural 2-second magic charge. No reference recording is read.

Soft liquid air, stationary crystalline resonances and shimmering motes; reproducible stereo.
Usage: python tools/vfx/synthesize_magic_charge.py --ffmpeg FILE
"""
import argparse
from pathlib import Path
import subprocess
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
RATE = 48000
DURATION = 2.0


def filtered_noise(rng, count, low, high):
    frequencies = np.fft.rfftfreq(count, 1 / RATE)
    spectrum = np.fft.rfft(rng.standard_normal(count))
    high_pass = 1 - np.exp(-(frequencies / low) ** 4)
    low_pass = np.exp(-(frequencies / high) ** 4)
    sound = np.fft.irfft(spectrum * high_pass * low_pass, n=count)
    return sound / max(np.std(sound), 1e-8)


def synthesize():
    rng = np.random.default_rng(481203)
    count = round(RATE * DURATION)
    time = np.arange(count) / RATE
    progress = time / DURATION
    rise = progress * progress * (3 - 2 * progress)
    attack = np.clip(time / .16, 0, 1)
    release = np.clip((DURATION - time) / .18, 0, 1)
    envelope = np.sin(attack * np.pi / 2) ** 2 * np.sin(release * np.pi / 2) ** 2
    envelope *= .30 + .70 * rise
    stereo = np.zeros((count, 2))

    # Airy continuous gathering: no bass oscillator, revving pitch sweep or motor flutter.
    for channel in range(2):
        air = filtered_noise(rng, count, 1400, 6500)
        fluid = filtered_noise(rng, count, 700, 2300)
        modulation = .85 + .10 * np.sin(time * 2.1 + channel * 1.3)
        stereo[:, channel] += air * (.012 + rise * .028) * modulation
        stereo[:, channel] += fluid * .022 * modulation

    # Slowly blooming high crystal tones keep pitch stable; accumulation comes from density.
    for index, (frequency, amplitude) in enumerate([(659.25,.032),(987.77,.029),
                                                   (1318.51,.020),(1975.53,.016),(2637.02,.009)]):
        bloom = np.clip((time-index*.16)/.38,0,1)
        bloom = bloom*bloom*(3-2*bloom)
        phase = 2*np.pi*frequency*time + rng.uniform(0,2*np.pi)
        tone = np.sin(phase)*amplitude*bloom
        pan = .5 + .25 * np.sin(index * 2.1 + time * .45)
        stereo[:, 0] += tone * np.sqrt(1 - pan)
        stereo[:, 1] += tone * np.sqrt(pan)

    # Glassy highlights drift in smoothly, without separate ticking/transient noises.
    for index in range(14):
        start = .08 + index * .11 + rng.uniform(-.025,.025)
        age = time - start
        highlight = np.where(age >= 0, (1 - np.exp(-np.maximum(age, 0) * 16))
                             * np.exp(-np.maximum(age, 0) * 3.8), 0)
        frequency = [1318.51,1760,1975.53,2637.02,3520][index%5]
        phase = 2*np.pi*frequency*time + rng.uniform(0,2*np.pi)
        tone = (np.sin(phase)+.18*np.sin(phase*1.997))*highlight*.013
        stereo[:, index % 2] += tone
        stereo[:, 1 - index % 2] += tone * .45

    stereo *= envelope[:, None]
    # Short stereo reflections soften the tone and create a broad gathering space.
    dry = stereo.copy()
    for delay_seconds, gain in [(.061,.20),(.113,.16),(.179,.10),(.263,.06)]:
        delay = round(delay_seconds * RATE)
        stereo[delay:] += dry[:-delay, ::-1] * gain
    stereo *= release[:, None] ** 2
    # Linear gain preserves the delicate sound instead of introducing buzzy saturation.
    stereo *= .58 / max(float(np.max(np.abs(stereo))), 1e-8)
    stereo[0] = 0
    stereo[-1] = 0
    assert np.isfinite(stereo).all()
    assert float(np.max(np.abs(stereo))) <= .581
    return stereo


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--ffmpeg', type=Path, required=True)
    args = parser.parse_args()
    destination = ROOT / 'ManosabaLin/audio/explosion_magic'
    destination.mkdir(parents=True, exist_ok=True)
    sound = synthesize()
    wav = destination / 'charge_synth.wav'
    with wave.open(str(wav), 'wb') as writer:
        writer.setnchannels(2)
        writer.setsampwidth(2)
        writer.setframerate(RATE)
        writer.writeframes((sound * 32767).astype('<i2').tobytes())
    subprocess.run([str(args.ffmpeg), '-hide_banner', '-loglevel', 'error', '-y',
                    '-i', str(wav), '-c:a', 'libvorbis', '-q:a', '6',
                    str(destination / 'charge_synth.ogg')], check=True)
    print(f'Original synthetic charge: {DURATION:.2f}s, stereo {RATE}Hz, peak={np.max(np.abs(sound)):.3f}')


if __name__ == '__main__':
    main()
