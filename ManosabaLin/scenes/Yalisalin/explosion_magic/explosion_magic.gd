extends CanvasLayer
## 2s unfold + 2s fluid charge/launch + 1s explosion + 2s aftershock.
signal impact
signal finished
const DURATION := 7.0
const IMPACT_TIME := 4.08
const UNFOLD_TIME := 2.0
const BASE := "res://ManosabaLin/scenes/Yalisalin/explosion_magic/"
const SOUND := "res://ManosabaLin/audio/explosion_magic/"
const RADII := [335.0,290.0,175.0,115.0,155.0,96.0,180.0,250.0,330.0,385.0]
const SIGILS := [preload(BASE+"sigil_0.svg"),preload(BASE+"sigil_1.svg"),preload(BASE+"sigil_2.svg")]
const SIGIL_SHADER := preload(BASE+"sigil.gdshader")
const LAYER_SHADER := preload(BASE+"texture_layer.gdshader")
const SMOKE_SHADER := preload(BASE+"smoke_layer.gdshader")
const DISTORT_SHADER := preload(BASE+"air_distortion.gdshader")
const PAINT_SCRIPT := preload(BASE+"explosion_geometry.gd")
const RIBBON_SCRIPT := preload(BASE+"charge_ribbons.gd")
const MASS_SHADER := preload(BASE+"magic_mass.gdshader")
const JET_SHADER := preload(BASE+"flame_jet.gdshader")
const NOISE := preload(BASE+"textures/ef_Bumps003.png")
const FIRE := preload(BASE+"textures/ef_fire004_tra.png")
const FOG := preload(BASE+"textures/ef_fog002.png")
const SMOKE := preload(BASE+"textures/ef_fog003.png")
const LIGHT := preload(BASE+"textures/ef_light003.png")
const RING := preload(BASE+"textures/ef_ring010.png")
const SOUNDS := [preload(SOUND+"unfold.ogg"),preload(SOUND+"charge_synth.ogg"),preload(SOUND+"fireball_launch_synth.ogg"),preload(SOUND+"detonation.ogg"),preload(SOUND+"aftershock.ogg")]
const CUES := [0.0,2.0,3.75,IMPACT_TIME,5.0]

@export var source_uv := Vector2(.21,.54)
@export var target_uv := Vector2(.77,.54)
@export var autoplay := true
@export var audio_enabled := true
@export var sfx_gain := 1.0
var clock := 0.0
var _impact_sent := false
var _finished_sent := false
var _cue_index := 0
var _stage: Node2D
var _dim: ColorRect
var _warm: ColorRect
var _distort: ColorRect
var _flash: ColorRect
var _copy: BackBufferCopy
var _paint: Node2D
var _ribbons: Node2D
var _core: Array[Sprite2D] = []
var _rings: Array[Sprite2D] = []
var _smoke: Array[Sprite2D] = []
var _fire: Array[Sprite2D] = []
var _shock: Array[Sprite2D] = []
var _players: Array[AudioStreamPlayer] = []
var _jet: Sprite2D
var _basin: Sprite2D

func _ready() -> void:
	_dim=_rect()
	_warm=_rect()
	_stage=Node2D.new()
	add_child(_stage)
	for i in range(RADII.size()):
		var ring := Sprite2D.new()
		ring.texture=SIGILS[i%3]
		ring.material=_material(SIGIL_SHADER)
		_stage.add_child(ring)
		_rings.append(ring)
	_ribbons=Node2D.new()
	_ribbons.set_script(RIBBON_SCRIPT)
	_stage.add_child(_ribbons)
	_basin=_sprite(FOG,LAYER_SHADER,0)
	for i in range(14):
		_smoke.append(_sprite(SMOKE,SMOKE_SHADER,i))
	for i in range(24):
		_fire.append(_sprite(FIRE if i%3!=0 else FOG,LAYER_SHADER,i))
	for i in range(3):
		_shock.append(_sprite(RING,LAYER_SHADER,i))
	_jet=_sprite(FOG,JET_SHADER,0)
	for i in range(3):
		_core.append(_sprite(FOG,MASS_SHADER,i))
	_paint=Node2D.new()
	_paint.set_script(PAINT_SCRIPT)
	var additive := CanvasItemMaterial.new()
	additive.blend_mode=CanvasItemMaterial.BLEND_MODE_ADD
	_paint.material=additive
	_stage.add_child(_paint)
	_copy=BackBufferCopy.new()
	_copy.copy_mode=BackBufferCopy.COPY_MODE_VIEWPORT
	add_child(_copy)
	_distort=_rect()
	_distort.material=_material(DISTORT_SHADER)
	_flash=_rect()
	for stream in SOUNDS:
		var player := AudioStreamPlayer.new()
		player.stream=stream
		player.bus=&"Master"
		add_child(player)
		_players.append(player)
	_render()

func _rect() -> ColorRect:
	var rect := ColorRect.new()
	rect.mouse_filter=Control.MOUSE_FILTER_IGNORE
	add_child(rect)
	return rect

func _material(shader: Shader) -> ShaderMaterial:
	var mat := ShaderMaterial.new()
	mat.shader=shader
	return mat

func _sprite(tex: Texture2D, shader: Shader, seed: int) -> Sprite2D:
	var sprite := Sprite2D.new()
	sprite.texture=tex
	sprite.material=_material(shader)
	sprite.material.set_shader_parameter("flow_noise",NOISE)
	sprite.material.set_shader_parameter("seed",float(seed)*.173)
	_stage.add_child(sprite)
	return sprite

func _process(delta: float) -> void:
	if not autoplay or _finished_sent:
		return
	clock=minf(clock+delta,DURATION)
	while _cue_index<CUES.size() and clock>=CUES[_cue_index]:
		if audio_enabled:
			_players[_cue_index].volume_linear=sfx_gain
			_players[_cue_index].play()
		_cue_index+=1
	for player in _players:
		player.volume_linear=sfx_gain
	_render()
	if not _impact_sent and clock>=IMPACT_TIME:
		_impact_sent=true
		impact.emit()
	if clock>=DURATION:
		_finished_sent=true
		finished.emit()
		queue_free()

func seek_preview(seconds: float) -> void:
	autoplay=false
	clock=clampf(seconds,0.0,DURATION)
	_render()

func _layer(sprite: Sprite2D, opacity: float, dissolve: float=0.0) -> void:
	sprite.visible=opacity>.001
	sprite.material.set_shader_parameter("clock",clock)
	sprite.material.set_shader_parameter("opacity",opacity)
	sprite.material.set_shader_parameter("dissolve",dissolve)

func _render() -> void:
	var size := get_viewport().get_visible_rect().size
	var ref := Vector2(1920,1080)
	_stage.scale=size/ref
	for rect in [_dim,_warm,_distort,_flash]:
		rect.size=size
	# Half of player's usual region remains to the left; array begins at screen-left.
	var source := Vector2(clampf(source_uv.x*960.0,130.0,240.0),clampf(source_uv.y*1080.0,390.0,650.0))
	var target := Vector2(maxf(target_uv.x*1920.0,1050.0),source.y)
	var charge := smoothstep(2.0,3.75,clock)
	var launch := smoothstep(3.8,4.0,clock)
	var darkness := lerpf(.15,.96,smoothstep(1.8,2.35,clock))
	darkness*=smoothstep(0.0,.3,clock)*(1.0-smoothstep(4.08,4.55,clock))
	_dim.color=Color(.005,.004,.018,darkness)
	var burst_age := maxf(0.0,clock-IMPACT_TIME)
	_warm.color=Color(1.0,.26,.015,smoothstep(4.08,4.2,clock)*(1.0-smoothstep(5.0,6.85,clock))*.22)
	var ring_fade := 1.0-smoothstep(4.13,4.48,clock)
	var ring_flare := 1.0+3.5*smoothstep(3.8,4.0,clock)
	for i in range(_rings.size()):
		var age := clock-float(i)*.18
		var opening := smoothstep(0.0,.36,age)
		var ring := _rings[i]
		ring.visible=age>0.0 and ring_fade>0.0
		ring.position=Vector2(lerpf(source.x+70.0,target.x-75.0,float(i)/9.0),source.y)
		ring.scale=Vector2(.33,1.0)*RADII[i]*2.0/1024.0*lerpf(.58,1.0,opening)
		ring.material.set_shader_parameter("reveal",opening)
		ring.material.set_shader_parameter("spin",clock*(.06 if i%2==0 else -.045)+float(i)*.31)
		ring.material.set_shader_parameter("strength",ring_flare*ring_fade)
	_ribbons.call("update_ribbons",clock,source)
	# Wide translucent convergence basin extends beyond half of the player region.
	_basin.position=source+Vector2(55.0,0.0)
	_basin.scale=Vector2(700.0,520.0)/float(FOG.get_width())
	_basin.rotation=sin(clock*.5)*.12
	_basin.material.set_shader_parameter("fire",false)
	_basin.material.set_shader_parameter("tint",Color(.28,.65,1.1))
	_layer(_basin,smoothstep(2.0,2.55,clock)*(1.0-smoothstep(3.8,4.08,clock))*.5)
	# Match the visible horizontal width of the smallest projected sigil.
	var aperture_width := float(RADII.min())*2.0*.33
	var core_diameter := lerpf(12.0,aperture_width,charge)
	var eruption := smoothstep(3.75,4.0,clock)
	# The largest disk's full aperture is the discharge diameter; grow during flight.
	var discharge_diameter := float(RADII.max())*2.0
	var core_width := lerpf(core_diameter,discharge_diameter,launch)
	var core_height := lerpf(core_diameter*1.15,discharge_diameter*.85,launch)
	var core_position := source.lerp(target,launch)
	var core_visible := smoothstep(2.0,2.25,clock)*(1.0-smoothstep(4.0,4.08,clock))
	for i in range(_core.size()):
		var core := _core[i]
		core.position=core_position
		var extra := [1.25,1.0,.82]
		core.scale=Vector2(core_width*(1.0+.08*sin(clock*3.1+i)),core_height*(1.0+.1*cos(clock*2.7+i)))*float(extra[i])/float(core.texture.get_width())
		core.rotation=sin(clock*1.8+i)*.14*(1.0-eruption)
		core.material.set_shader_parameter("launch",eruption)
		core.material.set_shader_parameter("clock",clock)
		core.material.set_shader_parameter("opacity",core_visible*(.5 if i==0 else 1.0))
		core.material.set_shader_parameter("tint",[Color(.6,.55,1.2),Color(.8,1.0,1.1),Color(1.2,1.2,1.2)][i])
		core.visible=core_visible>.001
	# Advected, ragged flame stream follows the uncompressed advancing mass.
	var jet_length := maxf(core_width,core_position.x-source.x+core_width*.55)
	_jet.position=Vector2(core_position.x-jet_length*.5,source.y)
	_jet.scale=Vector2(jet_length,core_height*1.25)/float(FOG.get_width())
	_jet.material.set_shader_parameter("clock",clock)
	_jet.material.set_shader_parameter("opacity",eruption*core_visible)
	_jet.visible=eruption*core_visible>.001
	var growth := 1.0-exp(-burst_age*5.5)
	var after_fade := 1.0-smoothstep(5.2,7.0,clock)
	# Dark rolling smoke first, followed by overlapping hot fire lobes at varied depths.
	for i in range(_smoke.size()):
		var angle := float(i)*2.39996
		var radius := growth*(170.0+float(i%5)*90.0)
		var sprite := _smoke[i]
		sprite.position=target+Vector2(cos(angle)*radius,sin(angle)*radius*.82-burst_age*65.0)
		sprite.rotation=angle+burst_age*.12
		sprite.scale=Vector2.ONE*(.08+growth*2.4+burst_age*.22)
		_layer(sprite,smoothstep(4.08,4.22,clock)*after_fade*.8,smoothstep(5.4,7.0,clock)*.65)
	for i in range(_fire.size()):
		var angle := float(i)*2.39996
		var radius := growth*(110.0+float(i%7)*68.0)
		var sprite := _fire[i]
		sprite.position=target+Vector2(cos(angle)*radius,sin(angle)*radius*.82-burst_age*38.0)
		sprite.rotation=angle+burst_age*(.11 if i%2==0 else -.16)
		var diameter := (160.0+float(i%5)*70.0)*(.12+growth)*lerpf(1.0,1.5,smoothstep(0.0,2.4,burst_age))
		sprite.scale=Vector2.ONE*diameter/float(sprite.texture.get_width())
		sprite.material.set_shader_parameter("tint",Color(1.0,.68+float(i%3)*.13,.5))
		_layer(sprite,smoothstep(4.08,4.2,clock)*after_fade*(.9 if i%3==0 else 1.6),smoothstep(4.8,6.9,clock)*.78)
	for i in range(_shock.size()):
		var age := burst_age-.18-float(i)*.17
		var ring := _shock[i]
		ring.position=target
		ring.scale=Vector2(1.0,.55+float(i)*.13)*maxf(0.0,100.0+age*1800.0)/float(RING.get_width())
		ring.rotation=float(i)*.17
		ring.material.set_shader_parameter("fire",false)
		ring.material.set_shader_parameter("tint",Color(1.8,1.55,1.2))
		_layer(ring,maxf(0.0,1.0-age/1.8) if age>=0.0 else 0.0)
	_paint.set("clock",clock)
	_paint.set("source",source)
	_paint.set("target",target)
	_paint.set("core_position",core_position)
	_paint.set("core_radius",core_width*.5)
	_paint.queue_redraw()
	_distort.visible=clock>=IMPACT_TIME and clock<6.95
	_copy.visible=_distort.visible
	_distort.material.set_shader_parameter("clock",clock)
	_distort.material.set_shader_parameter("center",target/ref)
	_distort.material.set_shader_parameter("aspect",size.x/size.y)
	if clock>=4.0 and clock<4.04:
		_flash.color=Color(0,0,0,1)
	elif clock>=4.04 and clock<4.08:
		_flash.color=Color(1,1,1,1)
	else:
		_flash.color=Color(1,.96,.83,exp(-burst_age*14.0)*.94 if clock>=IMPACT_TIME else 0.0)

func _exit_tree() -> void:
	for player in _players:
		if is_instance_valid(player):
			player.stop()
