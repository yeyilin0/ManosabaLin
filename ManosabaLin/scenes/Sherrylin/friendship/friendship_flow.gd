extends Node2D
## Analytical motion permits deterministic seeking and keeps behavior independent of FPS.
const SIZE := Vector2(1920,1080)
const CENTER := Vector2(960,556)
const BLUE := Color("33ccff")
const GREEN := Color("9ff5a5")
const PINK := Color("ff99cc")
const ORANGE := Color("ffcc99")
const BACKS := preload("res://ManosabaLin/scenes/Sherrylin/friendship/textures/light_backs.png")
const MOTE := preload("res://ManosabaLin/scenes/Sherrylin/friendship/glow_mote.svg")
static var cached_targets := PackedVector2Array()
var opening := true
var clock := 0.0
var mode := 0
var player := Vector2(420,580)
var target := Vector2(960,510)
var seeds: Array[Vector4] = []
var silhouette_targets: PackedVector2Array = []
var petal_shape := PackedVector2Array()
var random := RandomNumberGenerator.new()

func _ready() -> void:
	# This RNG is purely cosmetic and never touches combat/card RNG.
	random.seed=719031
	for i in range(420): seeds.append(Vector4(random.randf(),random.randf(),random.randf(),random.randf()))
	for i in range(24):
		var a := float(i)/24.0*TAU
		petal_shape.append(Vector2(cos(a)*(.62+.38*cos(a)),sin(a)*.48))
	if opening:
		if not cached_targets.is_empty():
			silhouette_targets=cached_targets
			return
		# Converging dust meets the actual generated luminous contours rather than a generic oval.
		var source := BACKS.get_image()
		if source.is_compressed(): source.decompress()
		for i in range(110):
			var found := Vector2(.5,.5)
			for attempt in range(60):
				var x := random.randi_range(0,source.get_width()-1)
				var y := random.randi_range(0,source.get_height()-1)
				var pixel := source.get_pixel(x,y)
				if pixel.a>.4 and pixel.get_luminance()>.46:
					found=Vector2(float(x)/source.get_width(),float(y)/source.get_height())
					break
			silhouette_targets.append(((found-Vector2(.5,.5))*.94+Vector2(.5,.5))*SIZE)
		cached_targets=silhouette_targets

func render_at(seconds: float, effect: int, origin: Vector2, destination: Vector2) -> void:
	clock=seconds
	mode=effect
	player=origin
	target=destination
	queue_redraw()

func tint(color: Color, alpha: float) -> Color:
	return Color(color.r,color.g,color.b,clampf(alpha,0.0,1.0))

func glow(point: Vector2, radius: float, color: Color, alpha: float) -> void:
	if alpha<.003: return
	# One cached soft texture replaces four tessellated circles per light.
	var extent := maxf(radius*5.0,3.0)
	draw_texture_rect(MOTE,Rect2(point-Vector2.ONE*extent,Vector2.ONE*extent*2),false,tint(color.lerp(Color.WHITE,.28),alpha))

func petal(point: Vector2, size: float, angle: float, phase: float, color: Color, alpha: float) -> void:
	if alpha<.003: return
	var points := PackedVector2Array()
	var colors := PackedColorArray()
	var tumble := .30+.70*absf(sin(phase))
	for p in petal_shape:
		points.append(point+Vector2(p.x,p.y*tumble).rotated(angle)*size)
		colors.append(tint(color.lerp(Color.WHITE,(p.y+.5)*.35),alpha))
	draw_circle(point,size*1.5,tint(color,alpha*.025))
	draw_polygon(points,colors)
	draw_line(point-Vector2(size*.12,0).rotated(angle),point+Vector2(size*.64,0).rotated(angle),tint(Color.WHITE,alpha*.48),.9,true)

func bezier(a: Vector2,b: Vector2,c: Vector2,d: Vector2,t: float) -> Vector2:
	return a.bezier_interpolate(b,c,d,clampf(t,0.0,1.0))

func ribbon(points: PackedVector2Array, width: float, color: Color, alpha: float) -> void:
	if points.size()<3 or alpha<.003: return
	var clean := PackedVector2Array()
	for point in points:
		if clean.is_empty() or clean[clean.size()-1].distance_squared_to(point)>.01:
			clean.append(point)
	if clean.size()<3: return
	# Vertex-colored tapered ribbons: a broad transparent skirt and a thin liquid center.
	for pass_index in range(3):
		var poly := PackedVector2Array()
		var colors := PackedColorArray()
		var multiplier := 3.8 if pass_index==0 else (1.4 if pass_index==1 else .42)
		var opacity := .028 if pass_index==0 else (.15 if pass_index==1 else .64)
		for side in [1.0,-1.0]:
			for j in range(clean.size()):
				var i := j if side>0 else clean.size()-1-j
				var before := clean[maxi(i-1,0)]
				var after := clean[mini(i+1,clean.size()-1)]
				var normal := (after-before).normalized().orthogonal()
				var u := float(i)/float(clean.size()-1)
				var taper := pow(maxf(sin(u*PI),0.0),.65)
				poly.append(clean[i]+normal*width*multiplier*maxf(taper,.005)*side)
				colors.append(tint(color.lerp(Color.WHITE,.35 if pass_index==2 else .0),alpha*opacity*taper))
		# A center line that hooks back on itself (mode 2's beam does this whenever the
		# destination is the energy counter in the top-left, because c2 pulls above and
		# left of it) makes the two offset edges cross, so ear clipping yields nothing.
		# draw_polygon then prints "Invalid polygon data, triangulation failed" every
		# frame and draws nothing at all. Pre-check here and fall back to a polyline of
		# the same colour, which is strictly better than the nothing we draw today.
		if Geometry2D.triangulate_polygon(poly).is_empty():
			draw_polyline(clean,tint(color.lerp(Color.WHITE,.35 if pass_index==2 else .0),alpha*opacity),width*multiplier,true)
		else:
			draw_polygon(poly,colors)

func _draw() -> void:
	if mode!=0:
		draw_pulse()
		return
	draw_entrance()
	draw_rim()
	draw_convergence()
	draw_release()

func draw_entrance() -> void:
	for side in range(2):
		var age := clock-float(side)*.09
		if age<0 or age>1.12: continue
		var t := smoothstep(0.0,.82,age)
		var start := player+Vector2(-80,-15) if side==0 else Vector2(1530,880)
		var c1 := Vector2(380,185) if side==0 else Vector2(1480,220)
		var c2 := CENTER+Vector2(-200,-190) if side==0 else CENTER+Vector2(230,-180)
		var points := PackedVector2Array()
		for k in range(50):
			var u := maxf(0.0,t-float(49-k)/49.0*.40)
			points.append(bezier(start,c1,c2,CENTER,u))
		var color := BLUE if side==0 else GREEN
		var alpha := (1.0-smoothstep(.75,1.12,age))
		ribbon(points,6.5,color,alpha)
		glow(points[points.size()-1],5.0,color,alpha)

func draw_rim() -> void:
	var alpha := smoothstep(.16,.65,clock)*(1.0-smoothstep(2.65,3.45,clock))
	if alpha<.003: return
	var radius := lerpf(24.0,340.0,smoothstep(.08,.75,clock))+smoothstep(2.58,3.28,clock)*237.0
	for i in range(42):
		var seed := seeds[i]
		var angle := seed.x*TAU+clock*(.06+seed.y*.10)
		var drift := sin(angle*7.0+clock*.8)*5.0+sin(angle*13.0-clock*.7)*3.0
		var point := CENTER+Vector2.from_angle(angle)*(radius+drift)
		var color := BLUE.lerp(GREEN,seed.z).lerp(PINK,seed.w*.27)
		glow(point,1.1+seed.y*2.1,color,alpha*(.30+.30*sin(clock*2.0+seed.z*TAU)))
		if i%4==0:
			var trail := PackedVector2Array()
			for j in range(15): trail.append(CENTER+Vector2.from_angle(angle-float(j)*.005)*(radius+drift))
			ribbon(trail,1.0,color,alpha*.7)

func draw_convergence() -> void:
	if clock<.7 or clock>2.85: return
	for i in range(silhouette_targets.size()):
		var seed := seeds[i]
		var dest := silhouette_targets[i]
		var begin := .70+seed.y*.65
		var duration := .52+seed.z*.38
		var age := (clock-begin)/duration
		if age<0 or age>1.6: continue
		var angle := seed.x*TAU
		var start := CENTER+Vector2(cos(angle)*1120.0,sin(angle)*680.0)
		var side := -1.0 if dest.x<960 else 1.0
		var c1 := start+Vector2(-side*100,-170)
		var c2 := dest+Vector2(side*100,40+seed.w*140)
		var u := smoothstep(0.0,1.0,age)
		var point := bezier(start,c1,c2,dest,u)
		var alpha := smoothstep(0.0,.18,age)*(1.0-smoothstep(1.0,1.55,age))
		var color := BLUE if side<0 else GREEN
		glow(point,1.2+seed.z*1.4,color,alpha*.65)
		if i%3==0:
			var points := PackedVector2Array()
			for k in range(12): points.append(bezier(start,c1,c2,dest,smoothstep(0.0,1.0,maxf(0,age-float(11-k)*.008))))
			ribbon(points,1.0+seed.z*.7,color,alpha*.62)

func draw_release() -> void:
	if clock<2.68: return
	var palette := [BLUE,GREEN,PINK,ORANGE,Color("bd95ff"),Color("fff09b"),Color("7df2e8")]
	for i in range(360):
		var seed := seeds[i]
		var age := clock-(2.70+seed.w*.25)
		if age<0: continue
		var angle := seed.x*TAU
		var velocity := 390.0+seed.y*900.0
		var travel := velocity*(1.0-exp(-age*2.3))/2.3
		var radius := 55.0+seed.z*270.0
		var origin := CENTER+Vector2.from_angle(angle)*radius
		var point := origin+Vector2.from_angle(angle)*travel+Vector2(sin(age*2.8+seed.z*8.0)*age*13.0,age*age*58.0)
		var fade := smoothstep(0.0,.16,age)*(1.0-smoothstep(.82,1.47,age))*(1.0-smoothstep(3.88,4.2,clock))
		var color: Color=palette[i%palette.size()]
		if i%11==0:
			var twinkle := pow(.5+.5*sin(age*(5.0+seed.z*3.0)+seed.w*TAU),3.0)
			glow(point,2.8+seed.z*1.5,color,fade*.8)
			var diamond := PackedVector2Array()
			# (5+seed.z*10)*twinkle is the diamond's half-height. Below roughly half a
			# pixel the sliver is invisible and ear clipping degenerates, which used to
			# spam "Invalid polygon data, triangulation failed" every frame.
			var diamond_scale := (5+seed.z*10)*twinkle
			if diamond_scale>.5:
				for v in [Vector2(0,-1),Vector2(.25,0),Vector2(0,1),Vector2(-.25,0)]:
					diamond.append(point+v.rotated(angle*.15)*diamond_scale)
				draw_colored_polygon(diamond,tint(color.lerp(Color.WHITE,.76),fade*.65))
		elif i%4==0:
			petal(point,5.0+seed.z*9.0,angle+age*(seed.y-.5)*3.0,seed.w*TAU+age*3.0,color,fade*.82)
		else:
			glow(point,.8+seed.z*2.0,color,fade*.68)
	# Fragmented curved filaments ride the spreading rainbow, each peeling off at a different speed.
	for i in range(12):
		var seed := seeds[370+i]
		var age := maxf(0,clock-2.75-seed.w*.14)
		var angle := seed.x*TAU+age*(seed.y-.5)*.26
		var radius := 300.0+(1.0-exp(-age*1.8))*(430+seed.z*360)
		var points := PackedVector2Array()
		for k in range(36):
			var q := float(k)/35.0
			var a := angle+(q-.5)*(.12+seed.y*.23)
			var r := radius+sin(q*PI)*17.0+sin(a*3+clock)*9.0
			points.append(CENTER+Vector2.from_angle(a)*r)
		var fade := smoothstep(0,.17,age)*(1.0-smoothstep(.73,1.30,age))
		var color: Color=palette[i%palette.size()]
		ribbon(points,2.0+seed.z*3.0,color,fade*.85)
	# Two final arcs deliver the memory's blue/green light to the actual player position.
	for i in range(2):
		var u := smoothstep(3.06+float(i)*.07,3.97,clock)
		var points := PackedVector2Array()
		var end := player+Vector2(float(i*2-1)*22,-12)
		for k in range(35):
			var q := maxf(0,u-float(34-k)*.008)
			points.append(bezier(CENTER,CENTER+Vector2(-260,-140-float(i)*75),end+Vector2(140,-190),end,q))
		var fade := smoothstep(3.04,3.20,clock)*(1.0-smoothstep(3.87,4.2,clock))
		var color := BLUE if i==0 else GREEN
		ribbon(points,3.1,color,fade*.8)
		glow(points[points.size()-1],3.5,color,fade*.65)

func pulse_arc(center: Vector2, radius: Vector2, rotation_angle: float, extent: float, color: Color, alpha: float) -> void:
	var points := PackedVector2Array()
	for j in range(31):
		var t := float(j)/30
		var a := rotation_angle+(t-.5)*extent
		points.append(center+Vector2(cos(a)*radius.x,sin(a)*radius.y))
	ribbon(points,2.0,color,alpha)

func pulse_star(point: Vector2, size: float, color: Color, alpha: float) -> void:
	glow(point,size*.65,color,alpha*.65)
	var diamond := PackedVector2Array([point+Vector2(0,-size),point+Vector2(size*.22,0),point+Vector2(0,size),point-Vector2(size*.22,0)])
	draw_colored_polygon(diamond,tint(color.lerp(Color.WHITE,.65),alpha*.75))

func draw_pulse() -> void:
	var fade := smoothstep(0.0,.09,clock)*(1.0-smoothstep(.79,1.15,clock))
	if mode==1 or mode==2:
		var color := BLUE if mode==1 else GREEN
		var companion := GREEN if mode==1 else BLUE
		var origin := player+Vector2(0,-35)
		var c1 := origin+Vector2(130,-135)
		var c2 := target+Vector2(-110,-165)
		var t := smoothstep(.04,.76,clock)
		# A paired stream of blue/green light, each peel travelling at its own pace.
		for lane in range(3):
			var points := PackedVector2Array()
			for k in range(32):
				var q := maxf(0,t-float(31-k)*.012-float(lane)*.035)
				var p := bezier(origin,c1,c2,target,q)
				var separation := sin(q*PI)*(11+float(lane)*7)
				p+=Vector2(sin(q*TAU+clock*3+float(lane)*PI),cos(q*TAU+clock*3+float(lane)*PI)*.4)*separation
				points.append(p)
			ribbon(points,(3.8 if mode==2 else 2.2)*(1.0-float(lane)*.2),color if lane==0 else companion,fade*(.95-float(lane)*.16))
		var head := bezier(origin,c1,c2,target,t)
		glow(head,7.0,color,fade*.85)
		if mode==1:
			for j in range(3):
				petal(head+Vector2(-j*15,sin(clock*4+j)*12),18.0-float(j)*3,-.7+clock*1.7+j*.5,clock*5+j,color.lerp(companion,float(j)*.25),fade*.9)
		for i in range(28):
			var seed := seeds[i]
			var q := clampf(t-seed.x*.32,0,1)
			var p := bezier(origin,c1,c2,target,q)+Vector2(sin(seed.y*TAU+clock*3),cos(seed.z*TAU+clock*2))*seed.w*22.0
			glow(p,1.3+seed.y*1.6,color.lerp(companion,seed.z),fade*.7)
		var arrival := smoothstep(.55,.76,clock)*(1.0-smoothstep(.87,1.15,clock))
		var radius := 28+smoothstep(.55,1.13,clock)*64
		for j in range(3):
			pulse_arc(target,Vector2(radius,radius*.68),clock*1.5+float(j)*TAU/3,1.1,color.lerp(companion,float(j)*.4),arrival*.95)
		for j in range(6):
			var angle := float(j)*2.399963
			pulse_star(target+Vector2.from_angle(angle)*radius,5+float(j%3)*2,color.lerp(Color.WHITE,.3),arrival*.8)
	elif mode==3:
		# Two rising strands wrap the body; the back halves are dimmer than the front.
		for lane in range(2):
			var points := PackedVector2Array()
			for k in range(48):
				var u := float(k)/47
				var angle := u*TAU*1.2-clock*3+lane*PI
				points.append(player+Vector2(cos(angle)*(95-u*38),65-u*245+sin(angle)*24))
			ribbon(points,3.2,PINK if lane==0 else GREEN,fade*.8)
		for i in range(38):
			var seed := seeds[i]
			var angle := seed.x*TAU+clock*(1.4+seed.y)
			var p := player+Vector2(cos(angle)*(65+seed.y*65),sin(angle)*36+48-clock*180+seed.z*55)
			petal(p,9+seed.y*11,angle*.6,clock*4+seed.w*TAU,PINK.lerp(GREEN,seed.z*.4),fade*(.5+seed.z*.4))
		pulse_arc(player+Vector2(0,65),Vector2(120,36),clock*1.2,4.8,PINK,fade*.75)
		glow(player+Vector2(0,-40-clock*35),29.0,PINK,fade*.35)
	elif mode==4:
		for side in [-1.0,1.0]:
			var points := PackedVector2Array()
			for k in range(35):
				var t := float(k)/34
				var a := -PI*.55+t*PI*1.35
				points.append(target+Vector2(side*(65+sin(a)*24),cos(a)*110-clock*30))
			ribbon(points,2.5,ORANGE.lerp(PINK,.3),fade*.8)
		for i in range(44):
			var seed := seeds[i]
			var side := -1.0 if i%2==0 else 1.0
			var origin := target+Vector2(side*66,(seed.z-.5)*150)
			var age := maxf(clock-seed.w*.12,0)
			var p := origin+Vector2(side*(35+seed.x*125)*(1.0-exp(-age*2.0)), -age*(75+seed.y*140))
			var hue := ORANGE.lerp(PINK,seed.z*.55)
			petal(p,8+seed.y*11,seed.x*TAU+age,age*4+seed.z*TAU,hue,fade*.8)
			if i%4==0: pulse_star(p+Vector2(0,-12),5+seed.y*5,hue,fade*.7)
		glow(target,43.0,ORANGE,fade*(1.0-smoothstep(.1,.58,clock))*.45)
