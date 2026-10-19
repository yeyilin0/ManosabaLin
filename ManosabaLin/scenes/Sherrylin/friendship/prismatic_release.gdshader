shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float clock = 0.0;
float hash21(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
float noise2(vec2 p) {
    vec2 i=floor(p),f=fract(p); f=f*f*(3.0-2.0*f);
    return mix(mix(hash21(i),hash21(i+vec2(1,0)),f.x),mix(hash21(i+vec2(0,1)),hash21(i+vec2(1)),f.x),f.y);
}
vec3 spectrum(float phase) {
    return .58+.42*cos(6.2831853*(phase+vec3(.0,.333,.667)));
}
void fragment() {
    vec2 p=(UV-vec2(.5,.515))*vec2(1.7777778,1.0);
    float d=length(p), angle=atan(p.y,p.x);
    float age=max(clock-2.66,0.0);
    float envelope=smoothstep(2.66,2.90,clock)*(1.0-smoothstep(3.55,4.20,clock));
    float flow=noise2(p*6.0+vec2(age*.18,-age*.31));
    float fine=noise2(p*31.0+vec2(-age*.27,age*.19));
    vec3 light=vec3(0.0);
    // Four staggered, broken spectral sheets: different speed, width, turbulence and curl.
    // Soft internal caustics and narrow highlights keep the spectrum richer than simple rings.
    for(int i=0;i<4;i++) {
        float fi=float(i);
        float t=max(age-fi*.087,0.0);
        float radius=.18+(1.0-exp(-t*(1.6-fi*.12)))*(.84+fi*.055);
        float curl=sin(angle*(2.0+fi*.3)-age*.60+fi*1.9)*.023;
        curl+=sin(angle*7.0+age*.31+fi)*.007;
        float distance=d-radius+curl+(flow-.5)*(.035+fi*.008);
        float width=.009+fi*.005+(fine*.009);
        float ribbon=exp(-pow(distance/width,2.0));
        float haze=exp(-pow(distance/(width*3.3),2.0))*.16;
        float broken=.28+.72*smoothstep(.20,.67,noise2(vec2(cos(angle),sin(angle))*3.0+vec2(fi*3.7,age*.28)));
        vec3 hue=spectrum(distance*8.5+fi*.19+angle*.11-age*.07);
        float glint=exp(-pow((distance-width*.32)/.0028,2.0))*.11;
        light+=(hue*(ribbon*.26+haze)+vec3(.88,.97,1.0)*glint)*broken*smoothstep(0.0,.14,t);
    }
    // Wide wisps between the advancing fronts breathe independently of the bright edges.
    float spread=.19+age*.47;
    float veil=exp(-pow((d-spread)/.15,2.0));
    float lace=pow(max(0.0,sin(d*60.0-angle*2.0+flow*7.0-age*2.0)),6.0);
    light+=spectrum(angle*.16+d*.9)*veil*lace*.08;
    COLOR=vec4(light,envelope);
}
