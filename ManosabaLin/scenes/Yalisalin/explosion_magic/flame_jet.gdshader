shader_type canvas_item;
render_mode unshaded, blend_add;
uniform sampler2D flow_noise : repeat_enable, filter_linear;
uniform float clock=0.0;
uniform float opacity=1.0;
uniform float seed=0.0;
void fragment() {
	vec2 flow=texture(flow_noise,vec2(UV.x*2.7-clock*2.0,UV.y*1.2+seed)).rg;
	float n=texture(flow_noise,UV*vec2(4.5,2.0)+flow*.6-vec2(clock*3.2,clock*.3)).r;
	float wave=sin(UV.x*10.0-clock*7.0)*.07;
	float y=abs(UV.y-.5-wave+(flow.g-.5)*.18)*2.0;
	float profile=1.0-smoothstep(.15,.88,y+(.5-n)*.35);
	float taper=smoothstep(0.0,.38,UV.x);
	float wisps=smoothstep(.16,.45,n+.32*UV.x);
	float alpha=profile*taper*wisps*opacity;
	vec3 col=mix(vec3(.35,.22,1.3),vec3(1.5,.32,.12),smoothstep(.2,.9,UV.x));
	col=mix(col,vec3(2.0,1.5,.85),pow(profile,4.0)*n);
	COLOR=vec4(col,clamp(alpha,0.0,1.0));
}
