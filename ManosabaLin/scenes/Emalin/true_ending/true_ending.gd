extends Node2D
## Five-second reunion, followed by a power-owned, silent peripheral aura.
signal opening_finished
signal finished
const BASE := "res://ManosabaLin/scenes/Emalin/true_ending/"
const FLOW := preload(BASE + "true_ending_flow.gd")
const FLOW_MATERIAL := preload(BASE + "additive.tres")
const REUNION := preload(BASE + "textures/reunion.png")
const FIELDS := [preload(BASE + "confluence.gdshader"), preload(BASE + "reunion.gdshader"), preload(BASE + "bloom.gdshader"), preload(BASE + "border.gdshader")]
const OPENING_SOUND := preload(BASE + "audio/opening.wav")
const MEMORY_SOUND := preload(BASE + "audio/memory.wav")
const COMPLETE_SOUND := preload(BASE + "audio/complete.wav")
@export var sfx_gain := 1.0
@export var autoplay := true
@export var audio_enabled := true
@export var aura_enabled := true
var aura_gain := 1.0
var player_uv := Vector2(.22, .55)
var clock := 0.0
var progress := 0
var opening := true
var opening_reported := false
var ending := false
var end_age := 0.0
var border_pulse := 0.0
var border_pulse_age := 100.0
var fields: Array[ColorRect] = []
var flow: Node2D
var sound: AudioStreamPlayer
var short_sounds: Array[AudioStreamPlayer] = []
var sound_started := false
var idle_prepared := false
var field_viewport := Vector2.ZERO

func prepare_idle() -> void:
	idle_prepared = true
	autoplay = false
	if is_node_ready(): hide_idle()

func setup(player: Vector2, replay_opening: bool, collected: int) -> void:
	idle_prepared = false
	autoplay = true
	# Only a successful room-field claim may enable the retained border.
	aura_enabled = false
	aura_gain = 0.0
	player_uv = player
	opening = replay_opening
	progress = clampi(collected, 0, 13)
	clock = 0.0 if opening else 5.2
	opening_reported = not opening
	sound_started = false
	ending = false
	end_age = 0.0
	border_pulse_age = 100.0
	if is_instance_valid(sound): sound.stop()
	for voice in short_sounds:
		if is_instance_valid(voice): voice.stop()
	if is_instance_valid(flow):
		flow.visible = true
		flow.call("reset_memory")

func _ready() -> void:
	make_field(FIELDS[0], 0.0, 2.35)
	var figures := make_field(FIELDS[1], 1.25, 4.65)
	figures.material.set_shader_parameter("reunion_tex", REUNION)
	figures.material.set_shader_parameter("reunion_size", Vector2(REUNION.get_size()))
	make_field(FIELDS[2], 2.85, 5.3)
	make_field(FIELDS[3], 4.05, INF)
	flow = Node2D.new()
	flow.set_script(FLOW)
	# Keep the same material alive in the cached scene dependency graph after prewarm.
	flow.material = FLOW_MATERIAL
	add_child(flow)
	sound = AudioStreamPlayer.new()
	sound.stream = OPENING_SOUND
	add_child(sound)
	# Bounded voice pool: overlapping marks never create an unbounded audio tree.
	for i in range(3):
		var voice := AudioStreamPlayer.new()
		add_child(voice)
		short_sounds.append(voice)
	if idle_prepared: hide_idle()
	else: render_at(clock)

func hide_idle() -> void:
	# Loading-time offscreen prewarm covers every pass; room instances only wait here.
	flow.visible = false
	for field in fields: field.visible = false

func prewarm_at(seconds: float) -> void:
	# Called only by the loading-screen SubViewport. Use real draw paths and opacity,
	# including atlas sprites, antialiased tears, ribbons and all three memory responses.
	autoplay = false
	audio_enabled = false
	clock = seconds
	flow.visible = true
	flow.call("reset_memory")
	if seconds >= 5.5:
		flow.call("add_memory", 1, Vector2(.38,.72), 1, seconds-.45)
		flow.call("add_memory", 2, Vector2(.55,.72), 7, seconds-1.32)
		flow.call("add_memory", 3, Vector2(.50,.52), 13, seconds-1.05)
	render_at(seconds)

func make_field(shader: Shader, begin: float, end: float) -> ColorRect:
	var field := ColorRect.new()
	field.mouse_filter = Control.MOUSE_FILTER_IGNORE
	field.set_meta("begin", begin)
	field.set_meta("end", end)
	var mat := ShaderMaterial.new()
	mat.shader = shader
	field.material = mat
	add_child(field)
	fields.append(field)
	return field

func _process(delta: float) -> void:
	if not autoplay:
		return
	clock += delta
	aura_gain = move_toward(aura_gain,1.0 if aura_enabled else 0.0,delta/.28)
	border_pulse_age += delta
	if ending:
		end_age += delta
		if end_age >= .6:
			finished.emit()
			queue_free()
			return
	if opening and audio_enabled and not sound_started and not ending:
		sound_started = true
		sound.play()
	var gain := 1.0 - smoothstep(0.0, .6, end_age) if ending else 1.0
	sound.volume_db = linear_to_db(maxf(sfx_gain * .67 * gain, .00001))
	for voice in short_sounds:
		voice.volume_db = linear_to_db(maxf(sfx_gain * .35 * gain, .00001))
	if not opening_reported and clock >= 5.0:
		opening_reported = true
		opening_finished.emit()
	render_at(clock)

func render_at(seconds: float) -> void:
	var viewport := get_viewport_rect().size
	var gain := 1.0 - smoothstep(0.0, .6, end_age) if ending else 1.0
	if viewport != field_viewport:
		field_viewport = viewport
		for field in fields:
			field.size = viewport
			field.material.set_shader_parameter("viewport_size", viewport)
	for field in fields:
		field.visible = seconds >= float(field.get_meta("begin")) and seconds < float(field.get_meta("end"))
		if field.visible:
			field.material.set_shader_parameter("clock", seconds)
			field.material.set_shader_parameter("gain", gain)
	# The last pass stays active only around the viewport's perimeter.
	fields[-1].visible = fields[-1].visible and aura_gain > .001
	fields[-1].material.set_shader_parameter("gain",gain*aura_gain)
	fields[-1].material.set_shader_parameter("pulse", border_pulse * exp(-border_pulse_age * 1.55))
	flow.set("perimeter_gain",aura_gain)
	flow.modulate.a = gain
	flow.call("render_at", seconds, viewport, player_uv, progress)

func set_field_active(active: bool) -> void:
	aura_enabled = active
	# A new/idle scene starts with no rejected border flash; existing auras fade out.
	if active: aura_gain = 1.0
	elif not is_node_ready() or clock < 4.05: aura_gain = 0.0

func notify_memory(kind: int, origin_uv: Vector2, collected: int) -> void:
	if ending or not is_instance_valid(flow):
		return
	progress = clampi(collected, 0, 13)
	border_pulse = .85 if kind < 3 else 1.35
	border_pulse_age = 0.0
	flow.call("add_memory", kind, origin_uv, progress, clock)
	if audio_enabled:
		for voice in short_sounds:
			if not voice.playing:
				voice.stream = COMPLETE_SOUND if kind == 3 else MEMORY_SOUND
				voice.volume_db = linear_to_db(maxf(sfx_gain * .35, .00001))
				voice.play()
				break

func finish_effect() -> void:
	if ending:
		return
	ending = true
	end_age = 0.0
	if not opening_reported:
		opening_reported = true
		opening_finished.emit()

func seek_preview(seconds: float) -> void:
	autoplay = false
	audio_enabled = false
	clock = seconds
	if is_instance_valid(sound): sound.stop()
	for voice in short_sounds:
		if is_instance_valid(voice): voice.stop()
	if is_instance_valid(flow): render_at(seconds)
