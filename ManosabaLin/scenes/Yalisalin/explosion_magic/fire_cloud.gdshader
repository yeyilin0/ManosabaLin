shader_type canvas_item;
render_mode unshaded;
uniform float clock = 0.0;
uniform vec2 center = vec2(0.76,0.55);
uniform float aspect = 1.77778;
float hash(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
float noise(vec2 p) {
	vec2 i=floor(p), f=fract(p); f=f*f*(3.0-2.0*f);
	return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);
}
float fbm(vec2 p) {
	float n=0.0, a=0.55;
	for(int i=0;i<4;i++) { n+=a*noise(p); p=mat2(vec2(1.6,1.2),vec2(-1.2,1.6))*p+3.1; a*=0.48; }
	return n;
}
void fragment() {
	float t=max(0.0,clock-8.72);
	float growth=0.05+0.68*(1.0-exp(-t*1.8));
	vec2 p=(UV-center)*vec2(aspect,1.0);
	p.y+=min(t*0.043,0.17);
	vec2 q=p/growth;
	float n=fbm(q*5.0+vec2(t*0.22,-t*0.65));
	float curl=fbm(q*9.0+vec2(n*2.5,t*-0.38));
	float angle=atan(q.y,q.x);
	float lobe=0.06*sin(angle*9.0+t*.55)+0.035*sin(angle*15.0-t);
	float boundary=length(q*vec2(0.94,1.12)) + (n-.48)*.42 + lobe;
	float alpha=(1.0-smoothstep(.83,1.03,boundary))*smoothstep(8.72,8.88,clock);
	alpha*=1.0-smoothstep(12.7,15.0,clock);
	float heat=clamp((1.0-boundary)*0.75+(1.0-n)*.5+curl*.2,0.0,1.0);
	float seams=smoothstep(.38,.47,curl)*(1.0-smoothstep(.50,.59,curl));
	heat=clamp(heat+seams*.42,0.0,1.0);
	vec3 col=mix(vec3(.055,.012,.018),vec3(.62,.055,.008),smoothstep(.06,.35,heat));
	col=mix(col,vec3(1.0,.28,.008),smoothstep(.35,.56,heat));
	col=mix(col,vec3(1.0,.8,.14),smoothstep(.55,.77,heat));
	col=mix(col,vec3(1.0,.98,.84),smoothstep(.77,.94,heat));
	float hotCore=exp(-dot(p,p)*32.0)*exp(-t*1.3);
	col=mix(col,vec3(1.0,.99,.92),hotCore);
	col*=1.0-smoothstep(11.8,14.8,clock)*.72;
	COLOR=vec4(col,alpha);
}
