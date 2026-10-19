extends Node2D
var clock := 0.0
var source := Vector2(202,583)
var target := Vector2(1478,583)
var core_position := Vector2(202,583)
var core_radius := 10.0
const STAR_COLORS := [Color(.35,.85,1.0),Color(1.0,.4,.82),Color(.62,1.0,.45),Color(1.0,.86,.3),Color(.7,.5,1.0),Color(.9,1.0,1.0)]

func _star(at: Vector2, radius: float, color: Color, angle: float) -> void:
	var points := PackedVector2Array()
	for j in range(8):
		var a := angle+float(j)*PI*.25
		var length := radius*(1.0 if j%2==0 else .17)
		points.append(at+Vector2(cos(a),sin(a))*length)
	draw_circle(at,radius*1.85,Color(color,color.a*.15),true,-1.0,true)
	draw_colored_polygon(points,color)
	draw_circle(at,radius*.12,Color(1.0,1.0,1.0,color.a),true,-1.0,true)

func _stroke(points: PackedVector2Array, color: Color, width: float) -> void:
	draw_polyline(points,Color(color,color.a*.12),width*4.0,true)
	draw_polyline(points,Color(color,color.a*.38),width*2.0,true)
	draw_polyline(points,color,width,true)

func _draw() -> void:
	if clock>=2.0 and clock<4.08:
		var charge := smoothstep(2.0,3.75,clock)
		var alpha := smoothstep(2.0,2.3,clock)*(1.0-smoothstep(4.0,4.08,clock))
		# Open, wandering filaments: no rigid circles around the deforming fluid mass.
		for i in range(3):
			var points := PackedVector2Array()
			for j in range(31):
				var angle := float(j)/30.0*PI*1.1+clock*.7+i*2.2
				var radius := core_radius*(.72+.18*sin(angle*3.0-clock*2.3+i))
				points.append(core_position+Vector2(cos(angle),sin(angle)*.7)*radius)
			_stroke(points,Color(.5+float(i)*.1,.35,1.0,alpha*.25),1.3)
		for i in range(190):
			var phase := float(i)*2.39996
			var progress := fposmod(float(i)*.137+(clock-2.0)*(.72+float(i%5)*.045),1.0)
			var radius := (1.0-progress)*(1000.0+float(i%7)*90.0)
			var angle := phase+(1.0-progress)*.8+clock*.16
			var p := source+Vector2(cos(angle),sin(angle)*.82)*radius
			var a := alpha*smoothstep(0.0,.12,progress)*(1.0-smoothstep(.92,1.0,progress))*(.72+charge*.28)
			var col: Color=STAR_COLORS[i%STAR_COLORS.size()]
			_star(p,6.0+float(i%5)*1.9,Color(col,a),phase+clock*.22)
	if clock<4.08:
		return
	var t := clock-4.08
	var end_fade := 1.0-smoothstep(5.2,7.0,clock)
	# Broad expanding pressure ellipses, visible traversing the playable screen.
	for i in range(4):
		var age := t-.15-float(i)*.17
		if age<0.0 or age>1.6:
			continue
		var radius := 60.0+age*1800.0
		var points := PackedVector2Array()
		for j in range(181):
			var angle := float(j)/180.0*TAU
			points.append(target+Vector2(cos(angle)*radius,sin(angle)*radius*(.50+float(i)*.1)))
		_stroke(points,Color(1.0,.90,.7,(1.0-age/1.6)*.95),maxf(1.0,7.0-age*3.0))
	# A second train begins after the fire cloud, spreading visibly through the screen.
	for i in range(7):
		var age := t-.78-float(i)*.15
		if age<0.0 or age>1.8:
			continue
		var points := PackedVector2Array()
		var radius := 90.0+age*1300.0
		for j in range(121):
			var angle := float(j)/120.0*TAU
			var ripple := 1.0+.015*sin(angle*11.0-age*7.0+i)
			points.append(target+Vector2(cos(angle),sin(angle)*(.7+float(i%3)*.12))*radius*ripple)
		_stroke(points,Color(1.0,.65,.3,(1.0-age/1.8)*end_fade*.7),3.5)
	if t<.65:
		for i in range(70):
			var angle := float(i)*2.39996
			var direction := Vector2(cos(angle),sin(angle))
			var length := t*2600.0*(.55+.45*sin(i*13.7)*sin(i*13.7))
			_stroke(PackedVector2Array([target+direction*length*.55,target+direction*length]),Color(1.0,.7,.28,(.65-t)*1.3),3.0)
	# Radial ballistic embers scatter from the fire cloud and cross the full viewport.
	for i in range(220):
		var birth := .12+float(i%13)*.032
		var age := t-birth
		if age<0.0:
			continue
		var angle := float(i)*2.39996
		var speed := 350.0+fposmod(float(i)*137.3,950.0)
		var dir := Vector2(cos(angle),sin(angle)*.85)
		var p := target+dir*speed*age*.74+Vector2(0,-age*age*24.0)
		var trail := dir*(14.0+speed*.027)
		var alpha := end_fade*clampf(1.0-age/3.0,0.0,1.0)
		_stroke(PackedVector2Array([p-trail,p]),Color(1.0,.27+float(i%4)*.14,.04,alpha),2.0+float(i%3))
		if i%4==0:
			draw_circle(p,3.0,Color(1.0,.93,.6,alpha),true,-1.0,true)

