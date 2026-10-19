extends CanvasLayer
signal impact
signal prepared
signal finished
const IMPACT_TIME := 2.0
const PINK_END := 6.15
const REDRAW_MIN_INTERVAL := 1.0 / 30.0
const PAINT := preload("res://ManosabaLin/scenes/Hiro/error_glass/glass_geometry.gd")
const SOUND := "res://ManosabaLin/audio/error_glass/"
const SOUNDS := [preload(SOUND+"knife_scrape.ogg"),preload(SOUND+"local_crack.ogg"),preload(SOUND+"spread.ogg"),preload(SOUND+"glass_burst.ogg")]
const CUES := [0.0,2.0,3.0]
@export var autoplay := true
@export var hold_fullscreen := false
@export var start_in_burst := false
@export var sfx_gain := 1.0
var clock := 0.0
var bursting := false
var _holding := false
var _impact_sent := false
var _prepared_sent := false
var _finished_sent := false
var _field_replaced := false
var _dim: ColorRect
var _flash: ColorRect
var _paint: Node2D
var _players: Array[AudioStreamPlayer] = []
var _cue_index := 0
var _redraw_accum := 999.0
var _last_view_size := Vector2.ZERO
var _size_changed := false

func _ready() -> void:
	_dim=_rect()
	_paint=Node2D.new()
	_paint.set_script(PAINT)
	add_child(_paint)
	_flash=_rect()
	for stream in SOUNDS:
		var player := AudioStreamPlayer.new()
		player.stream=stream
		add_child(player)
		_players.append(player)
	if start_in_burst:
		begin_burst()
	_render()

func _rect() -> ColorRect:
	var rect := ColorRect.new()
	rect.mouse_filter=Control.MOUSE_FILTER_IGNORE
	add_child(rect)
	return rect

# Visual state is applied every frame (cheap); the actual canvas redraw is
# demand-driven: throttled to 30 Hz while the presentation is moving, and once
# it reaches its static terminal state the redraw stops entirely. The previous
# unconditional per-frame redraw kept the ~2.9k-call fracture painter running
# for the entire retention window, which tanked the frame rate and stalled
# multiplayer sync (observed: a single qualifying Error play froze the host
# long enough to nearly disconnect the client).
func _process(delta: float) -> void:
	for player in _players:
		player.volume_linear=sfx_gain
	if not autoplay or _finished_sent:
		return
	if _holding:
		return
	var end := PINK_END if bursting else (4.0 if hold_fullscreen else 3.0)
	clock=minf(clock+delta,end)
	if not bursting:
		while _cue_index<CUES.size() and clock>=CUES[_cue_index]:
			if not _field_replaced and (_cue_index<2 or hold_fullscreen):
				_players[_cue_index].volume_linear=sfx_gain
				_players[_cue_index].play()
			_cue_index+=1
	if not _field_replaced:
		_update_props()
		_request_redraw(delta)
	if not _impact_sent and clock>=IMPACT_TIME:
		_impact_sent=true
		impact.emit()
	if not bursting and clock>=end:
		if hold_fullscreen:
			_holding=true
			if not _field_replaced:
				_render()
		if not _prepared_sent:
			_prepared_sent=true
			prepared.emit()
		if hold_fullscreen:
			return
	if clock>=end:
		_finished_sent=true
		finished.emit()
		queue_free()

## A newer retained field owns the screen. Continue only the opening's signal
## clock; C# disposes this presenter after prepared without cancelling the card.
func suppress_retained() -> void:
	_field_replaced=true
	visible=false
	for player in _players:
		player.stop()

## The End consumes the retained overlay; no automatic burst from playing Error.
func begin_burst() -> void:
	bursting=true
	_holding=false
	autoplay=true
	clock=4.0
	_cue_index=CUES.size()
	_players[3].volume_linear=sfx_gain
	_players[3].play()
	_render()

func seek_preview(seconds: float) -> void:
	autoplay=false
	clock=maxf(0.0,seconds)
	_render()

func _presentation_moving() -> bool:
	if bursting:
		# Shards are gone by age 1.0 (clock 5.0) and light/flash are settled;
		# a small margin keeps the final shard frames complete.
		return clock < 5.1
	if _holding:
		return false
	return clock < 4.0

func _request_redraw(delta: float) -> void:
	if _size_changed:
		_redraw_accum = 0.0
		_paint.queue_redraw()
		return
	if not _presentation_moving():
		return
	_redraw_accum += delta
	if _redraw_accum < REDRAW_MIN_INTERVAL:
		return
	_redraw_accum = 0.0
	_paint.queue_redraw()

func _render() -> void:
	_update_props()
	_redraw_accum = 0.0
	_paint.queue_redraw()

func _update_props() -> void:
	var size := get_viewport().get_visible_rect().size
	_size_changed = size != _last_view_size
	_last_view_size = size
	_dim.size=size
	_flash.size=size
	_dim.color=Color(.008,.01,.017,smoothstep(0.0,.4,clock)*(.08 if _holding else .18))
	_paint.scale=size/Vector2(1920,1080)
	_paint.set("clock",clock)
	_paint.set("bursting",bursting)
	var fade := 1.0-smoothstep(2.8,3.0,clock) if not hold_fullscreen and not bursting else 1.0
	_paint.modulate=Color(1,1,1,fade)
	# Rose explosion becomes luminous pink-white by 4.15, held until 6.15 (2 seconds).
	var pink := Color(1.0,.73,.91).lerp(Color(1.0,.93,.99),smoothstep(4.5,5.8,clock))
	var color := Color("cc6666").lerp(pink,smoothstep(4.08,4.15,clock))
	_flash.color=Color(color,smoothstep(4.0,4.045,clock) if bursting else 0.0)

func _exit_tree() -> void:
	for player in _players:
		if is_instance_valid(player):
			player.stop()
