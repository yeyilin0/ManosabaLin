extends Node2D
## Six-second courtroom memory, followed by power-owned peripheral light.
signal impact_ready
signal opening_finished
signal finished
const BASE := "res://ManosabaLin/scenes/Emalin/bad_ending/"
const FLOW := preload(BASE + "bad_ending_flow.gd")
const ADDITIVE := preload(BASE + "additive.tres")
const COURT := preload(BASE + "textures/courtroom.png")
const COURT_STAGE := preload(BASE + "textures/court_stage.png")
const FIGURES := [
	preload(BASE + "textures/figure_01.png"),
	preload(BASE + "textures/figure_02.png"),
	preload(BASE + "textures/figure_03.png"),
	preload(BASE + "textures/figure_04.png"),
	preload(BASE + "textures/figure_05.png"),
	preload(BASE + "textures/figure_06.png"),
]
const FIELDS := [preload(BASE + "courtroom.gdshader"), preload(BASE + "figures.gdshader"), preload(BASE + "waves.gdshader"), preload(BASE + "border.gdshader")]
const OPENING_SOUND := preload(BASE + "audio/opening.wav")
const SUPPORT_SOUND := preload(BASE + "audio/support.wav")
const CARD_SOUND := preload(BASE + "audio/card.wav")
@export var sfx_gain := 1.0
@export var audio_enabled := true
@export var autoplay := true
@export var aura_enabled := true
var aura_gain := 1.0
var player_uv := Vector2(.22,.55)
var clock := 0.0
var opening := true
var impact_reported := false
var opening_reported := false
var ending := false
var end_age := 0.0
var pulse_age := 100.0
var idle_prepared := false
var sound_started := false
var fields: Array[ColorRect] = []
var background_flow: Node2D
var flow: Node2D
var sound: AudioStreamPlayer
var voices: Array[AudioStreamPlayer] = []
var last_viewport := Vector2.ZERO

func prepare_idle() -> void:
	idle_prepared = true
	autoplay = false
	set_process(false)
	if is_node_ready(): hide_idle()

func setup(player: Vector2, replay_opening: bool) -> void:
	player_uv = player
	opening = replay_opening
	# Restore/feedback can be rejected by a newer room field; start hidden.
	aura_enabled = false
	aura_gain = 0.0
	clock = 0.0 if opening else 6.1
	impact_reported = not opening
	opening_reported = not opening
	ending = false
	end_age = 0.0
	pulse_age = 100.0
	idle_prepared = false
	autoplay = true
	set_process(true)
	sound_started = false
	if is_instance_valid(sound): sound.stop()
	for voice in voices: voice.stop()
	if is_instance_valid(flow):
		flow.call("reset_support")
		flow.visible = true
		background_flow.visible = true

func _ready() -> void:
	var court := make_field(FIELDS[0],0.0,5.95)
	court.material.set_shader_parameter("court_tex",COURT)
	court.material.set_shader_parameter("stage_tex",COURT_STAGE)
	court.material.set_shader_parameter("stage_size",Vector2(COURT_STAGE.get_size()))
	background_flow = make_flow(true)
	var figures := make_field(FIELDS[1],.88,5.85)
	for i in range(FIGURES.size()):
		figures.material.set_shader_parameter("figure_%d" % i,FIGURES[i])
	figures.material.set_shader_parameter("frame_size",Vector2(FIGURES[0].get_size()))
	make_field(FIELDS[2],2.25,5.8)
	flow = make_flow(false)
	make_field(FIELDS[3],4.1,INF)
	sound = AudioStreamPlayer.new()
	sound.stream = OPENING_SOUND
	add_child(sound)
	for i in range(3):
		var voice := AudioStreamPlayer.new()
		add_child(voice)
		voices.append(voice)
	if idle_prepared: hide_idle()
	else: render_at(clock)

func make_flow(behind: bool) -> Node2D:
	var node := Node2D.new()
	node.set_script(FLOW)
	node.set("behind_figures",behind)
	node.material = ADDITIVE
	add_child(node)
	return node

func make_field(shader: Shader, begin: float, end: float) -> ColorRect:
	var field := ColorRect.new()
	field.mouse_filter = Control.MOUSE_FILTER_IGNORE
	field.set_meta("begin",begin)
	field.set_meta("end",end)
	var mat := ShaderMaterial.new()
	mat.shader = shader
	field.material = mat
	add_child(field)
	fields.append(field)
	return field

func hide_idle() -> void:
	for field in fields: field.visible = false
	flow.visible = false
	background_flow.visible = false

func _process(delta: float) -> void:
	if not autoplay: return
	clock += delta
	aura_gain = move_toward(aura_gain,1.0 if aura_enabled else 0.0,delta/.28)
	pulse_age += delta
	if ending:
		end_age += delta
		if end_age >= .65:
			finished.emit()
			queue_free()
			return
	var fade := 1.0-smoothstep(0.0,.65,end_age) if ending else 1.0
	sound.volume_db = linear_to_db(maxf(.72*sfx_gain*fade,.00001))
	for voice in voices: voice.volume_db = linear_to_db(maxf(.75*sfx_gain*fade,.00001))
	if opening and audio_enabled and not sound_started and not ending:
		sound_started = true
		sound.play()
	if not impact_reported and clock >= 3.05:
		impact_reported = true
		impact_ready.emit()
	if not opening_reported and clock >= 6.0:
		opening_reported = true
		opening_finished.emit()
	render_at(clock)

func render_at(seconds: float) -> void:
	var viewport := get_viewport_rect().size
	var fade := 1.0-smoothstep(0.0,.65,end_age) if ending else 1.0
	if viewport != last_viewport:
		last_viewport = viewport
		for field in fields:
			field.size = viewport
			field.material.set_shader_parameter("viewport_size",viewport)
	for field in fields:
		field.visible = seconds >= float(field.get_meta("begin")) and seconds < float(field.get_meta("end"))
		if field.visible:
			field.material.set_shader_parameter("clock",seconds)
			field.material.set_shader_parameter("gain",fade)
	fields[-1].material.set_shader_parameter("pulse",exp(-pulse_age*2.2))
	fields[-1].visible = fields[-1].visible and aura_gain > .001
	fields[-1].material.set_shader_parameter("gain",fade*aura_gain)
	flow.set("show_perimeter",aura_gain > .001)
	flow.set("perimeter_gain",aura_gain)
	flow.modulate.a = fade
	background_flow.modulate.a = fade
	flow.call("render_at",seconds,viewport,player_uv)
	background_flow.visible = seconds < 5.95
	if background_flow.visible: background_flow.call("render_at",seconds,viewport,player_uv)

func set_field_active(active: bool) -> void:
	aura_enabled = active
	if active: aura_gain = 1.0
	elif not is_node_ready() or clock < 4.10: aura_gain = 0.0

func notify_support(kind: int, origin_uv: Vector2, count: int) -> void:
	if ending or not is_instance_valid(flow): return
	pulse_age = 0.0
	flow.call("add_support",kind,origin_uv,count,clock)
	play_short(CARD_SOUND if kind >= 3 else SUPPORT_SOUND)

func notify_card(kind: int, origin_uv: Vector2, art: Texture2D) -> void:
	if ending or not is_instance_valid(flow): return
	pulse_age = 0.0
	flow.call("add_card",kind,origin_uv,art,clock)
	play_short(CARD_SOUND)

func play_short(stream: AudioStream) -> void:
	if not audio_enabled: return
	for voice in voices:
		if not voice.playing:
			voice.stream = stream
			voice.volume_db = linear_to_db(maxf(.75*sfx_gain,.00001))
			voice.play()
			break

func finish_effect() -> void:
	if ending: return
	ending = true
	end_age = 0.0
	if not impact_reported:
		impact_reported = true
		impact_ready.emit()
	if not opening_reported:
		opening_reported = true
		opening_finished.emit()

func prewarm_at(seconds: float) -> void:
	autoplay = false
	audio_enabled = false
	clock = seconds
	flow.visible = true
	background_flow.visible = true
	flow.call("reset_support")
	if seconds >= 6.3:
		for kind in range(1,5):
			flow.call("add_support",kind,Vector2(.22+float(kind)*.13,.58),kind,seconds-.32)
		# The same texture/color draw path used by cached real-card portraits.
		flow.call("add_card",3,Vector2(.58,.58),FIGURES[0],seconds-.32)
	render_at(seconds)

func seek_preview(seconds: float) -> void:
	autoplay = false
	audio_enabled = false
	clock = seconds
	if is_instance_valid(sound): sound.stop()
	for voice in voices: voice.stop()
	if is_instance_valid(flow): render_at(seconds)
