shader_type canvas_item;
render_mode unshaded, blend_add;
uniform sampler2D flow_noise : repeat_enable, filter_linear;
uniform float clock=0.0;
uniform float opacity=1.0;
uniform float launch=0.0;
uniform float seed=0.0;
uniform vec4 tint : source_color=vec4(1.0);
void fragment() {
	vec2 p=(UV-.5)*2.0;
	vec2 flow=texture(flow_noise,UV*1.8+vec2(-clock*.23,clock*.17)+seed).rg;
	float n=texture(flow_noise,UV*3.1+flow*.6+vec2(-clock*.51,-clock*.24)).r;
	float angle=atan(p.y,p.x);
	float edge=.12*sin(angle*3.0-clock*2.4)+.09*sin(angle*5.0+clock*3.1+seed);
	float radius=length(p+(flow-.5)*.22)-edge-(n-.5)*.23;
	float mask=1.0-smoothstep(.60,.98,radius);
	// Irregular tongues are advected backwards from the advancing, burning head.
	float fire_noise=texture(flow_noise,vec2(UV.x*3.0+clock*1.8,UV.y*2.7)+flow*.45).g;
	float tongues=1.0-smoothstep(.22,.94,abs(p.y)+(fire_noise-.5)*.55);
	mask=mix(mask,mask*tongues,launch*.7);
	float heat=clamp((1.0-radius)*.8+n*.4,0.0,1.0);
	vec3 magic=mix(vec3(.25,.15,1.0),vec3(.45,1.1,1.6),heat);
	magic=mix(magic,vec3(1.9,1.9,2.2),smoothstep(.48,.9,heat));
	vec3 fire=mix(vec3(1.2,.12,.35),vec3(2.0,.8,.15),heat);
	fire=mix(fire,vec3(2.4,2.0,1.4),smoothstep(.58,.91,heat));
	COLOR=vec4(mix(magic,fire,launch*.75)*tint.rgb,mask*opacity*tint.a);
}
