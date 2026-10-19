shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float clock = 0.0;
void fragment() {
    vec2 p=(UV-vec2(.5,.52))*vec2(1.777778,1.0);
    float d=length(p);
    float hold=smoothstep(1.43,2.48,clock)*(1.0-smoothstep(2.65,3.50,clock));
    float breath=.96+.04*sin(clock*3.4);
    float core=exp(-d*d/(.0014+smoothstep(2.45,2.95,clock)*.026));
    float halo=exp(-d*7.5)*.24;
    float bloom=smoothstep(2.40,2.95,clock)*(1.0-smoothstep(3.02,3.52,clock));
    float wave_radius=mix(.05,.96,smoothstep(2.86,3.96,clock));
    float wave=exp(-pow((d-wave_radius)/.012,2.0))*(1.0-smoothstep(3.15,4.1,clock))*smoothstep(2.86,2.98,clock);
    vec3 rainbow=.65+.35*cos(vec3(0,2.1,4.2)+d*28.0-clock*.4);
    vec3 hue=mix(vec3(.52,.92,1.0),vec3(.75,1.0,.52),smoothstep(-.13,.13,p.x));
    vec3 rgb=hue*(core*.58+halo)*hold*breath+vec3(.95,.99,.80)*exp(-d*d/.17)*bloom*.34+rainbow*wave*.23;
    COLOR=vec4(rgb,1.0);
}
