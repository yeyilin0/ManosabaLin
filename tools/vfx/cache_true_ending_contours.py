"""Read the existing cutout once and cache light-gathering coordinates, without editing pixels."""
from pathlib import Path
import hashlib
import math
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / 'ManosabaLin/scenes/Emalin/true_ending'
SOURCE = BASE / 'textures/reunion.png'


def random_value(index, salt):
    value = math.sin(index * 127.1 + salt * 311.7) * 43758.5453
    return value - math.floor(value)


def main():
    points = []
    with Image.open(SOURCE) as image:
        assert image.mode == 'RGBA'
        for index in range(1800):
            x, y = random_value(index, 1), random_value(index, 2)
            r, g, b, alpha = image.getpixel((int(x*(image.width-1)), int(y*(image.height-1))))
            if alpha > 255*.25 and r*.2126 + g*.7152 + b*.0722 > 255*.22:
                points.append((x, y))
            if len(points) == 180:
                break
    assert len(points) == 180
    digest = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
    lines = ['extends RefCounted', '# Generated coordinate metadata; no runtime texture readback.',
             '# Refresh with tools/vfx/cache_true_ending_contours.py after changing the cutout.',
             'const SOURCE_SHA256 := "' + digest + '"', 'const POINTS := [']
    lines += ['\tVector2(%.8f, %.8f),' % point for point in points]
    lines += [']', '']
    target = BASE / 'reunion_contours.gd'
    target.write_text('\n'.join(lines), encoding='utf-8')
    print(str(target), len(points), 'points, source SHA256', digest)


if __name__ == '__main__':
    main()
