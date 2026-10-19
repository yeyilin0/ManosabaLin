extends Node2D
signal opened
signal closed
signal impact
@export var player_anchor := Vector2(.22,.55)
@export var target_anchor := Vector2(.75,.5)
@export var sfx_gain := 1.0
@export var autoplay := true
@export var audio_enabled := true
const SIZE := Vector2(1920,1080)
const BASE := "res://ManosabaLin/scenes/Ananlin/finished_draft/"
const DARK := preload(BASE+"dark_field.gdshader")
const SPOT := preload(BASE+"spotlight.gdshader")
const WING_SHADER := preload(BASE+"butterfly_glow.gdshader")
const MOTE := preload(BASE+"glow_mote.svg")
const OPEN_SOUND := preload(BASE+"curtain_pull.wav")
const WING_SOUND := preload(BASE+"theater_butterflies.wav")
const HIT_SOUND := preload(BASE+"written_impact.wav")
const WINGS := [
	preload("res://ManosabaLin/scenes/Ananlin/finished_draft/wings/butterfly_0.svg"),
	preload("res://ManosabaLin/scenes/Ananlin/finished_draft/wings/butterfly_1.svg"),
	preload("res://ManosabaLin/scenes/Ananlin/finished_draft/wings/butterfly_2.svg"),
	preload("res://ManosabaLin/scenes/Ananlin/finished_draft/wings/butterfly_3.svg"),
	preload("res://ManosabaLin/scenes/Ananlin/finished_draft/wings/butterfly_4.svg"),
	preload("res://ManosabaLin/scenes/Ananlin/finished_draft/wings/butterfly_5.svg"),
	preload("res://ManosabaLin/scenes/Ananlin/finished_draft/wings/butterfly_6.svg"),
	preload("res://ManosabaLin/scenes/Ananlin/finished_draft/wings/butterfly_7.svg"),
	preload("res://ManosabaLin/scenes/Ananlin/finished_draft/wings/butterfly_8.svg"),
	preload("res://ManosabaLin/scenes/Ananlin/finished_draft/wings/butterfly_9.svg")]
const INK := Color("6666cc")
const BLUE := Color("87ceeb")
const MOTE_LIMIT := 360
const HUES := [Color("ff0000"),Color("ffcc99"),Color("ee82ee"),Color("efc851"),Color("33ccff"),Color("7fff00"),BLUE,Color("ff9966"),Color("999999"),Color("cc9966")]
var curtain_audio: AudioStreamPlayer
var butterfly_audio: AudioStreamPlayer
var impact_audio: AudioStreamPlayer
var curtain_started := false
var butterflies_started := false
var dark_field: ColorRect
var spotlight: ColorRect
var backdrop: Node2D
var clock := 0.0
var ending := -1.0
var ready_sent := false
var hit_clock := -100.0
var impact_sent := true
var hit_serial := 0
var marks: Array[Dictionary] = []
var wings: Array[Sprite2D] = []
var phases := PackedFloat32Array()
var departures := PackedVector2Array()
var flight_starts := PackedFloat32Array()
var thread_upper := PackedVector2Array()
var thread_lower := PackedVector2Array()
var threads: Array[PackedVector2Array] = []
var wakes: Array[PackedVector2Array] = []
var mote_positions := PackedVector2Array()
var mote_velocities := PackedVector2Array()
var mote_births := PackedFloat32Array()
var mote_lives := PackedFloat32Array()
var mote_radii := PackedFloat32Array()
var mote_colors := PackedColorArray()
var mote_timer := 0.0
var mote_serial := 0

func _ready() -> void:
	dark_field=make_field(DARK,-2)
	spotlight=make_field(SPOT,-1)
	curtain_audio=make_audio(OPEN_SOUND)
	butterfly_audio=make_audio(WING_SOUND)
	impact_audio=make_audio(HIT_SOUND)
	butterfly_audio.finished.connect(func():
		if ending<0 and audio_enabled: butterfly_audio.play()
	)
	for i in range(24):
		var wing := Sprite2D.new()
		wing.texture=WINGS[i%10]
		var mat := ShaderMaterial.new()
		mat.shader=WING_SHADER
		wing.material=mat
		wing.z_index=1
		add_child(wing)
		wings.append(wing)
		phases.append(fmod(float(i)*2.399963,TAU))
		departures.append(Vector2.ZERO)
		flight_starts.append(-100)
		thread_upper.append(Vector2.ZERO)
		thread_lower.append(Vector2.ZERO)
		var curve := PackedVector2Array()
		curve.resize(33)
		threads.append(curve)
		var wake := PackedVector2Array()
		wake.resize(17)
		wakes.append(wake)
	mote_positions.resize(MOTE_LIMIT)
	mote_velocities.resize(MOTE_LIMIT)
	mote_births.resize(MOTE_LIMIT)
	mote_births.fill(-100)
	mote_lives.resize(MOTE_LIMIT)
	mote_radii.resize(MOTE_LIMIT)
	mote_colors.resize(MOTE_LIMIT)
	render_frame(0.0)

func make_field(shader: Shader, order: int) -> ColorRect:
	var field := ColorRect.new()
	field.size=SIZE
	field.mouse_filter=Control.MOUSE_FILTER_IGNORE
	field.z_index=order
	var mat := ShaderMaterial.new()
	mat.shader=shader
	mat.set_shader_parameter("target_uv",target_anchor)
	field.material=mat
	add_child(field)
	return field

func make_audio(stream: AudioStream) -> AudioStreamPlayer:
	var player := AudioStreamPlayer.new()
	player.stream=stream
	player.volume_db=linear_to_db(maxf(sfx_gain,.00001))
	add_child(player)
	return player

func strike() -> void:
	if wings.is_empty(): return
	hit_clock=clock
	impact_sent=false
	hit_serial+=1
	# Independent flight clocks let the previous butterfly finish its return.
	var selected := 10+(hit_serial-1)%14
	departures[selected]=wings[selected].position
	flight_starts[selected]=clock

func end_show() -> void:
	if ending<0: ending=clock

func flight(i: int, age: float, orbit: Vector2) -> Vector2:
	var target := target_anchor*SIZE
	var start := departures[i]
	var bend := sin(phases[i])*85.0
	if age<.78:
		var t := smoothstep(.15,.78,age)
		return start.bezier_interpolate(start+Vector2(100,-145+bend),target+Vector2(-155,-60+bend*.3),target,t)
	var t := smoothstep(.78,1.75,age)
	return target.bezier_interpolate(target+Vector2(150,55+bend),orbit+Vector2(145,140),orbit,t)

func _process(delta: float) -> void:
	if not autoplay: return
	clock+=delta
	var audio_fade := 1.0-smoothstep(ending,ending+1.5,clock) if ending>=0 else 1.0
	for audio in [curtain_audio,butterfly_audio,impact_audio]:
		audio.volume_db=linear_to_db(maxf(sfx_gain*audio_fade,.00001))
	if clock>=.35 and not curtain_started:
		curtain_started=true
		if audio_enabled: curtain_audio.play()
	if clock>=.7 and not butterflies_started:
		butterflies_started=true
		if audio_enabled: butterfly_audio.play()
	if clock>=2.35 and not ready_sent:
		ready_sent=true
		opened.emit()
	if clock-hit_clock>=.78 and not impact_sent:
		impact_sent=true
		stamp_mark()
		impact_audio.pitch_scale=1.0+float(hit_serial%3-1)*.035
		if audio_enabled: impact_audio.play()
		impact.emit()
	render_frame(delta)
	if ending>=0 and clock-ending>=1.65:
		closed.emit()
		queue_free()

func render_frame(delta: float) -> void:
	var fade := 1.0-smoothstep(ending+.5,ending+1.65,clock) if ending>=0 else 1.0
	modulate.a=fade
	dark_field.material.set_shader_parameter("strength",smoothstep(0,.65,clock))
	spotlight.material.set_shader_parameter("strength",smoothstep(.4,1.6,clock))
	spotlight.material.set_shader_parameter("clock",clock)
	spotlight.material.set_shader_parameter("accent",exp(-maxf(0,clock-hit_clock-.78)*7.0) if clock-hit_clock>=.78 and clock-hit_clock<1.6 else 0.0)
	if is_instance_valid(backdrop) and backdrop.is_node_ready(): backdrop.call("update_cloth",clock,fade*.95)
	scale=get_viewport_rect().size/SIZE
	var player := player_anchor*SIZE
	for i in range(wings.size()):
		var wing := wings[i]
		var phase := phases[i]
		var speed := .39+float(i%5)*.055
		var angle := clock*speed+phase
		var radius := 100.0+float(i%5)*29.0
		var orbit := player+Vector2(cos(angle)*radius,sin(angle)*78-32+sin(clock*.6+phase)*35)
		if i>=10:
			# The theatrical flock occupies several depths instead of stacking on the player.
			orbit+=Vector2(125+float(i%4)*48,-75+float(i%3)*45)
		# Emerge in four loose groups along the moving velvet lip, not a straight column.
		var group := float(i%4)
		var emerge := Vector2(690+group*80+sin(phase)*45,310+group*65+cos(phase)*70)
		var arrival := smoothstep(.65+group*.13,1.93+group*.18,clock)
		var previous := wing.position
		wing.position=emerge.bezier_interpolate(emerge+Vector2(-120,-80),orbit+Vector2(80,-60),orbit,arrival)
		var age := clock-flight_starts[i]
		if age>=0 and age<1.75:
			wing.position=flight(i,age,orbit)
			var wake := wakes[i]
			for j in range(17): wake[j]=flight(i,maxf(0,age-float(16-j)*.014),orbit)
			wakes[i]=wake
		if ending>=0:
			wing.position+=Vector2(cos(phase)*150,-110+sin(phase)*90)*pow(clock-ending,1.3)
		var depth := .5+.5*sin(angle)
		var tint: Color=HUES[i%10]
		wing.modulate=Color(tint,smoothstep(.65+group*.13,1.3+group*.13,clock)*lerpf(.65,1.0,depth))
		wing.scale=Vector2.ONE*(.33+float(i%3)*.028)*lerpf(.9,1.08,depth)
		var desired := sin(angle)*.14
		if delta>0 and age>=.15 and age<1.75:
			desired=clampf((wing.position-previous).angle()+PI*.5,-.65,.65)
		wing.rotation=lerp_angle(wing.rotation,desired,1.0-exp(-delta*8.0)) if delta>0 else desired
		wing.material.set_shader_parameter("wing_phase",clock*(8.6+float(i%3)*.75)+phase)
		if i>=10:
			var top := thread_origin(i)
			var tension := smoothstep(0,.40,age)*(1.0-smoothstep(.80,1.7,age))
			var span := wing.position-top
			var sag := lerpf(115,46,tension)+maxf(0,clock-ending)*48 if ending>=0 else lerpf(115,46,tension)
			var upper := top+Vector2(span.x*.11,span.y*.39)
			var lower := wing.position-Vector2(span.x*.25,span.y*.12)+Vector2(0,sag)
			var follow := 1.0-exp(-delta*8.0)
			if thread_upper[i]==Vector2.ZERO or delta<=0: follow=1
			thread_upper[i]=thread_upper[i].lerp(upper,follow)
			thread_lower[i]=thread_lower[i].lerp(lower,follow)
			var curve := threads[i]
			for j in range(33):
				curve[j]=top.bezier_interpolate(thread_upper[i],thread_lower[i],wing.position,float(j)/32)
			threads[i]=curve
	if clock>.85 and ending<0 and delta>0:
		mote_timer+=minf(delta,.08)
		while mote_timer>=.016:
			mote_timer-=.016
			for j in range(4): emit_mote()
	for i in range(marks.size()-1,-1,-1):
		if clock-float(marks[i]["born"])>2.15: marks.remove_at(i)
	queue_redraw()

func thread_origin(i: int) -> Vector2:
	return Vector2(580+float(i-10)*83+sin(phases[i])*17,-45)

func emit_mote() -> void:
	var slot := mote_serial%MOTE_LIMIT
	var owner := mote_serial%24
	var phase := phases[owner]+float(mote_serial)*2.399963
	var offset := Vector2.from_angle(phase)*(18+float(mote_serial%65))
	mote_positions[slot]=wings[owner].position+offset
	mote_velocities[slot]=Vector2(-sin(phase)*24,-18+cos(phase)*22)
	mote_births[slot]=clock
	mote_lives[slot]=.8+float(mote_serial%7)*.13
	mote_radii[slot]=7+float(mote_serial%4)*3
	mote_colors[slot]=HUES[mote_serial%10]
	mote_serial+=1

func stamp_mark() -> void:
	var target := target_anchor*SIZE
	var variant := hit_serial%4
	var tilt := -.23+float(variant)*.14
	var a := Vector2(-285,85)
	var b := Vector2(-140,-195-float(variant)*12)
	var c := Vector2(60,180-float(variant)*42)
	var d := Vector2(285,-80)
	var points := PackedVector2Array()
	var pressure := PackedFloat32Array()
	for j in range(65):
		var u := float(j)/64
		points.append(target+a.bezier_interpolate(b,c,d,u).rotated(tilt))
		pressure.append(pow(maxf(sin(u*PI),.001),.55)*(4.0+4.0*pow(sin(u*PI*1.4+.3),2)))
	marks.append({"born":clock,"points":points,"pressure":pressure})
	if marks.size()>5: marks.pop_front()

func draw_glow(point: Vector2, radius: float, color: Color, alpha: float) -> void:
	draw_texture_rect(MOTE,Rect2(point-Vector2.ONE*radius,Vector2.ONE*radius*2),false,Color(color,alpha))

func draw_mark(mark: Dictionary) -> void:
	var elapsed := clock-float(mark["born"])
	var alpha := 1.0-smoothstep(.75,2.15,elapsed)
	var points: PackedVector2Array=mark["points"]
	var pressure: PackedFloat32Array=mark["pressure"]
	var count := mini(65,2+int(smoothstep(0,.32,elapsed)*63))
	var center := points.slice(0,count)
	# Broad soft skirts and pressure-shaped ink. No repeated uniform sine stroke.
	draw_polyline(center,Color(INK,alpha*.07),36,true)
	draw_polyline(center,Color(INK,alpha*.19),17,true)
	var poly := PackedVector2Array()
	poly.resize(count*2)
	for j in range(count):
		var normal := (points[mini(j+1,64)]-points[maxi(0,j-1)]).normalized().orthogonal()
		poly[j]=points[j]+normal*pressure[j]
		poly[count*2-1-j]=points[j]-normal*pressure[j]
	draw_colored_polygon(poly,Color(INK.lerp(Color.WHITE,.18),alpha*.9))
	draw_polyline(center,Color(.86,.84,1.0,alpha*.75),1.3,true)
	if elapsed<.38:
		draw_glow(center[count-1],37,INK,alpha)
		draw_glow(center[count-1],11,Color.WHITE,alpha*.8)
	# Small lifted flecks leave the end of the pen stroke, then drift rather than flash.
	for i in range(12):
		var age := elapsed-float(i%3)*.035
		if age<0 or age>1.05: continue
		var base := points[12+(i*4)%45]
		var direction := Vector2.from_angle(-PI*.8+float(i)*.39)
		var point := base+direction*(18+float(i%4)*11)*(1.0-exp(-age*3.0))+Vector2(0,-age*24)
		draw_glow(point,7+float(i%3)*3,INK.lerp(Color.WHITE,.35),smoothstep(0,.08,age)*(1.0-smoothstep(.35,1.05,age))*.85)

func _draw() -> void:
	var opening := smoothstep(.65,2.15,clock)
	for i in range(MOTE_LIMIT):
		var age := clock-mote_births[i]
		var life := mote_lives[i]
		if age<0 or age>=life: continue
		var alpha := smoothstep(0,.1,age)*(1.0-smoothstep(life*.3,life,age))
		var point := mote_positions[i]+mote_velocities[i]*age+Vector2(sin(age*2+i)*4,-age*age*8)
		draw_glow(point,mote_radii[i],mote_colors[i],alpha*.9)
	for i in range(10,wings.size()):
		var age := clock-flight_starts[i]
		var tension := smoothstep(0,.40,age)*(1.0-smoothstep(.80,1.7,age))
		var points := threads[i]
		draw_polyline(points,Color(INK.lerp(Color.WHITE,.27),opening*(.16+tension*.40)),1.1,true)
		if tension>.005:
			var wake := wakes[i]
			if age>.18 and age<1.3:
				draw_polyline(wake,Color(HUES[i%10],tension*.09),19,true)
				draw_polyline(wake,Color(HUES[i%10].lerp(INK,.4),tension*.45),3.0,true)
			draw_polyline(points,Color(INK,opening*tension*.07),13,true)
			draw_polyline(points,Color(INK,opening*tension*.25),4,true)
			# Visible traveling light follows the real curved tether down to the butterfly.
			var head := clampf(age/.78,0,1)*32
			var pulse := points.slice(maxi(0,int(head)-8),mini(33,int(head)+2))
			if pulse.size()>1:
				draw_polyline(pulse,Color(.83,.81,1.0,opening*tension*.86),2.0,true)
			draw_glow(wings[i].position,32,INK,tension*.75)
	for mark in marks: draw_mark(mark)

func prepare_idle() -> void:
	set_process(false)
	autoplay=false
	audio_enabled=false
	visible=false
	if is_instance_valid(backdrop): backdrop.hide()

func begin_show() -> void:
	set_process(true)
	# A prepared scene learns the actual enemy only when the card is played.
	spotlight.material.set_shader_parameter("target_uv",target_anchor)
	dark_field.material.set_shader_parameter("target_uv",target_anchor)
	clock=0
	ending=-1
	hit_clock=-100
	hit_serial=0
	ready_sent=false
	impact_sent=true
	curtain_started=false
	butterflies_started=false
	marks.clear()
	flight_starts.fill(-100)
	mote_births.fill(-100)
	mote_timer=0
	mote_serial=0
	thread_upper.fill(Vector2.ZERO)
	thread_lower.fill(Vector2.ZERO)
	autoplay=true
	audio_enabled=true
	visible=true
	if is_instance_valid(backdrop): backdrop.show()
	render_frame(0.0)

func prewarm_at(seconds: float) -> void:
	prepare_idle()
	visible=true
	if is_instance_valid(backdrop): backdrop.show()
	clock=seconds
	render_frame(0.0)
	if seconds>2.5:
		hit_serial=1
		hit_clock=seconds-.85
		departures[10]=wings[10].position
		flight_starts[10]=hit_clock
		marks.clear()
		stamp_mark()
		marks[0]["born"]=seconds-.35
		for i in range(48):
			emit_mote()
			mote_births[(mote_serial-1)%MOTE_LIMIT]=seconds-.2
		render_frame(0.0)
