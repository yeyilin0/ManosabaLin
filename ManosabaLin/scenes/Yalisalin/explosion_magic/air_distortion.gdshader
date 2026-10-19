shader_type canvas_item;
render_mode unshaded;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;
uniform vec2 center=vec2(0.76,0.55);
uniform float clock=0.0;
uniform float aspect=1.77778;
void fragment() {
	float t=clock-4.08;
	vec2 p=(SCREEN_UV-center)*vec2(aspect,1.0);
	float r=length(p);
	vec2 dir=p/max(r,0.002)/vec2(aspect,1.0);
	float displacement=0.0;
	for(int i=0;i<6;i++) {
		float delay=float(i)*.17+step(3.0,float(i))*.35;
		float age=t-delay;
		float radius=age*1.65;
		float envelope=step(0.0,age)*(1.0-smoothstep(.8,1.8,age));
		displacement+=sin((r-radius)*65.0)*exp(-pow((r-radius)/.095,2.0))*.032*envelope;
	}
	float heat=sin(p.y*70.0+t*10.0)*sin(p.x*43.0-t*4.0)*.002;
	heat*=smoothstep(.15,.5,t)*(1.0-smoothstep(1.0,2.9,t))*exp(-r*2.2);
	vec2 offset=dir*displacement+vec2(heat,heat*.5);
	COLOR=vec4(texture(screen_texture,clamp(SCREEN_UV+offset,vec2(.001),vec2(.999))).rgb,1.0);
}
