# Explosion Magic VFX

The `ExplosionMagic` card owns this 7-second scene through `ExplosionMagicVfx`.

| Time | Presentation |
| --- | --- |
| 0–2 s | Ten unequal sigil disks reveal in order, forming a horizontal hourglass from the left half of player space to the enemy side. Original recording's unfold sound. |
| 2–3.75 s | Battlefield darkens. Eight continuously bending, water-like streams arrive from outside the viewport and merge in a 700×520 translucent basin at the leftmost array. Cyan, pink, green, gold, violet and white four-point stars flow inward. A deforming inner mass grows to the visible width of the smallest projected sigil (63.36 at reference width 1920). Original procedural charge sound plays from 2–4 seconds. |
| 3.75–3.8 s | Mass erupts wider without shrinking; flowing contour changes into irregular flame tongues. |
| 3.8–4 s | Burning mass travels through the disks, widening progressively to the largest sigil's full aperture (770 at reference width 1920). An advected magic/flame jet follows; sigils brighten. |
| 4–4.08 s | Black flash then white flash. |
| 4.08–5 s | Card damage synchronizes at impact. Layered rolling fire, smoke, pressure streaks and air refraction. Original recording's explosion sound. |
| 5–7 s | A second train of seven spreading pressure waves joins the shock rings and six refraction fronts. Ballistic embers radiate outward, fire dissolves and smoke fades. Original recording's two-second aftershock sound starts at 5 s. |

## Layer construction

The user-provided ArcaneEruption reference supplied ten original textures under `textures/`:
fire masks, fog masks, ring masks, flow/noise, light and palette textures. Copied pixels are
unchanged. This scene uses its own timeline, ribbon geometry and materials; it does not load
the reference's sampled geometry caches or reproduce that archive's original animation.

Fourteen smoke billows and twenty-four fire lobes use independent positions, rotations,
growth and noise-driven dissolution. Three textured shock rings supplement the geometry
pressure fronts. Magic streams use cubic Bezier geometry, low-frequency meandering,
changing width and advected noise with luminous channels. Connected triangle-strip quads
and a bright continuous opacity floor keep the broad streams visible along their entire
length. 190 bright colored stars gather at roughly twice the previous speed. Sigils
retain their release brightness surge; outward radial rays have been removed.
The energy mass uses a
noise-warped asymmetric contour and violet/blue-white emission; it has no closed circular
shell. Its shot grows to the largest sigil aperture and drags a ragged flame jet toward
the enemy without a compression phase.

Air refraction explicitly copies the framebuffer after the effect layers, so the pressure
front bends both the underlying battlefield and the newly drawn fire.

Unfold, charge, fireball launch, detonation and aftershock cues play at 0, 2, 3.75, 4.08 and 5 seconds.
The original generated launch cue is 0.33 seconds: a hot pressure flare followed by a forward,
airy flame rush and a faint crystalline edge. `tools/vfx/synthesize_fireball_launch.py` reads
no reference audio; its tail ends at impact.
Charge is independently synthesized by `tools/vfx/synthesize_magic_charge.py`: fluid band-limited
noise, stationary crystal resonances, glass partials and short stereo reflections. It reads no reference
recording. `charge_synth.ogg` is the active cue; the older `charge.ogg` is unreferenced.
Unfold gain is increased by 6 dB with the existing limiter retained. The other three cues are cut
from the supplied recording's mixed track, not isolated stems. `tools/vfx/build_explosion_assets.py`
records source timestamps; it writes only this effect's assets. Unreferenced older audio
files do not play.

## Integration and validation

Simulation without combat visuals skips the scene. Room removal resolves pending waits;
an 8-second scene has a 9-second presentation watchdog to prevent a stalled card if a
script fails. Damage, debuffs, X cost and once-per-combat behavior remain in the card.

C# was compiled with dependency deployment disabled and all build output directed into
`.local/build-verify`. Resource paths and reference-texture hashes were checked statically.
Godot rendering and in-game visual quality have not been validated for this revision:
the user restricted the Godot validation tool after its attempt to access external config.
