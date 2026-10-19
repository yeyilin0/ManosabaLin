extends Node2D
const BASE := "res://ManosabaLin/scenes/Yalisalin/explosion_magic/"
const SHADER := preload(BASE+"ribbon.gdshader")
const NOISE := preload(BASE+"textures/ef_Bumps003.png")
const ENTRY := [Vector2(-450,-80),Vector2(660,-400),Vector2(1450,-350),Vector2(2400,130),Vector2(2350,930),Vector2(900,1420),Vector2(-300,1180),Vector2(-550,620)]
var bands: Array[MeshInstance2D] = []

func _ready() -> void:
	for i in range(ENTRY.size()):
		var band := MeshInstance2D.new()
		band.mesh=ArrayMesh.new()
		var mat := ShaderMaterial.new()
		mat.shader=SHADER
		mat.set_shader_parameter("flow_noise",NOISE)
		mat.set_shader_parameter("seed",float(i)*.237)
		mat.set_shader_parameter("tint",Color(.26,.64,1.0) if i%3!=0 else Color(.75,.3,1.0))
		band.material=mat
		add_child(band)
		bands.append(band)

func update_ribbons(clock: float, destination: Vector2) -> void:
	visible=clock>=2.0 and clock<3.98
	if not visible:
		return
	var charge := smoothstep(2.0,3.75,clock)
	var fade := 1.0-smoothstep(3.76,3.98,clock)
	for i in range(bands.size()):
		var phase := float(i)*1.73
		var start: Vector2=ENTRY[i]
		var side := 1.0 if i%2==0 else -1.0
		var control_a := start.lerp(destination,.32)+Vector2(sin(clock*.9+phase)*220.0,side*270.0)
		var control_b := destination+Vector2(190.0*cos(phase+clock*.6),side*(135.0+charge*180.0))
		var vertices := PackedVector3Array()
		var uvs := PackedVector2Array()
		var indices := PackedInt32Array()
		for j in range(65):
			var u := float(j)/64.0
			var p := start.bezier_interpolate(control_a,control_b,destination,u)
			var tangent := start.bezier_derivative(control_a,control_b,destination,u).normalized()
			var normal := Vector2(-tangent.y,tangent.x)
			var meander := sin(u*6.0-clock*1.3+phase)*35.0+sin(u*11.0-clock*1.9+phase*.7)*12.0
			p+=normal*meander*sin(u*PI)
			var neck := .72+.18*sin(u*8.0-clock*1.8+phase)+.1*sin(u*15.0-clock*2.6)
			var width := (42.0+charge*34.0)*(.78+.22*sin(u*PI))*maxf(neck,.72)
			var left := p+normal*width
			var right := p-normal*width
			vertices.append(Vector3(left.x,left.y,0.0))
			vertices.append(Vector3(right.x,right.y,0.0))
			uvs.append(Vector2(u,0.0))
			uvs.append(Vector2(u,1.0))
			if j<64:
				var k := j*2
				indices.append_array(PackedInt32Array([k,k+1,k+2,k+1,k+3,k+2]))
		# Explicit connected quads keep every curved band continuous; no polygon
		# triangulation can drop an entire ribbon when its outline folds over itself.
		var arrays := []
		arrays.resize(Mesh.ARRAY_MAX)
		arrays[Mesh.ARRAY_VERTEX]=vertices
		arrays[Mesh.ARRAY_TEX_UV]=uvs
		arrays[Mesh.ARRAY_INDEX]=indices
		var mesh := bands[i].mesh as ArrayMesh
		mesh.clear_surfaces()
		mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES,arrays)
		bands[i].material.set_shader_parameter("clock",clock)
		bands[i].material.set_shader_parameter("opacity",(.8+charge*.2)*fade*smoothstep(2.0,2.18,clock))
