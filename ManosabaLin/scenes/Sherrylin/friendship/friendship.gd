extends Node2D
## 0: opening; 1: drawn card; 2: energy; 3: strength; 4: exhausted card.
signal finished
const BASE := "res://ManosabaLin/scenes/Sherrylin/friendship/"
const FLOW := preload(BASE+"friendship_flow.gd")
const GARDEN := preload(BASE+"textures/memory_garden.png")
const BACKS := preload(BASE+"textures/light_backs.png")
const HANDS := preload(BASE+"textures/clasp_atlas.png")
const PORTAL_SHADER := preload(BASE+"memory_portal.gdshader")
const BACKS_SHADER := preload(BASE+"light_backs.gdshader")
const HANDS_SHADER := preload(BASE+"clasp.gdshader")
const HALO_SHADER := preload(BASE+"halo.gdshader")
const RELEASE_SHADER := preload(BASE+"prismatic_release.gdshader")
const OPEN_SOUND := preload(BASE+"audio/open_chime.wav")
const PULSE_SOUND := preload(BASE+"audio/pulse.wav")
@export var sfx_gain := 1.0
@export var autoplay := true
@export var audio_enabled := true
var player_uv := Vector2(.22,.55)
var target_uv := Vector2(.5,.47)
var mode := 0
var clock := 0.0
var fields: Array[ColorRect] = []
var flow: Node2D
var audio: AudioStreamPlayer
var sound_started := false
var ended := false

func setup(player: Vector2, target: Vector2, effect: int) -> void:
	player_uv=player
	target_uv=target
	mode=effect

func _ready() -> void:
	if mode==0:
		var portal := make_field(PORTAL_SHADER,.08,3.52)
		portal.material.set_shader_parameter("garden",GARDEN)
		var backs := make_field(BACKS_SHADER,1.43,3.92)
		backs.material.set_shader_parameter("backs",BACKS)
		var hands := make_field(HANDS_SHADER,.48,2.76)
		hands.material.set_shader_parameter("hands",HANDS)
		make_field(HALO_SHADER,1.43,4.10)
		make_field(RELEASE_SHADER,2.66,4.20)
	flow=Node2D.new()
	flow.set_script(FLOW)
	flow.set("opening",mode==0)
	var additive := CanvasItemMaterial.new()
	additive.blend_mode=CanvasItemMaterial.BLEND_MODE_ADD
	flow.material=additive
	add_child(flow)
	audio=AudioStreamPlayer.new()
	audio.stream=OPEN_SOUND if mode==0 else PULSE_SOUND
	audio.bus=&"Master"
	add_child(audio)
	render_at(0.0)

func make_field(shader: Shader, begin: float, end: float) -> ColorRect:
	var field := ColorRect.new()
	field.size=Vector2(1920,1080)
	field.mouse_filter=Control.MOUSE_FILTER_IGNORE
	field.set_meta("active_begin",begin)
	field.set_meta("active_end",end)
	var shader_material := ShaderMaterial.new()
	shader_material.shader=shader
	field.material=shader_material
	add_child(field)
	fields.append(field)
	return field

func _process(delta: float) -> void:
	if not autoplay or ended:
		return
	clock+=delta
	if audio_enabled and not sound_started:
		sound_started=true
		audio.volume_db=linear_to_db(maxf(sfx_gain*(.66 if mode==0 else .22),.00001))
		audio.play()
	var tail := 1.0-smoothstep(3.8,4.2,clock) if mode==0 else 1.0-smoothstep(.85,1.15,clock)
	audio.volume_db=linear_to_db(maxf(sfx_gain*(.66 if mode==0 else .22)*tail,.00001))
	render_at(clock)
	if clock>=(4.2 if mode==0 else 1.15):
		ended=true
		finished.emit()
		queue_free()

func render_at(seconds: float) -> void:
	scale=get_viewport_rect().size/Vector2(1920,1080)
	for field in fields:
		field.visible=seconds>float(field.get_meta("active_begin")) and seconds<float(field.get_meta("active_end"))
		if field.visible: field.material.set_shader_parameter("clock",seconds)
	flow.call("render_at",seconds,mode,player_uv*Vector2(1920,1080),target_uv*Vector2(1920,1080))

func seek_preview(seconds: float) -> void:
	autoplay=false
	audio_enabled=false
	clock=seconds
	if is_instance_valid(audio): audio.stop()
	if is_instance_valid(flow): render_at(seconds)
