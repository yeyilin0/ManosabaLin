extends Node2D
## Analytic motion: seeking and variable frame rate produce the same paths.
## Flower silhouettes are luminous calligraphic ornaments, not opaque plants.
const BASE := "res://ManosabaLin/scenes/Emalin/true_ending/"
const ATLAS := preload(BASE + "textures/botanicals.png")
const FIGURES := preload(BASE + "textures/reunion.png")
const CONTOURS := preload(BASE + "reunion_contours.gd")
const PINK := Color(1.0,.66,.83)
const RED := Color(.81,.18,.29)
const PEARL := Color(1.0,.92,.98)
const AURA_BLUE := Color(.62,.78,1.0)
const AURA_PEARL := Color(.89,.96,1.0)
const RIBBON_WIDTHS := [3.8,1.8,1.0]
const RIBBON_OPACITY := [.055,.12,.62]
# Generated ornaments have deliberately unequal silhouettes. These measured
# regions avoid cutting a tall spray at a nominal equal-grid boundary.
const REGIONS := [
	Rect2(0,0,490,548), Rect2(506,0,409,544),
	Rect2(930,0,438,544), Rect2(1390,0,384,540),
	Rect2(38,549,448,338), Rect2(498,548,418,339),
	Rect2(930,544,418,343), Rect2(1390,540,384,347)
]
var seconds := 0.0
var perimeter_gain := 1.0
var world := Vector2(1920,1080)
var player := Vector2(422,594)
var progress := 0
var events: Array[Dictionary] = []
var contours := PackedVector2Array(CONTOURS.POINTS)
var atlas_regions: Array[Rect2] = []
var figure_size := Vector2(FIGURES.get_size())
var figure_fit := 1.0
var last_viewport := Vector2.ZERO

func _init() -> void:
	# Immutable atlas bounds are reused by all petals instead of queried per sprite/frame.
	var atlas_scale := Vector2(ATLAS.get_size())/Vector2(1774,887)
	for source in REGIONS:
		var region: Rect2 = source
		region.position *= atlas_scale
		region.size *= atlas_scale
		atlas_regions.append(region)

func rnd(index: int, salt: int) -> float:
	return fposmod(sin(float(index)*127.1 + float(salt)*311.7)*43758.5453,1.0)

func render_at(time: float, viewport: Vector2, player_uv: Vector2, collected: int) -> void:
	seconds = time
	if viewport != last_viewport:
		last_viewport = viewport
		var unit := viewport.y/1080.0
		scale = Vector2.ONE*unit
		world = viewport/maxf(unit,.0001)
		figure_fit = minf(world.x*.92/figure_size.x,world.y*.94/figure_size.y)
	player = player_uv*world
	progress = collected
	for i in range(events.size()-1,-1,-1):
		if seconds-float(events[i].time) > memory_duration(int(events[i].kind)): events.remove_at(i)
	queue_redraw()

func add_memory(kind: int, origin_uv: Vector2, collected: int, time: float) -> void:
	if events.size() >= 18: events.pop_front()
	events.append({"kind":kind,"origin":origin_uv,"count":collected,"time":time})

func reset_memory() -> void:
	events.clear()

func core(side: int, time: float) -> Vector2:
	var enter := smoothstep(0.0,.84,time)
	var orbit := smoothstep(.62,1.48,time)
	var close := smoothstep(1.05,1.82,time)
	var angle := float(side)*PI+orbit*2.0
	var start := Vector2(-.57,.10) if side==1 else Vector2(.57,-.10)
	var ring := Vector2(cos(angle)*.21,sin(angle)*.155)
	return Vector2(world.x*.5,1080*.485) + start.lerp(ring,enter).lerp(Vector2.ZERO,close)*1080

func figure_point(uv: Vector2) -> Vector2:
	return world*.5+(uv-Vector2(.5,.5))*figure_size*figure_fit

func _draw() -> void:
	if seconds < 2.3: draw_approach()
	if seconds > .82 and seconds < 2.55: draw_gathering()
	if seconds > 1.9 and seconds < 3.65: draw_tears_and_blossoms()
	if seconds > 2.94 and seconds < 5.32: draw_release()
	if seconds > 4.05 and perimeter_gain > .001: draw_perimeter()
	for event in events: draw_memory(event)

func draw_approach() -> void:
	var envelope := smoothstep(0.0,.18,seconds)*(1.0-smoothstep(1.68,2.27,seconds))
	for side in range(2):
		var hue := PINK if side==0 else RED
		for strand in range(4):
			var path := PackedVector2Array()
			for j in range(36):
				var back := float(j)/35.0
				var point := core(side,maxf(0.0,seconds-back*.59))
				var curl := Vector2(sin(back*8.0-seconds*3.0+strand),cos(back*11.0+seconds*2.0+strand))
				point += curl*(6.0+strand*5.4)*back
				path.append(point)
			ribbon(path,6.0+strand*1.44,hue,envelope*.52)
		for i in range(28):
			var seed := i+side*50
			var age := fposmod(seconds*.8+rnd(seed,4),1.0)
			var point := core(side,maxf(0.0,seconds-age*.60))
			point += Vector2(sin(seed*2.0+seconds*1.9),cos(seed+seconds*1.5))*(24+age*120)
			var alpha := envelope*(1.0-age)*.64
			if i%5==0: ornament(4+i%4,point,36+rnd(seed,5)*42,seconds+rnd(seed,6)*TAU,.75,hue,alpha)
			else: glow(point,4.5+rnd(seed,5)*6.0,hue,alpha)

func draw_gathering() -> void:
	for i in range(contours.size()):
		var onset := .84+rnd(i,8)*.65
		var u := clampf((seconds-onset)/.82,0.0,1.0)
		if u <= 0.0 or u >= 1.0: continue
		var target := figure_point(contours[i])
		var angle := rnd(i,9)*TAU
		var start := target+Vector2(cos(angle)*world.x*.32,sin(angle)*380)
		var bend := (start+target)*.5+Vector2(sin(angle+1.0)*85,cos(angle)*95)
		var point := bezier(start,bend,target,u)
		var hue := PINK if contours[i].x<.52 else RED.lerp(PEARL,.28)
		var alpha := sin(u*PI)*.58
		glow(point,1.8+rnd(i,10)*2.6,hue,alpha)
		if i%4==0:
			var path := PackedVector2Array()
			for j in range(9): path.append(bezier(start,bend,target,maxf(0.0,u-float(j)*.011)))
			ribbon(path,1.2,hue,alpha*.6)

func draw_tears_and_blossoms() -> void:
	# Anchors on the generated Ema/Hiro cutout; the face remains registered.
	for i in range(2):
		var age := seconds-(2.13+float(i)*.21)
		if age<0.0 or age>.91: continue
		var uv := Vector2(.49,.308) if i==0 else Vector2(.62,.247)
		uv += Vector2(-.009*age,.017*age+.08*age*age)
		var point := figure_point(uv)
		var alpha := smoothstep(0.0,.09,age)*(1.0-smoothstep(.60,.91,age))
		glow(point,2.7,PEARL,alpha*.76)
		draw_line(point,point-Vector2(0,3+age*5),Color(1.0,.88,.96,alpha*.24),1.0,true)
	for i in range(3):
		var age := seconds-(2.27+float(i)*.19)
		var alpha := smoothstep(0.0,.17,age)*(1.0-smoothstep(.72,1.10,age))
		var point := figure_point(Vector2(.49+float(i)*.07,.504))
		glow(point,10.0,PEARL,alpha*.46)
		ornament(6,point,40.0,0.1*float(i),1.0,PINK,alpha*.36)

func draw_release() -> void:
	var center := Vector2(world.x*.5,1080*.51)
	# Three depth families; each curling spray unfurls from its base independently.
	for i in range(52):
		var age := seconds-(3.02+rnd(i,21)*.40)
		if age<=0.0 or age>1.8: continue
		var u := clampf(age/1.45,0.0,1.0)
		var angle := float(i)*2.399963+rnd(i,22)*.45
		var depth := rnd(i,23)
		var travel := .13+(1.0-exp(-age*1.3))*(.43+depth*.31)
		var point := center+Vector2(cos(angle)*world.x*.64,sin(angle)*1080*.73)*travel
		point += Vector2(sin(age*1.7+float(i))*15,cos(age*1.4+i)*11)
		var alpha := smoothstep(0.0,.25,age)*(1.0-smoothstep(1.03,1.78,age))
		var hue := PINK.lerp(Color(.79,.65,1.0),depth*.42).lerp(PEARL,rnd(i,24)*.45)
		var twist := angle+PI*.5+sin(age*1.8+float(i))*.12
		var height := (100.0+depth*130.0)*(.72+.38*u)
		ornament(i%4,point,height*1.24,twist,.84+sin(age*1.5+i)*.08,hue,alpha*.72,smoothstep(0.0,.5,age))
	# Near petals tumble through the light field; dust follows slower, finer eddies.
	for i in range(270):
		var age := seconds-(3.05+rnd(i,31)*.40)
		if age<=0.0: continue
		var angle := float(i)*2.399963+rnd(i,32)*.36
		var depth := rnd(i,33)
		var speed := 470.0+depth*830.0
		var radius := 30.0+speed*(1.0-exp(-age*1.50))/1.50
		var point := center+Vector2(cos(angle)*world.x/1920.0,sin(angle)*.82)*radius
		point += Vector2(sin(age*2.2+i)*age*13,age*age*(25.0+depth*34.0))
		var alpha := smoothstep(0.0,.16,age)*(1.0-smoothstep(1.15,1.96,age))*(1.0-smoothstep(4.89,5.3,seconds))
		var hue := PINK.lerp(PEARL,rnd(i,34)*.75)
		if i%3==0:
			var spin := rnd(i,35)*TAU+age*(.38+depth*1.2)
			var flip := cos(age*(2.5+depth*2.0)+rnd(i,36)*TAU)
			if absf(flip)<.08: flip=.08 if flip>=0.0 else -.08
			ornament(4+i%4,point,27.0+pow(depth,2.0)*68.0,spin,flip,hue,alpha*(.55+depth*.38))
		else:
			glow(point,1.8+depth*3.1,hue,alpha*.82)
	# Thin broken flow lines connect the unfurling motifs without a rigid wheel.
	for i in range(14):
		var age := seconds-3.07-rnd(i,38)*.30
		if age<=0.0 or age>1.65: continue
		var angle := rnd(i,39)*TAU
		var path := PackedVector2Array()
		for j in range(26):
			var u := float(j)/25.0
			var theta := angle+u*(.16+rnd(i,40)*.31)+sin(age*.8)*.06
			var radius := 80.0+(1.0-exp(-age*1.2))*800.0+sin(u*PI)*24.0
			path.append(center+Vector2(cos(theta)*world.x/1920.0,sin(theta)*.83)*radius)
		var alpha := smoothstep(0.0,.22,age)*(1.0-smoothstep(.8,1.65,age))
		ribbon(path,1.7,PINK.lerp(PEARL,.5),alpha*.70)

func draw_perimeter() -> void:
	var envelope := smoothstep(4.05,5.15,seconds)*perimeter_gain
	# One continuous ellipse goes beyond the viewport; visible particles share its arcs.
	for i in range(80):
		var lane := float(i%3)
		var angle := rnd(i,44)*TAU+seconds*(.035+rnd(i,45)*.024)
		var radius := 1.10+lane*.056+.025*sin(angle*2.0-seconds*.14+lane*.35)+.009*sin(angle*5.0+seconds*.23+lane*.8)
		var point := world*.5+Vector2(cos(angle),sin(angle))*world*.5*radius
		if point.x < -40.0 or point.x > world.x+40.0 or point.y < -40.0 or point.y > world.y+40.0: continue
		var alpha := envelope*(.32+.26*pow(maxf(0.0,sin(seconds*.7+i)),2.0))
		var hue := AURA_BLUE.lerp(AURA_PEARL,.38+rnd(i,47)*.32)
		if i%11==0:
			ornament(4+i%4,point,27.0+rnd(i,48)*18.0,angle+PI*.5+sin(seconds*.3+i)*.2,.6+sin(seconds*.45+i)*.25,hue,alpha)
		else: glow(point,2.1+rnd(i,48)*1.8,hue,alpha)

func memory_duration(kind: int) -> float:
	return 2.65 if kind==3 else 2.15

func draw_memory(event: Dictionary) -> void:
	var age := seconds-float(event.time)
	var kind := int(event.kind)
	var duration := memory_duration(kind)
	if age<0.0 or age>duration: return
	var origin: Vector2 = event.origin*world
	var fade := smoothstep(0.0,.14,age)*(1.0-smoothstep(duration-.65,duration,age))
	if kind==1:
		for i in range(6):
			var angle := float(i)*TAU/6.0+age*.3
			var point := origin+Vector2(cos(angle),sin(angle))*(46.0+sin(minf(age,1.4)*1.2)*13.0)
			ornament(4+i%4,point,63.0,angle,.8,PINK.lerp(RED,float(i)/10.0),fade*.95)
		glow(origin,24.0,PEARL,fade*.72)
		memory_flourish(origin,age,fade,72.0)
	elif kind==2:
		var destination := perimeter(fposmod(float(event.count)*.381966,1.0),40.0)
		var bend := (origin+destination)*.5+Vector2(-100,-160)
		var path := PackedVector2Array()
		var head := clampf(age/1.10,0.0,1.0)
		for j in range(32):
			var u := maxf(0.0,head-float(j)*.014)
			path.append(bezier(origin,bend,destination,smoothstep(0.0,1.0,u)))
		ribbon(path,3.6,PEARL,fade*.8)
		for i in range(17):
			var u := clampf((age-float(i)*.025)/1.10,0.0,1.0)
			var point := bezier(origin,bend,destination,smoothstep(0.0,1.0,u))
			point += Vector2(sin(i*2.4+age*3.0),cos(i*1.8+age*2.0))*sin(u*PI)*18.0
			ornament(4+i%4,point,34.0+rnd(i,50)*27.0,age*1.3+i,.65,PINK.lerp(PEARL,rnd(i,51)),fade*.78)
		var arrive := smoothstep(1.03,1.27,age)
		glow(destination,28.0,PEARL,fade*arrive*.85)
		memory_flourish(destination,maxf(0.0,age-1.03),fade*arrive,90.0)
	else:
		var center := Vector2(world.x*.5,1080*.49)
		for i in range(13):
			var u := smoothstep(0.0,1.13,age)
			var start := perimeter(float(i)/13.0,25.0)
			var destination := center+Vector2(cos(i*2.399963),sin(i*2.399963))*(30.0+sqrt(float(i))*20.0)
			var point := bezier(start,(start+center)*.5+Vector2(sin(i)*100,-80),destination,u)
			point=point.lerp(origin,smoothstep(1.75,2.60,age))
			ornament(i%4,point,170.0-float(i%3)*17.0,sin(age+i)*.2,.88,PINK.lerp(PEARL,.5),fade*.85)
		var bloom := smoothstep(.70,1.13,age)*(1.0-smoothstep(1.75,2.30,age))
		glow(center,62.0,PEARL,fade*bloom*.9)
		memory_flourish(center,maxf(0.0,age-.70),fade*bloom,190.0)

func memory_flourish(center: Vector2, age: float, alpha: float, radius: float) -> void:
	# Two incomplete sweeping strokes and scattered glints, rather than a static stamped ring.
	for arm in range(2):
		var path := PackedVector2Array()
		for j in range(26):
			var u := float(j)/25.0
			var angle := float(arm)*PI+age*.72+u*2.15
			var r := radius*(.75+.25*sin(u*PI))*(.55+.45*smoothstep(0.0,.45,age))
			path.append(center+Vector2(cos(angle),sin(angle)*.70)*r)
		ribbon(path,2.0,PEARL,alpha*.85)
	for i in range(18):
		var angle := float(i)*2.399963+age*.26
		var distance := radius*(.5+rnd(i,61)*.6)+age*(12.0+rnd(i,62)*18.0)
		var point := center+Vector2(cos(angle),sin(angle)*.72)*distance
		glow(point,2.0+rnd(i,63)*2.8,PINK.lerp(PEARL,rnd(i,64)),alpha*.9)

func perimeter(u: float, inset: float) -> Vector2:
	var length := (world.x+world.y-4.0*inset)*2.0
	var distance := u*length
	var width := world.x-2.0*inset
	var height := world.y-2.0*inset
	if distance<width: return Vector2(inset+distance,inset+sin(u*TAU*3.0+seconds*.3)*5.0)
	distance-=width
	if distance<height: return Vector2(world.x-inset,inset+distance)
	distance-=height
	if distance<width: return Vector2(world.x-inset-distance,world.y-inset)
	return Vector2(inset,world.y-inset-(distance-width))

func bezier(a: Vector2,b: Vector2,c: Vector2,u: float) -> Vector2:
	return a*(1.0-u)*(1.0-u)+b*2.0*(1.0-u)*u+c*u*u

func ornament(index: int, point: Vector2, height: float, angle: float, flip: float, hue: Color, alpha: float, reveal: float=1.0) -> void:
	if alpha<=.003 or reveal<=.001: return
	var region: Rect2 = atlas_regions[index]
	var cell := region.size
	var size := Vector2(height*cell.x/cell.y,height)
	var hidden := 1.0-clampf(reveal,0.0,1.0)
	var dest := Rect2(-size*.5+Vector2(0,size.y*hidden),Vector2(size.x,size.y*(1.0-hidden)))
	region.position.y += cell.y*hidden
	region.size.y *= 1.0-hidden
	draw_set_transform(point,angle,Vector2(flip,1.0))
	draw_texture_rect_region(ATLAS,dest,region,Color(hue,alpha))
	draw_set_transform(Vector2.ZERO)

func glow(point: Vector2, radius: float, hue: Color, alpha: float) -> void:
	if alpha<=.003: return
	draw_circle(point,radius*3.6,Color(hue,alpha*.055))
	draw_circle(point,radius*1.8,Color(hue,alpha*.15))
	draw_circle(point,radius,Color(hue.lerp(PEARL,.55),alpha*.72))

func ribbon(path: PackedVector2Array,width: float,hue: Color,alpha: float) -> void:
	if path.size()<3 or alpha<=.002: return
	# All three glow layers follow the same spine. Compute tangents and taper once,
	# retaining the same polygon winding and positions as the original layered ribbon.
	var spine := PackedVector2Array()
	var normals := PackedVector2Array()
	var taper := PackedFloat64Array()
	for i in range(path.size()):
		var tangent := path[mini(i+1,path.size()-1)]-path[maxi(0,i-1)]
		if tangent.length_squared()<.001: continue
		var u := float(i)/float(path.size()-1)
		spine.append(path[i])
		normals.append(Vector2(-tangent.y,tangent.x).normalized())
		taper.append(pow(maxf(.005,sin(u*PI)),.7))
	var count := spine.size()
	if count<3: return
	for layer in range(3):
		var points := PackedVector2Array()
		points.resize(count*2)
		for i in range(count):
			var half_width := width*float(RIBBON_WIDTHS[layer])*taper[i]
			points[i] = spine[i]+normals[i]*half_width
			points[count*2-1-i] = spine[i]-normals[i]*half_width
		# Sharp spine turns can make the two rails self-intersect; triangulation then fails and
		# Godot logs "Invalid polygon data" on every redraw (the failed draw is a no-op anyway).
		# Pre-check here so the failure stays silent — visual output is unchanged.
		if Geometry2D.triangulate_polygon(points).is_empty():
			continue
		var opacity: float = alpha*float(RIBBON_OPACITY[layer])
		draw_colored_polygon(points,Color(hue,float(opacity)))
