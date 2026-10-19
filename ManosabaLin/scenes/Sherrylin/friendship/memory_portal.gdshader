shader_type canvas_item;
render_mode unshaded;
uniform sampler2D garden : source_color, filter_linear, repeat_disable;
uniform float clock = 0.0;

float hash21(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
float noise2(vec2 p) {
    vec2 i=floor(p), f=fract(p); f=f*f*(3.0-2.0*f);
    return mix(mix(hash21(i),hash21(i+vec2(1,0)),f.x),mix(hash21(i+vec2(0,1)),hash21(i+vec2(1)),f.x),f.y);
}
float mist(vec2 p) { return noise2(p)*.57+noise2(p*2.03+8.1)*.28+noise2(p*4.17-6.4)*.15; }
void fragment() {
    vec2 p=(UV-vec2(.5,.515))*vec2(1.7777778,1.0);
    float age=clock;
    float unfold=smoothstep(.08,.75,age);
    float radius=mix(.022,.315,unfold)+smoothstep(2.58,3.28,age)*.22;
    float drift=mist(p*8.0+vec2(age*.10,-age*.15));
    float fine=mist(p*27.0+vec2(-age*.20,age*.08));
    float d=length(p)+(drift-.5)*.036+(fine-.5)*.013-radius;
    float edge=exp(-abs(d)*105.0);
    float veil=exp(-abs(d)*29.0)*(.38+.62*fine);
    float mask=1.0-smoothstep(-.012,.026,d);
    float entrance=smoothstep(.08,.32,age);
    float departure=1.0-smoothstep(2.72,3.52,age);
    // The garden slowly moves behind the hands, without moving the circular opening.
    vec2 guv=vec2(.5)+p*vec2(.84,1.05)/(1.0+age*.012)+vec2(0,-age*.005);
    guv+=vec2(drift-.5,fine-.5)*.008;
    vec3 scenery=texture(garden,clamp(guv,vec2(.001),vec2(.999))).rgb;
    vec3 prism=.65+.35*cos(vec3(0,2.1,4.2)+atan(p.y,p.x)*2.0+age*.7+d*170.0);
    float clasp=smoothstep(1.45,2.6,age);
    float contact=exp(-length(p-vec2(0,.015))*9.0)*clasp*.38;
    vec3 color=scenery+vec3(.75,.95,.73)*contact;
    color=mix(color,vec3(.95,1.0,.9),edge*.75);
    color+=prism*veil*.40;
    float wash=smoothstep(2.58,3.05,age);
    color=mix(color,vec3(1.0,.995,.85),wash*.77);
    float alpha=max(mask*.89,veil*.72)*entrance*departure;
    COLOR=vec4(color,alpha);
}
