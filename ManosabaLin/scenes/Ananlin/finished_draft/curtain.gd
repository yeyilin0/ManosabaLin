extends Node2D
@onready var cloth: TextureRect=$Cloth
func update_cloth(time: float, opacity: float) -> void:
	if not is_instance_valid(cloth): return
	# Full-viewport cloth in the creature's ordinary canvas, compensating camera offset/zoom.
	global_transform=get_canvas_transform().affine_inverse()
	var size := get_viewport_rect().size
	if cloth.size!=size: cloth.size=size
	cloth.material.set_shader_parameter("clock",time)
	cloth.material.set_shader_parameter("opacity",opacity)
