extends Node2D
## Bounded analytic particles; all seeds/path geometry are made before card playback.
const BASE := "res://ManosabaLin/scenes/Emalin/bad_ending/"
const MOTE := preload(BASE + "mote.tres")
const FIRST_FIGURE := preload(BASE + "textures/figure_01.png")
const RED := Color(.80,.40,.40)
const GARNET := Color(.90,.20,.34)
const PINK := Color(1.0,.66,.79)
const PEARL := Color(1.0,.91,.95)
const AURA_CORE := Color(1.0,.66,.58)
@export var behind_figures := false
var show_perimeter := true
var perimeter_gain := 1.0
var seconds := 0.0
var world := Vector2(1920,1080)
var player := Vector2(.22,.55)
var last_viewport := Vector2.ZERO
var cell := Vector2(FIRST_FIGURE.get_size())
var figure_size := Vector2(950.4,950.4)
var seeds: Array[Vector4] = []
var gather_paths: Array[PackedVector2Array] = []
var events: Array[Dictionary] = []
var card_events: Array[Dictionary] = []

func rnd(index: int, salt: int) -> float:
	return fposmod(sin(float(index)*127.1+float(salt)*311.7)*43758.5453,1.0)

func _init() -> void:
	for i in range(192): seeds.append(Vector4(rnd(i,1),rnd(i,2),rnd(i,3),rnd(i,4)))
	# Curves approach from different heights and bend softly into the hand, not lightning.
	for i in range(12):
		var angle := float(i)*2.399963
		var start := Vector2(.5+cos(angle)*1.1,.5+sin(angle)*.85)
		var finish := Vector2(.60,.42)
		var control := Vector2(.48+sin(angle+1.0)*.52,.15+cos(angle)*.30)
		var path := PackedVector2Array()
		for j in range(49): path.append(bezier(start,control,finish,float(j)/48.0))
		gather_paths.append(path)

func render_at(time: float, viewport: Vector2, player_uv: Vector2) -> void:
	seconds = time
	player = player_uv
	if viewport != last_viewport:
		last_viewport = viewport
		var unit := maxf(viewport.y/1080.0,.0001)
		scale = Vector2.ONE*unit
		world = viewport/unit
		figure_size = cell*minf(world.x*.85/cell.x,1080*.88/cell.y)
	for i in range(events.size()-1,-1,-1):
		if seconds-float(events[i].time)>2.3: events.remove_at(i)
	for i in range(card_events.size()-1,-1,-1):
		if seconds-float(card_events[i].time)>2.3: card_events.remove_at(i)
	queue_redraw()

func add_support(kind: int, origin_uv: Vector2, count: int, time: float) -> void:
	if events.size()>=16: events.pop_front()
	events.append({"kind":kind,"origin":origin_uv,"count":count,"time":time})

func add_card(kind: int, origin_uv: Vector2, art: Texture2D, time: float) -> void:
	if card_events.size()>=4: card_events.pop_front()
	card_events.append({"kind":kind,"origin":origin_uv,"art":art,"time":time})

func reset_support() -> void:
	events.clear()
	card_events.clear()

func figure_point(uv: Vector2) -> Vector2:
	return world*Vector2(.5,.51)+(uv-Vector2(.5,.5))*figure_size

func bezier(a: Vector2,b: Vector2,c: Vector2,u: float) -> Vector2:
	return a*(1.0-u)*(1.0-u)+b*2.0*u*(1.0-u)+c*u*u

func _draw() -> void:
	if behind_figures:
		if seconds<5.95: draw_court_dust()
		return
	if seconds>.75 and seconds<2.18: draw_materialize()
	if seconds>1.15 and seconds<2.52: draw_sword_streams()
	if seconds>3.10 and seconds<5.90: draw_release()
	if seconds>4.15 and show_perimeter: draw_perimeter_dust()
	for event in events: draw_support(event)
	for event in card_events: draw_card_memory(event)

func glow(point: Vector2,radius: float,hue: Color,alpha: float) -> void:
	if alpha<=.001: return
	draw_texture_rect(MOTE,Rect2(point-Vector2.ONE*radius,Vector2.ONE*radius*2.0),false,Color(hue,alpha))

func thread(path: PackedVector2Array,width: float,hue: Color,alpha: float) -> void:
	if path.size()<2 or alpha<=.001: return
	draw_polyline(path,Color(hue,alpha*.055),width*7.0,true)
	draw_polyline(path,Color(hue,alpha*.15),width*3.0,true)
	draw_polyline(path,Color(hue.lerp(PEARL,.4),alpha*.76),maxf(.65,width),true)

func draw_court_dust() -> void:
	var envelope := smoothstep(.20,1.15,seconds)*(1.0-smoothstep(4.85,5.95,seconds))
	for i in range(70):
		var s: Vector4 = seeds[i]
		var x := s.x*world.x+sin(seconds*.22+s.z*TAU)*13.0
		var y := fposmod(s.y*1080.0-seconds*(5.0+s.z*7.0),1080.0)
		var alpha := envelope*(.08+s.w*.12)*(.6+.4*sin(seconds+s.x*8.0))
		glow(Vector2(x,y),2.0+s.z*4.5,PINK,alpha)

func draw_materialize() -> void:
	for i in range(104):
		var s: Vector4 = seeds[i]
		var age := seconds-(.77+s.w*.45)
		var u := clampf(age/.86,0.0,1.0)
		if u<=0.0 or u>=1.0: continue
		var side := i%2
		# Light streams end throughout the figure, not on one opaque circular mask.
		var target := figure_point(Vector2(.24+s.x*.33+side*.22,.15+s.y*.74))
		var a := s.z*TAU
		var start := target+Vector2(cos(a)*420.0,sin(a)*310.0)
		var bend := target+Vector2(sin(a+1.3)*260.0,cos(a)*110.0)
		var point := bezier(start,bend,target,u)
		var hue := PEARL if side==0 else GARNET.lerp(PINK,.30)
		var alpha := sin(u*PI)*.60
		glow(point,3.0+s.w*5.0,hue,alpha)
		if i%4==0:
			var path := PackedVector2Array()
			for j in range(10): path.append(bezier(start,bend,target,maxf(0.0,u-float(j)*.011)))
			thread(path,.8,hue,alpha*.50)

func draw_sword_streams() -> void:
	var envelope := smoothstep(1.15,1.43,seconds)*(1.0-smoothstep(2.15,2.48,seconds))
	for i in range(gather_paths.size()):
		var source: PackedVector2Array = gather_paths[i]
		var head := clampf((seconds-1.14-float(i%4)*.07)/.81,0.0,1.0)
		var path := PackedVector2Array()
		for j in range(21):
			var u := maxf(0.0,head-float(20-j)*.018)
			var index := minf(u*48.0,47.999)
			var a := int(index)
			var uv := source[a].lerp(source[a+1],index-a)
			path.append(figure_point(uv))
		thread(path,1.0+float(i%3)*.45,GARNET,envelope*.58)
		glow(path[-1],5.5,PINK,envelope*.55)
	var hilt := figure_point(Vector2(.60,.42))
	glow(hilt,35.0,GARNET,envelope*.35)
	glow(hilt,10.0,PEARL,envelope*.70)
	# Fine glints follow the crystal blade from guard to point during formation.
	for i in range(11):
		var u := float(i)/10.0
		var alpha := smoothstep(1.62+u*.32,1.76+u*.32,seconds)*envelope
		var point := figure_point(Vector2(.60,.42).lerp(Vector2(.43,.82),u))
		glow(point,2.0+sin(u*PI)*2.5,PEARL,alpha*.48)

func shard(point: Vector2,size: float,angle: float,flip: float,hue: Color,alpha: float) -> void:
	draw_set_transform(point,angle,Vector2(maxf(.10,absf(flip)),1.0))
	var poly := PackedVector2Array([Vector2(-size*.22,-size*.65),Vector2(size*.42,-size*.13),Vector2(size*.15,size*.56),Vector2(-size*.38,size*.20)])
	draw_colored_polygon(poly,Color(hue,alpha*.38))
	draw_line(poly[0],poly[1],Color(PEARL,alpha*.76),.8,true)
	draw_line(poly[1],poly[2],Color(hue,alpha*.70),.65,true)
	draw_set_transform(Vector2.ZERO)

func draw_release() -> void:
	var center := world*Vector2(.5,.59)
	for i in range(112):
		var s: Vector4 = seeds[i]
		var age := seconds-(3.13+s.z*.47)
		if age<=0.0 or age>2.10: continue
		var near := i%3==0
		var u := clampf(age/1.70,0.0,1.0)
		var angle := float(i)*2.399963+s.y*.35
		var radius := (1.0-pow(1.0-u,1.65))*(580.0+s.x*720.0)
		var curl := sin(age*2.2+s.w*TAU)*(.11+s.y*.17)
		var point := center+Vector2(cos(angle+curl)*radius,sin(angle+curl)*radius*.64)
		var alpha := smoothstep(0.0,.12,age)*(1.0-smoothstep(1.18,2.10,age))
		var hue := PINK.lerp(PEARL,s.x) if i%2==0 else RED.lerp(PEARL,s.x*.55)
		if near:
			shard(point,8.0+s.z*17.0,angle+age*.6,cos(age*2.2+s.y*TAU),hue,alpha*.58)
			glow(point,14.0,hue,alpha*.15)
		else:
			glow(point,2.5+s.z*5.0,hue,alpha*(.20+s.w*.35))
			if i%5==0:
				draw_line(point-Vector2(5,0),point+Vector2(5,0),Color(PEARL,alpha*.28),.7,true)
				draw_line(point-Vector2(0,5),point+Vector2(0,5),Color(PEARL,alpha*.28),.7,true)

func draw_perimeter_dust() -> void:
	var fade := smoothstep(4.15,5.4,seconds)*perimeter_gain
	for i in range(38):
		var s: Vector4 = seeds[i]
		var a := s.x*TAU+seconds*(.017+s.z*.016)
		var r := 1.09+s.y*.11+.018*sin(a*3.0+seconds*.2)
		var uv := Vector2(.5,.5)+Vector2(cos(a),sin(a))*.5*r
		if uv.x<-.02 or uv.x>1.02 or uv.y<-.02 or uv.y>1.02: continue
		var point := uv*world
		var alpha := fade*(.12+s.w*.21)*(.65+.35*sin(seconds*.8+s.z*6.0))
		glow(point,2.5+s.y*4.0,AURA_CORE if i%3==0 else RED,alpha)
		if i%6==0: shard(point,6.0+s.z*6.0,a,cos(seconds*.4+s.w*TAU),RED,alpha*.65)

func draw_support(event: Dictionary) -> void:
	var kind := int(event.kind)
	var age := seconds-float(event.time)
	var origin: Vector2 = event.origin
	var center := origin*world
	var alpha := smoothstep(0.0,.16,age)*(1.0-smoothstep(1.05,2.10,age))
	if alpha<=.001: return
	var hue := RED if kind==1 else PINK
	var radius := (34.0+smoothstep(0.0,.50,age)*24.0)*1.6
	if kind>=3: radius = (46.0+smoothstep(0.0,.6,age)*22.0)*1.6
	glow(center,radius*1.8,hue,alpha*.32)
	# A readable luminous crest holds before fading; the central halo stays translucent.
	# Pointed stained-glass lobes, asymmetrical bone-branch stems and a travelling rim.
	for lobe in range(6):
		var path := PackedVector2Array()
		var angle := float(lobe)*TAU/6.0-.15
		for j in range(13):
			var u := float(j)/12.0
			var a := angle+(u-.5)*.77
			var r := radius*(.64+sin(u*PI)*.39)
			path.append(center+Vector2(cos(a),sin(a))*r)
		thread(path,1.3,hue,alpha*.95)
		var tip := center+Vector2(cos(angle),sin(angle))*radius
		glow(tip,8.0,PEARL,alpha*.90)
		var glint := tip+Vector2(cos(angle),sin(angle))*7.0
		draw_line(tip,glint,Color(PEARL,alpha*.75),1.0,true)
	if kind==1 or kind==2:
		for i in range(3):
			var a := float(i)*2.1+age*.14
			var base := center+Vector2(cos(a),sin(a))*radius*.3
			var tip := center+Vector2(cos(a+.38),sin(a+.38))*radius*.86
			draw_line(base,tip,Color(PEARL,alpha*.72),1.9,true)
			var middle := base.lerp(tip,.60)
			draw_line(middle,middle+Vector2(cos(a-.65),sin(a-.65))*radius*.23,Color(hue,alpha*.85),1.5,true)
	if kind==3:
		var u := smoothstep(.32,1.85,age)
		var finish := player*world+Vector2(0,-25)
		var control := (center+finish)*.5+Vector2(0,-125)
		var path := PackedVector2Array()
		for j in range(20): path.append(bezier(center,control,finish,maxf(0.0,u-float(19-j)*.012)))
		thread(path,2.1,PINK,alpha*.92)
		glow(path[-1],12.0,PEARL,alpha*.95)

func draw_card_memory(event: Dictionary) -> void:
	var age := seconds-float(event.time)
	var art: Texture2D = event.art
	if not is_instance_valid(art): return
	var origin: Vector2 = event.origin
	var u := smoothstep(.32,1.85,age)
	var start := origin*world
	var finish := player*world+Vector2(0,-25)
	var point := bezier(start,(start+finish)*.5+Vector2(0,-160),finish,u)
	var alpha := smoothstep(0.0,.18,age)*(1.0-smoothstep(1.45,2.10,age))
	var height := lerpf(224.0,74.0,u)
	var art_size := Vector2(art.get_size())
	var size := Vector2(height*minf(art_size.x/maxf(art_size.y,1.0),.82),height)
	draw_set_transform(point,sin(u*PI)*-.14)
	draw_texture_rect(art,Rect2(-size*.5,size),false,Color(1.0,.88,.93,alpha*.92))
	draw_rect(Rect2(-size*.5,size),Color(PEARL,alpha*.90),false,2.1,true)
	draw_set_transform(Vector2.ZERO)
	glow(point,height*.65,PINK,alpha*.24)
