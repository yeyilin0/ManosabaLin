shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float reveal : hint_range(0.0, 1.0) = 0.0;
uniform float spin = 0.0;
uniform float strength = 1.0;
void fragment() {
	vec2 p = UV - 0.5;
	float c = cos(spin), s = sin(spin);
	vec2 uv = mat2(vec2(c,s),vec2(-s,c)) * p + 0.5;
	float a = texture(TEXTURE,uv).a;
	vec2 px = TEXTURE_PIXEL_SIZE * 2.5;
	float halo = texture(TEXTURE,uv+vec2(px.x,0)).a + texture(TEXTURE,uv-vec2(px.x,0)).a;
	halo += texture(TEXTURE,uv+vec2(0,px.y)).a + texture(TEXTURE,uv-vec2(0,px.y)).a;
	float angle = fract(atan(p.y,p.x)/6.2831853 + 0.5);
	float mask = 1.0-smoothstep(reveal, reveal+0.025,angle);
	vec3 col = vec3(1.0,0.43,0.065)*a + vec3(0.8,0.14,0.01)*halo*0.32;
	COLOR = vec4(col*strength, clamp((a+halo*0.24)*mask,0.0,1.0));
}
