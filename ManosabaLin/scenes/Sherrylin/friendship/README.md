# 友谊：牵手回忆

Used by `EmotionFriendship` and `EmotionFriendshipOrb` through `FriendshipVfx`.
Scene: `friendship.tscn`; `setup(player_uv, target_uv, mode)` accepts viewport UVs.
`finished` always releases the opening wait. Game rules and game RNG remain unchanged.

## Main timeline — 4.2 seconds

- 0–0.75: blue/green tapered fluid trails meet; a turbulent pearlescent garden aperture opens.
- 0.48–1.46: matching Sherry/Hanna cuffs approach, fingers touch, then clasp. Exactly one
  atlas pose is sampled per frame: no crossfading extra hand silhouettes. Wrist/approach
  deformation and brief contact glints support the three registered finger poses.
- 1.43–2.3: blue/green light travels from the clasp, assembling large luminous backs across
  the left/right halves of the screen. Light contours, translucent interiors and moving
  filament detail extend beyond the circle. The backs stay **in front of** the garden
  aperture; original clasp brightness is retained, per the latest correction.
- 2.66–3.6: warm-white bloom releases four differently timed broken spectral wave sheets,
  thin caustic highlights, 12 curved filament fragments, and 360 mixed iridescent particles.
- 3.06–4.2: blue/green trails return to the player; particles slow under drag, tumble and
  fall before disappearing. The effect does not hide or replace the combat background.

Draw order: blue/green perimeter → garden aperture → full-screen luminous backs → hands → contact halo → spectral
release → fluid trails / petals. The original video informs the soft irregular aperture,
hand contact/pause, rising illumination and prismatic dispersal, rather than being embedded.

## Orb-owned perimeter field

`friendship_field.tscn` is separate from the 4.2-second opening and all short feedbacks.
Its root is a `CanvasLayer`, with the script on `Stage`. Only a blue/green oversized
ellipse appears: the viewport crops it into four clearly curved corner segments,
with intermittent links dipping into the edges. Three softly breathing lanes carry
thin pearl cores, sparse traveling glints and restrained iridescent reflections.
The center is transparent; it adds no trees, scenery, platform or scene-wide dimming.
Blue/green is distinct from True Ending's silver-blue/pink and Bad Ending's wine/rose.

The field starts when Friendship is played, fades in over 0.62 seconds, and remains
while the associated friendship charge orb exists. The C# presenter owns that
lifetime; the opening finishing does **not** retire it. Orb removal or replacement
by another retained field calls `retire_field()`, fading for 0.55 seconds and emitting
`finished` before the Stage is freed. The presenter then frees the root layer.
Retirement cannot reactivate a previous field. Feedback alone never acquires a slot.

Stage API: `prepare_idle()`, `activate_field()`, `retire_field()`, `prewarm_at(seconds)`,
`seek_preview(seconds)`, `finished` signal. Preparation only hides/stops the instance;
it does not emit completion. Prewarming evaluates the same shader without audio,
orb changes, card actions or acquiring `CombatFieldVfxSlot`.

This retained pass has no textures, particle nodes, screen readback or runtime RNG.
It skips all ring calculations inside the central radius, uses three fixed lanes,
and updates viewport-dependent parameters only on resize. Shader/material objects
are created once per prepared scene. All motion is analytical and frame-rate independent.

## Four short feedbacks — at most 1.15 seconds each

1. Draw: one blue petal to each actual newly drawn visible card.
2. Energy: a green fluid trail to the local player's actual energy counter.
3. Temporary Strength: a pink petal spiral around the player.
4. Exhaust curse/status: peach petals rise from the actual card's pre-exhaust screen position.

Pulses do not delay gameplay. Hand/energy UI feedback is local-owner only. Startup failures,
combat ending, node exit and watchdog timeouts finish safely. Audio follows the game's SFX
volume and background-mute preference.

## Assets and performance

Three project-owned PNGs were generated with built-in image_gen from the existing card art;
prompts are recorded in [art-prompts.md](art-prompts.md). Saved assets:

- [Luminous backs](textures/light_backs.png)
- [Three-pose hand atlas](textures/clasp_atlas.png)
- [Memory garden](textures/memory_garden.png)

The luminous backs are re-lit from texture luminance
in the shader, so they are light forms rather than opaque recolored standing sprites.
`tools/vfx/synthesize_friendship_sounds.py` reproducibly generates original stereo audio:
two approach bells, clasp harmony, airy bloom/petals and a short passive cue.

Particle counts are bounded; all positions are analytic and independent of frame rate.
Cosmetic RNG is separate from combat RNG. Contour targets are cached after the first opening.
Invisible full-screen shader passes are disabled outside their active intervals. No screen
readback or physics simulation is used during rendering. The retained perimeter is
an independent, transparent shader pass and is never baked into the opening textures.

## 本轮附加反馈与验证范围（2026-10-18）

抽牌改为蓝绿双股光丝带出花瓣、回能采用更饱满的汇聚光流；到达实际卡牌/能量计数器时出现短弧与细小亮屑。临时力量用两股上升光丝和错层花瓣围绕玩家，消耗反馈沿卡边展开暖色光线与向外飘起的花瓣。均保持原来的 1.15 秒以内，且不阻塞卡牌效果。

柔光点从四个逐帧圆形改为一张缓存的柔光纹理；开场 shader 与声音改为 preload 依赖。C# 使用资源缓存，在加载阶段预热开场、四类附加反馈与外围场地；场地绑定具体友谊球的队列成员状态，回合开始球被移除后淡出 0.55 秒。新场地会替换旧场地，旧球结束不会清除后来者。

本轮仅静态检查源代码与资源引用，未执行构建、测试、Godot、游戏或浏览器。没有实测帧率结论。
.local/friendship_preview/friendship_preview_standalone.html 是离线 WebGL/Canvas 示意，外围光圈演示到 8 秒后退出；这不是运行时寿命。该页尚未同步本轮增强的四类附加反馈，因此附加反馈以项目脚本为准。
