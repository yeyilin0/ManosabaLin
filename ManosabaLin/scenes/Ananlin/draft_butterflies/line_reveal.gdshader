shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float reveal=0.0;
varying vec4 butterfly_tint;
void vertex() { butterfly_tint=COLOR; }
void fragment() {
	float along=abs(UV.x-.5)*1.55+abs(UV.y-.5)*.40;
	float mask=1.0-smoothstep(reveal,reveal+.035,along);
	float a=texture(TEXTURE,UV).a;
	vec2 px=TEXTURE_PIXEL_SIZE*2.0;
	float halo=texture(TEXTURE,UV+vec2(px.x,0)).a+texture(TEXTURE,UV-vec2(px.x,0)).a;
	halo+=texture(TEXTURE,UV+vec2(0,px.y)).a+texture(TEXTURE,UV-vec2(0,px.y)).a;
	vec2 wide=TEXTURE_PIXEL_SIZE*5.0;
	float aura=texture(TEXTURE,UV+vec2(wide.x,wide.y)).a+texture(TEXTURE,UV-vec2(wide.x,wide.y)).a;
	aura+=texture(TEXTURE,UV+vec2(wide.x,-wide.y)).a+texture(TEXTURE,UV+vec2(-wide.x,wide.y)).a;
	vec3 emission=butterfly_tint.rgb*1.85;
	COLOR=vec4(emission*(a+halo*.18+aura*.06)+vec3(.10)*a,butterfly_tint.a*mask*clamp(a+halo*.24+aura*.09,0.0,1.0));
}
