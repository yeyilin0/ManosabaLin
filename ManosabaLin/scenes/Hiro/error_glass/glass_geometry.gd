extends Node2D
## Batched renderer for the Error glass fracture overlay.
##
## Performance note: the previous version issued ~2.9k draw calls per frame
## (1868 vein polylines + 652 edge strokes + 326 cone triangles + 45 chips),
## which collapsed the frame rate and stalled multiplayer sync while the
## overlay was retained on screen. This version keeps the identical geometry,
## timing and color math, but batches the submission:
##   glass glow layers 1-2 -> width-bucketed draw_multiline (32 calls max)
##   bright layers 3-4     -> kept as per-edge antialiased draw_polyline
##   veins                 -> 8 alpha-step batches (8 calls max)
##   cones                 -> one canvas_item_add_triangle_array
##   cells (burst)         -> one triangle array + one outline batch
##   chips / sparks        -> one merged draw_multiline each
##
## Note: bucket accumulation uses plain Arrays of Vector2 (reference semantics)
## and is converted to PackedVector2Array only at draw time. Packed arrays
## stored inside Arrays cannot be appended through index expressions (COW copy).
const DATA_PATH := "res://ManosabaLin/scenes/Hiro/error_glass/fracture.json"
const ROSE := Color("cc6666")
const BUCKET_COUNT := 16
const VEIN_STEPS := 8

# Glow layers batched via draw_multiline (width bucket * factor, alpha * light).
const BUCKETED_FACTORS := [22.0, 7.0]
const BUCKETED_ALPHAS := [0.065, 0.18]
# Bright crack layers kept as per-edge antialiased polylines (visual fidelity).
const AA_FACTORS := [2.8, 1.0]
const AA_LINE_COLOR := Color(0.95, 0.91, 0.92)

const VEIN_COLOR := Color(0.85, 0.84, 0.9)

var clock := 0.0
var bursting := false
var cut := PackedVector2Array()
var cut_times: Array = []
var edges: Array = []
var cells: Array = []

# Precomputed static batches.
var _edge_flat: Array = []          # per edge: segment point pairs (2 points per segment)
var _edge_bucket: PackedInt32Array = PackedInt32Array()
var _vein_flat: Array = []          # per edge: all vein segments flattened to point pairs
var _bucket_width: PackedFloat32Array = PackedFloat32Array()

# Per-frame reusable buffers (plain Arrays for buckets -> reference semantics).
var _layer_buckets: Array = []      # [bucket layer][bucket] -> Array of Vector2 pairs
var _vein_batches: Array = []       # [alpha step] -> Array of Vector2 pairs
var _aa_items: Array = []           # per frame: [PackedVector2Array points, float width]
var _chip_pairs := PackedVector2Array()
var _spark_pairs := PackedVector2Array()
var _cone_points := PackedVector2Array()
var _cone_colors := PackedColorArray()
var _cone_indices := PackedInt32Array()
var _cell_points := PackedVector2Array()
var _cell_colors := PackedColorArray()
var _cell_indices := PackedInt32Array()
var _cell_outline := PackedVector2Array()

func _ready() -> void:
	var data: Dictionary=JSON.parse_string(FileAccess.get_file_as_string(DATA_PATH))
	cut=_points(data["cut"])
	cut_times=data["cut_times"]
	var min_w := INF
	var max_w := -INF
	for item in data["edges"]:
		var veins: Array[PackedVector2Array]=[]
		for vein in item["veins"]:
			veins.append(_points(vein))
		var edge := {"points":_points(item["points"]),"veins":veins,
			"distance":float(item["distance"]),"local":bool(item["local"]),"width":float(item["width"]),"seed":float(item["seed"])}
		edges.append(edge)
		min_w = minf(min_w, float(item["width"]))
		max_w = maxf(max_w, float(item["width"]))
	for cell in data["cells"]:
		cells.append({"points":_points(cell["points"]),"center":Vector2(cell["center"][0],cell["center"][1]),"spin":float(cell["spin"])})
	_precompute_batches(min_w, max_w)

func _precompute_batches(min_w: float, max_w: float) -> void:
	var span := maxf(max_w - min_w, 0.0001)
	var bucket_sum := PackedFloat32Array()
	var bucket_n := PackedInt32Array()
	bucket_sum.resize(BUCKET_COUNT)
	bucket_n.resize(BUCKET_COUNT)
	for i in range(edges.size()):
		var w: float=edges[i]["width"]
		var bi := clampi(int((w - min_w) / span * float(BUCKET_COUNT)), 0, BUCKET_COUNT - 1)
		_edge_bucket.append(bi)
		bucket_sum[bi] += w
		bucket_n[bi] += 1
		_edge_flat.append(_expand(edges[i]["points"]))
		var vf := PackedVector2Array()
		for vein in edges[i]["veins"]:
			var seg := _expand(vein)
			if seg.size() >= 2:
				vf.append_array(seg)
		_vein_flat.append(vf)
	_bucket_width.resize(BUCKET_COUNT)
	for b in range(BUCKET_COUNT):
		if bucket_n[b] > 0:
			_bucket_width[b] = bucket_sum[b] / float(bucket_n[b])
		else:
			_bucket_width[b] = min_w + (float(b) + 0.5) / float(BUCKET_COUNT) * span
	for li in range(BUCKETED_FACTORS.size()):
		var layer: Array = []
		layer.resize(BUCKET_COUNT)
		for b in range(BUCKET_COUNT):
			layer[b] = []
		_layer_buckets.append(layer)
	for s in range(VEIN_STEPS):
		_vein_batches.append([])

func _points(values: Array) -> PackedVector2Array:
	var points := PackedVector2Array()
	for value in values:
		points.append(Vector2(float(value[0]),float(value[1])))
	return points

func _expand(points: PackedVector2Array) -> PackedVector2Array:
	var out := PackedVector2Array()
	for i in range(points.size() - 1):
		out.push_back(points[i])
		out.push_back(points[i + 1])
	return out

func _prefix(points: PackedVector2Array, progress: float) -> PackedVector2Array:
	if progress>=1.0:
		return points
	var result := PackedVector2Array()
	var position := clampf(progress,0.0,1.0)*float(points.size()-1)
	var last := mini(floori(position),points.size()-2)
	for i in range(last+1):
		result.append(points[i])
	result.append(points[last].lerp(points[last+1],position-float(last)))
	return result

func _draw() -> void:
	if cut.size()<2:
		return
	var scratch := PackedVector2Array()
	for i in range(cut.size()-1):
		if clock>=float(cut_times[i+1]):
			scratch.append(cut[i])
		else:
			scratch.append(cut[i])
			var u := inverse_lerp(float(cut_times[i]),float(cut_times[i+1]),clock)
			scratch.append(cut[i].lerp(cut[i+1],clampf(u,0.0,1.0)))
			break
	if clock>=2.0:
		scratch=cut
	if scratch.size()>1:
		draw_polyline(scratch,Color(.025,.025,.035,.95),9.0,true)
		draw_polyline(scratch,Color(.8,.72,.74,.8),2.1,true)
		# Broken bevels and short transverse chips express a resisting, scratched surface.
		_chip_pairs.clear()
		for i in range(1,scratch.size()-1):
			var p := scratch[i]
			_chip_pairs.push_back(p+Vector2(1,-3))
			_chip_pairs.push_back(p+Vector2(5+float(i%4),2))
		if _chip_pairs.size()>1:
			draw_multiline(_chip_pairs,Color(.9,.84,.85,.7),.8)
		if clock<2.0:
			var tip := scratch[-1]
			_spark_pairs.clear()
			for i in range(7):
				var direction := Vector2(cos(i*2.4+clock*8),sin(i*2.4+clock*8))
				_spark_pairs.push_back(tip+direction*5)
				_spark_pairs.push_back(tip+direction*(10+i*2))
			if _spark_pairs.size()>1:
				draw_multiline(_spark_pairs,Color(.85,.8,.78,.5),.8)
	if clock<2.0:
		return
	var light := smoothstep(2.0,2.12,clock)*(1.0-smoothstep(4.0,4.4,clock) if bursting else 1.0)
	# --- clear per-frame batches ---
	for li in range(BUCKETED_FACTORS.size()):
		var layer: Array = _layer_buckets[li]
		for b in range(BUCKET_COUNT):
			layer[b].clear()
	for s in range(VEIN_STEPS):
		_vein_batches[s].clear()
	_aa_items.clear()
	_cone_points.clear()
	_cone_colors.clear()
	_cone_indices.clear()
	# --- collect the fracture network ---
	for i in range(edges.size()):
		var edge = edges[i]
		var distance: float=edge["distance"]
		# Unequal stress pockets break simultaneously, with irregular off-axis boundaries.
		var onset := 3.0+clampf(distance/1300.0,0.0,.65)+float(edge["seed"])*.1
		var reveal := 1.0 if edge["local"] else smoothstep(onset,onset+.20,clock)
		if reveal<=0.0:
			continue
		var points: PackedVector2Array = edge["points"] if reveal>=1.0 else _prefix(edge["points"],reveal)
		if points.size()>=2:
			var bi: int = _edge_bucket[i]
			var seg: PackedVector2Array = _edge_flat[i] if reveal>=1.0 else _expand(points)
			if seg.size()>=2:
				for li in range(BUCKETED_FACTORS.size()):
					var layer: Array = _layer_buckets[li]
					layer[bi].append_array(seg)
			_aa_items.append([points, float(edge["width"])])
		var veins: PackedVector2Array = _vein_flat[i]
		if veins.size()>=2:
			var step := clampi(ceili(reveal*float(VEIN_STEPS)),1,VEIN_STEPS)
			_vein_batches[step-1].append_array(veins)
		# Narrow cones project light away from cracked bevels, on both sides of the glass.
		if points.size()>2 and light>0.0:
			var p := points[floori(float(points.size())*.5)]
			var normal := (points[-1]-points[0]).normalized().orthogonal()
			var reach := 45.0+float(edge["seed"])*160.0
			var col := Color(ROSE,.075*light)
			for side in [-1.0,1.0]:
				var base := _cone_points.size()
				_cone_points.push_back(p-Vector2(3,1))
				_cone_points.push_back(p+Vector2(3,1))
				_cone_points.push_back(p+normal*reach*side)
				_cone_colors.push_back(col)
				_cone_colors.push_back(col)
				_cone_colors.push_back(col)
				_cone_indices.push_back(base)
				_cone_indices.push_back(base+1)
				_cone_indices.push_back(base+2)
	# --- flush glow layers (batched) ---
	if light>0.0:
		for li in range(BUCKETED_FACTORS.size()):
			var layer: Array = _layer_buckets[li]
			var color := Color(ROSE,float(BUCKETED_ALPHAS[li])*light)
			var factor: float = BUCKETED_FACTORS[li]
			for b in range(BUCKET_COUNT):
				var bucket: Array = layer[b]
				if bucket.size()>1:
					draw_multiline(PackedVector2Array(bucket),color,factor*_bucket_width[b])
	# --- bright crack layers (kept antialiased per edge) ---
	for item in _aa_items:
		var pts: PackedVector2Array = item[0]
		var width: float = item[1]
		if light>0.0:
			draw_polyline(pts,Color(ROSE,.72*light),width*AA_FACTORS[0],true)
		draw_polyline(pts,Color(AA_LINE_COLOR,.85),width*AA_FACTORS[1],true)
	# --- veins (alpha-step batches) ---
	for s in range(VEIN_STEPS):
		var batch: Array = _vein_batches[s]
		if batch.size()>1:
			draw_multiline(PackedVector2Array(batch),Color(VEIN_COLOR,(float(s+1)/float(VEIN_STEPS))*.65),.6)
	# --- cones (single triangle array) ---
	if _cone_points.size()>=3:
		RenderingServer.canvas_item_add_triangle_array(get_canvas_item(),_cone_indices,_cone_points,_cone_colors)
	# --- burst shards ---
	if bursting and clock>=4.0:
		var age := clock-4.0
		if age<1.0:
			var facet := maxf(0.0,1.0-age)*.23
			var line_alpha := maxf(0.0,1.0-age)
			_cell_points.clear()
			_cell_colors.clear()
			_cell_indices.clear()
			_cell_outline.clear()
			for cell in cells:
				var center: Vector2=cell["center"]
				var direction := (center-Vector2(960,540)).normalized()
				var offset := direction*age*age*1600.0
				var rot := age*float(cell["spin"])
				var base := _cell_points.size()
				var local := PackedVector2Array()
				var cell_pts: PackedVector2Array = cell["points"]
				for p in cell_pts:
					var q := center+(p-center).rotated(rot)+offset
					local.push_back(q)
					_cell_points.push_back(q)
					_cell_colors.push_back(Color(ROSE,facet))
				for k in range(1,local.size()-1):
					_cell_indices.push_back(base)
					_cell_indices.push_back(base+k)
					_cell_indices.push_back(base+k+1)
				for k in range(local.size()):
					_cell_outline.push_back(local[k])
					_cell_outline.push_back(local[(k+1)%local.size()])
			if _cell_points.size()>=3:
				RenderingServer.canvas_item_add_triangle_array(get_canvas_item(),_cell_indices,_cell_points,_cell_colors)
			if _cell_outline.size()>1:
				draw_multiline(_cell_outline,Color(1.0,.87,.9,line_alpha),1.2)
