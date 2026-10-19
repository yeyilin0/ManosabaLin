# Error / TheEnd glass effect

`Error` uses its existing `handAttackCount >= 12` condition to decide the presentation.

| Card / condition | Visual and lifetime |
| --- | --- |
| Error, below threshold | 0–2s resisting upper-left → lower-right cut. At 2s irregular local stress pockets break simultaneously; #CC6666 light shows for 1s. Fade out at 3s. No fullscreen spread or burst. |
| Error, threshold met | Same 0–3s intro; full crack network spreads from 3–4s. At 4s stop the presentation timeline and retain the transparent glass overlay until TheEnd. The card finishes normally and its TheEnd token remains playable. |
| TheEnd | Reuse the retained overlay and burst immediately. Rose light becomes saturated luminous pink-white within 0.15s, then remains for a full 2s before disappearing. |
| TheEnd without retained glass | Reconstruct the full crack field and play only the burst/2s hold; no repeated knife intro. |

Error damage resolves at its 2s local crack; cost, counter, HP-loss formula and token
condition are unchanged. TheEnd waits for its flash before killing enemies, keeping the
room present for the entire pink-white hold. Retained nodes belong to the combat room;
leaving combat removes overlay/audio and resolves all waits. A retained overlay is exempt
from the intro watchdog; TheEnd resets it when consuming the overlay. Repeated qualifying
Error cards replace the previous persistent presentation instead of stacking it.

`tools/vfx/build_error_glass.py` generates unequal connected planar fragments, kinked
edges, bundled splinter veins and random off-axis stress pockets. Reference image pixels
are not copied. Data is included explicitly in `export_presets.cfg`. No combat RNG is used.

Independent synthesized sounds: knife scrape at 0s, local break at 2s, spread at 3s only
for qualifying Error, and burst only when TheEnd starts it. All sounds respect SFX volume
and background mute. Overlay rectangles ignore mouse input.

C# compilation and static generated-resource checks passed. Godot/gameplay rendering
has not been verified under the current tool restrictions.
