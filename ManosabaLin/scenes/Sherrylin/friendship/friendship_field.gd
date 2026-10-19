extends Node2D
## A retained, orb-owned perimeter. It never controls the opening or card rules.
signal finished

const FIELD_SHADER := preload("res://ManosabaLin/scenes/Sherrylin/friendship/friendship_field.gdshader")
const RETIRE_SECONDS := .55
@export var autoplay := true
var clock := 0.0
var retiring := false
var retire_age := 0.0
var retire_reveal := 1.0
var idle_prepared := false
var ended := false
var field: ColorRect
var last_viewport := Vector2.ZERO

func prepare_idle() -> void:
	idle_prepared = true
	autoplay = false
	set_process(false)
	if is_instance_valid(field): field.visible = false

func _ready() -> void:
	field = ColorRect.new()
	field.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var shader_material := ShaderMaterial.new()
	shader_material.shader = FIELD_SHADER
	field.material = shader_material
	add_child(field)
	if idle_prepared:
		field.visible = false
	else:
		render_at(clock)

func activate_field() -> void:
	if retiring or ended: return
	idle_prepared = false
	autoplay = true
	clock = 0.0
	set_process(true)
	if is_node_ready(): render_at(clock)

func retire_field() -> void:
	if retiring or ended: return
	retiring = true
	retire_age = 0.0
	retire_reveal = smoothstep(0.0,.62,clock)
	autoplay = true
	set_process(true)

func _process(delta: float) -> void:
	if not autoplay or ended: return
	clock += delta
	if retiring:
		retire_age += delta
		if retire_age >= RETIRE_SECONDS:
			ended = true
			field.visible = false
			finished.emit()
			queue_free()
			return
	render_at(clock)

func render_at(seconds: float) -> void:
	if not is_instance_valid(field): return
	var viewport := get_viewport_rect().size
	if viewport != last_viewport:
		last_viewport = viewport
		field.size = viewport
		field.material.set_shader_parameter("viewport_size",viewport)
	var fade := 1.0-smoothstep(0.0,RETIRE_SECONDS,retire_age) if retiring else 1.0
	# Replacing a field during its entrance must only fade it, never let the
	# unfinished entrance brighten it again while retirement is in progress.
	if retiring: fade *= retire_reveal/maxf(smoothstep(0.0,.62,seconds),.00001)
	field.visible = fade > .001
	if field.visible:
		field.material.set_shader_parameter("clock",seconds)
		field.material.set_shader_parameter("gain",fade)

func prewarm_at(seconds: float) -> void:
	# The C# pool prewarms this hidden instance before a card is played. Do not
	# acquire a field slot or run the retirement clock from this readied path.
	autoplay = false
	set_process(false)
	render_at(seconds)

func seek_preview(seconds: float) -> void:
	autoplay = false
	set_process(false)
	clock = seconds
	render_at(seconds)
