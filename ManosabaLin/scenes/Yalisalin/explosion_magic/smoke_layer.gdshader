shader_type canvas_item;
render_mode unshaded;
uniform sampler2D flow_noise : repeat_enable, filter_linear;
uniform float clock=0.0;
uniform float opacity=1.0;
uniform float dissolve=0.0;
uniform float seed=0.0;
void fragment() {
	vec2 drift=vec2(clock*.022,-clock*.065)+seed;
	vec2 flow=texture(flow_noise,UV*1.7+drift).rg-.5;
	vec4 tex=texture(TEXTURE,UV+flow*.08);
	float density=dot(tex.rgb,vec3(.299,.587,.114))*tex.a;
	float n=texture(flow_noise,UV*2.3-drift).r;
	float alpha=density*opacity*smoothstep(dissolve-.13,dissolve+.1,n);
	vec3 color=mix(vec3(.035,.015,.028),vec3(.23,.07,.035),density);
	COLOR=vec4(color,clamp(alpha,0.0,.92));
}
