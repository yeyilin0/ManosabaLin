shader_type canvas_item;
render_mode unshaded, blend_add;
uniform sampler2D flow_noise : repeat_enable, filter_linear;
uniform float clock=0.0;
uniform float seed=0.0;
uniform float opacity=1.0;
uniform vec4 tint : source_color=vec4(.3,.65,1.0,1.0);
void fragment() {
	vec2 flow=texture(flow_noise,vec2(UV.x*2.5-clock*.85,UV.y*.55+seed)).rg;
	float noise=texture(flow_noise,vec2(UV.x*5.0-clock*1.5,UV.y*.9+seed)+flow*.5).r;
	float channel=UV.y-.5+(flow.g-.5)*.28;
	float cross_profile=pow(max(0.0,1.0-abs(channel)*2.0),1.25);
	float pulse=.65+.35*noise;
	// Advected caustic channels running along a transparent liquid sheet.
	float caustic=abs(channel+.07*sin(UV.x*8.0-clock*2.4+seed)+(noise-.5)*.08);
	float vein=exp(-caustic*caustic*460.0);
	float side_vein=exp(-pow(channel-.18-(flow.r-.5)*.12,2.0)*650.0);
	float end_fade=smoothstep(0.0,.08,UV.x);
	float alpha=cross_profile*(.88+noise*.12)*end_fade*opacity;
	vec3 col=mix(tint.rgb*1.35,vec3(1.1,1.35,1.55),clamp((vein*.62+side_vein*.28)*pulse,0.0,1.0));
	COLOR=vec4(col,clamp(alpha,0.0,1.0));
}
