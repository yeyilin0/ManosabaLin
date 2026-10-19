"""Build deterministic sigils and cut the user-provided reference recording.

Usage: python tools/vfx/build_explosion_assets.py --video FILE --ffmpeg FILE
Only writes this effect's own assets. Audio is edited source mix, not isolated stems.
"""
import argparse
import math
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'ManosabaLin/scenes/Yalisalin/explosion_magic'
AUDIO = ROOT / 'ManosabaLin/audio/explosion_magic'


def point(radius, angle):
    return f'{512 + radius * math.cos(angle):.2f},{512 + radius * math.sin(angle):.2f}'


def sigil(variant):
    elements = ['<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024" viewBox="0 0 1024 1024">',
                '<g fill="none" stroke="white" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">']
    for radius, width in [(478, 4), (465, 2), (424, 3), (414, 1.5), (316, 2), (212, 3), (90, 2), (73, 4)]:
        elements.append(f'<circle cx="512" cy="512" r="{radius}" stroke-width="{width}"/>')
    n = 6 + variant * 2
    for offset in [0, math.pi / n]:
        elements.append('<polygon points="' + ' '.join(point(405, offset + i * 2 * math.pi / n) for i in range(n)) + '"/>')
    for i in range(n):
        angle = 2 * math.pi * i / n
        cx, cy = 512 + 200 * math.cos(angle), 512 + 200 * math.sin(angle)
        elements.append(f'<circle cx="{cx:.2f}" cy="{cy:.2f}" r="200" stroke-width="1.6"/>')
        elements.append(f'<polyline points="{point(90,angle)} {point(310,angle+.11)} {point(405,angle)} {point(310,angle-.11)} {point(90,angle)}"/>')
    # Geometric rune marks, never fonts: reproducible across editor and export.
    for i in range(72):
        a = 2 * math.pi * i / 72
        elements.append(f'<path d="M {point(434,a-.011)} L {point(453,a-.011)} L {point(446,a+.014)} L {point(434,a+.014)}"/>')
        if i % 3 == variant:
            elements.append(f'<path d="M {point(440,a-.016)} L {point(449,a+.018)}"/>')
    for i in range(12):
        a = 2 * math.pi * i / 12
        elements.append(f'<polyline points="{point(482,a)} {point(494,a+.008)} {point(503,a)} {point(494,a-.008)} {point(482,a)}"/>')
    elements += ['</g></svg>']
    (ART / f'sigil_{variant}.svg').write_text('\n'.join(elements), encoding='utf-8')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--video', type=Path, required=True)
    parser.add_argument('--ffmpeg', type=Path, required=True)
    args = parser.parse_args()
    ART.mkdir(parents=True, exist_ok=True)
    AUDIO.mkdir(parents=True, exist_ok=True)
    for variant in range(3):
        sigil(variant)
    # name, recording start, recording length, output length, gain dB.
    cuts = [('unfold', 41.6, 3.6, 2.0, 2),
            ('detonation', 105.25, 3.0, 2.92, -3),
            ('aftershock', 112.2, 2.0, 2.0, -5)]
    for name, start, length, duration, gain in cuts:
        filters = (f'atempo={length/duration:.8f},atrim=duration={duration},asetpts=PTS-STARTPTS,'
                   f'afade=t=in:d=0.035,afade=t=out:st={duration-.22}:d=0.22,'
                   f'volume={gain}dB,alimiter=limit=0.9:level=false')
        subprocess.run([str(args.ffmpeg), '-hide_banner', '-loglevel', 'error', '-y',
                        '-ss', str(start), '-i', str(args.video), '-t', str(length), '-vn',
                        '-af', filters, '-ar', '48000', '-ac', '2', '-c:a', 'libvorbis', '-q:a', '6',
                        str(AUDIO / f'{name}.ogg')], check=True)
    print('Built 3 sigils and 3 reference-audio cues; synthetic charge is built separately.')


if __name__ == '__main__':
    main()
