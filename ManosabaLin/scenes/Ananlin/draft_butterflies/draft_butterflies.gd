extends CanvasLayer
signal finished
const BASE := "res://ManosabaLin/scenes/Ananlin/draft_butterflies/"
const SHADER := preload(BASE+"line_reveal.gdshader")
const SHAPES := [preload(BASE+"butterfly_0.svg"),preload(BASE+"butterfly_1.svg"),preload(BASE+"butterfly_2.svg"),preload(BASE+"butterfly_3.svg"),preload(BASE+"butterfly_4.svg"),preload(BASE+"butterfly_5.svg"),preload(BASE+"butterfly_6.svg"),preload(BASE+"butterfly_7.svg"),preload(BASE+"butterfly_8.svg"),preload(BASE+"butterfly_9.svg")]
@export var magic_indices := PackedInt32Array()
@export var magic_colors := PackedColorArray()
@export var complete := false
@export var sfx_gain := 1.0
var clock := 0.0
var _stage: Node2D
var _wings: Array[Sprite2D] = []
var _origins: Array[Vector2] = []
var _phase: Array[float] = []
var _strokes: Array[Line2D] = []
var _paths: Array[PackedVector2Array] = []
var _sounds: Array[AudioStreamPlayer] = []
var _pen_started := false

func _sound(file: String) -> void:
	var player := AudioStreamPlayer.new()
	player.stream=load(BASE+file)
	player.volume_db=linear_to_db(maxf(sfx_gain,.00001))
	add_child(player)
	_sounds.append(player)
	player.play()

func _ready() -> void:
	_stage=Node2D.new()
	add_child(_stage)
	# Separate presentation RNG: never touches card-generation RNG or saved mask.
	var rng := RandomNumberGenerator.new()
	rng.randomize()
	for i in range(magic_indices.size()):
		var origin := Vector2.ZERO
		for attempt in range(80):
			origin=Vector2(rng.randf_range(190,1730),rng.randf_range(200,810))
			var separated := true
			for previous in _origins:
				if origin.distance_to(previous)<240: separated=false
			if separated: break
		_origins.append(origin)
		_phase.append(rng.randf_range(0,TAU))
		var wing := Sprite2D.new()
		wing.texture=SHAPES[magic_indices[i]]
		wing.modulate=magic_colors[i]
		var mat := ShaderMaterial.new()
		mat.shader=SHADER
		wing.material=mat
		_stage.add_child(wing)
		_wings.append(wing)
		if complete:
			# One marker stroke per magic, same hue as its butterfly; ten perimeter sectors.
			var angle := TAU*float(i)/float(magic_indices.size())-.5*PI+rng.randf_range(-.09,.09)
			var start := Vector2(960,500)+Vector2(cos(angle)*1300,sin(angle)*850)
			var path := PackedVector2Array()
			for j in range(65):
				var t := float(j)/64.0
				var p := start.lerp(Vector2(960,500),t)
				p+=Vector2(-sin(angle),cos(angle))*sin(t*PI)*sin(t*7.0+float(i))*32
				path.append(p)
			_paths.append(path)
			var stroke := Line2D.new()
			stroke.width=18+rng.randf_range(-3,3)
			stroke.default_color=Color(magic_colors[i]*1.3,.8)
			stroke.begin_cap_mode=Line2D.LINE_CAP_ROUND
			stroke.end_cap_mode=Line2D.LINE_CAP_ROUND
			stroke.joint_mode=Line2D.LINE_JOINT_ROUND
			var pressure := Curve.new()
			for k in range(17): pressure.add_point(Vector2(float(k)/16.0,rng.randf_range(.65,1.0)))
			stroke.width_curve=pressure
			_stage.add_child(stroke)
			_stage.move_child(stroke,0)
			_strokes.append(stroke)
	_sound("butterfly_flight.wav")
	_render()

func _process(delta: float) -> void:
	clock+=delta
	if complete and clock>=1.9 and not _pen_started:
		_pen_started=true
		_sound("marker_on_paper.wav")
	for sound in _sounds:
		sound.volume_db=linear_to_db(maxf(sfx_gain,.00001))
	_render()
	if clock>=(4.25 if complete else 2.5):
		finished.emit()
		queue_free()

func _render() -> void:
	var size := get_viewport().get_visible_rect().size
	_stage.scale=size/Vector2(1920,1080)
	var center := Vector2(960,500)
	var convergence := smoothstep(1.9,3.2,clock) if complete else 0.0
	for i in range(_strokes.size()):
		var progress := clampf((clock-1.9-float(i)*.018)/1.1,0,1)
		var count := int(progress*64)
		var points := PackedVector2Array()
		for j in range(count+1): points.append(_paths[i][j])
		if count<64: points.append(_paths[i][count].lerp(_paths[i][count+1],progress*64-count))
		_strokes[i].points=points
		_strokes[i].modulate.a=1.0-smoothstep(3.05,3.5,clock)
	for i in range(_wings.size()):
		var wing := _wings[i]
		var origin := _origins[i]
		var phase := _phase[i]
		var drift := Vector2(sin(clock*1.7+phase)*25,cos(clock*1.3+phase)*18)
		var curve := origin.lerp(center,.45)+Vector2(cos(phase)*220,sin(phase)*160)
		wing.position=(origin+drift).bezier_interpolate(curve,center+Vector2(sin(phase)*90,cos(phase)*90),center,convergence)
		wing.rotation=sin(clock*1.2+phase)*.13*(1.0-convergence)
		var flap := .72+.28*sin(clock*6.0+phase)
		wing.scale=Vector2(flap,1.0)*.92*lerpf(1.0,.35,convergence)
		wing.material.set_shader_parameter("reveal",smoothstep(float(i)*.06,float(i)*.06+1.0,clock))
		var alpha := 1.0-smoothstep(3.05,3.3,clock) if complete else 1.0-smoothstep(2.1,2.5,clock)
		wing.modulate=Color(magic_colors[i],alpha)
	var card := get_node_or_null("CompletionCard") as Node2D
	if card!=null:
		card.position=center/Vector2(1920,1080)*size
		var reveal := smoothstep(3.05,3.5,clock)
		card.scale=Vector2.ONE*lerpf(.35,.85,reveal)*minf(size.x/1920.0,size.y/1080.0)
		card.modulate=Color(1,1,1,reveal*(1.0-smoothstep(3.95,4.25,clock)))
