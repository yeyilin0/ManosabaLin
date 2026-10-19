"""Copy the selected user-provided VFX textures, preserving original pixels."""
import argparse
import shutil
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('source', type=Path)
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
target = root/'ManosabaLin/scenes/Yalisalin/explosion_magic/textures'
target.mkdir(parents=True, exist_ok=True)
names = ['ef_fire004_tra.png', 'ef_fire002.png', 'ef_fog001.png', 'ef_fog002.png',
         'ef_fog003.png', 'ef_ring010.png', 'ef_Bumps003.png', 'ef_Bumps006.png',
         'ef_light003.png', 'ef_PaletteR_001.png']
for name in names:
    shutil.copy2(args.source/'vfx/10032310_ArcaneEruption/btl_fire_all_2/textures'/name,target/name)
print(f'Copied {len(names)} original textures into the project.')
