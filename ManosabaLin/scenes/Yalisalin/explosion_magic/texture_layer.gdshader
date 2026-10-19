shader_type canvas_item;
render_mode unshaded, blend_add;
uniform sampler2D flow_noise : repeat_enable, filter_linear;
uniform float clock = 0.0;
uniform float opacity = 1.0;
uniform float dissolve = 0.0;
uniform float seed = 0.0;
uniform bool fire = true;
uniform vec4 tint : source_color = vec4(1.0);
void fragment() {
	vec2 drift = vec2(clock*.035, -clock*.09) + seed;
	vec2 flow = texture(flow_noise, UV*1.6+drift).rg - .5;
	vec2 uv = UV + flow*.055;
	vec4 tex = texture(TEXTURE,uv);
	float density = dot(tex.rgb,vec3(.299,.587,.114))*tex.a;
	float n = texture(flow_noise,UV*2.1-drift*.65).r;
	float erosion = smoothstep(dissolve-.09,dissolve+.09,n);
	float alpha = density*opacity*erosion;
	vec3 col=tint.rgb;
	if(fire) {
		float heat=clamp(density*1.65 + .17*n,0.0,1.0);
		col=mix(vec3(.8,.055,.015),vec3(1.7,.47,.035),smoothstep(.04,.44,heat));
		col=mix(col,vec3(2.1,1.5,.55),smoothstep(.42,.77,heat));
		col=mix(col,vec3(2.5,2.2,1.6),smoothstep(.76,.96,heat));
		col*=tint.rgb;
	}
	COLOR=vec4(col,clamp(alpha*tint.a,0.0,1.0));
}
