"""Original clean butterfly line art, without decorative dots or wing motifs.

Ten related but distinct wing silhouettes. No reference image pixels are read/copied.
"""
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
dest=ROOT/'ManosabaLin/scenes/Ananlin/draft_butterflies'
dest.mkdir(parents=True,exist_ok=True)
for i in range(10):
    w=153+(i*11)%34
    h=133+(i*13)%29
    lower=75+(i*7)%29
    elements=['<svg xmlns="http://www.w3.org/2000/svg" width="520" height="460" viewBox="0 0 520 460">',
              '<g transform="translate(260 230)" fill="none" stroke="white" stroke-width="3.2" stroke-linejoin="round" stroke-linecap="round">']
    for side in [-1,1]:
        elements.append(f'<g transform="scale({side} 1)">')
        # Long tapered top wing and a soft, scalloped lower wing; no busy vein grid.
        elements.append(f'<path d="M 0,-7 C 27,-{h*.5} {w*.42},-{h*.93} {w},-{h} C {w+30},-{h+12} {w*.97},-{h*.54} {w*.58},-18 Q {w*.33},8 8,15 C {w*.52},-17 {w*.96},-11 {w*.85},28 C {w+3},55 {w*.69},69 {w*.81},{lower} C {w*.85},{lower+37} {w*.48},{lower+25} {w*.45},{lower+34} Q {w*.24},{lower+47} 26,61 C 12,31 6,22 0,8"/>')
        elements.append(f'<path d="M 4,-3 Q {w*.34},-{h*.12} {w*.62},-{h*.56}" stroke-width="1.7"/>')
        elements.append('</g>')
    # Single body and light antennae.
    elements.append('<path d="M-1,-26 Q10,17 -1,56 M-1,-18 Q-9,-52 -28,-59 M3,-18 Q12,-52 29,-59"/>')
    elements+=['</g></svg>']
    (dest/f'butterfly_{i}.svg').write_text('\n'.join(elements),encoding='utf-8')
print('Generated 10 clean butterfly outlines without petal/star/dot decorations.')
