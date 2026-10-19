shader_type canvas_item;
render_mode unshaded, blend_add;
uniform sampler2D backs : source_color, filter_linear, repeat_disable;
uniform float clock = 0.0;

float hash21(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
float noise2(vec2 p) {
    vec2 i=floor(p),f=fract(p); f=f*f*(3.0-2.0*f);
    return mix(mix(hash21(i),hash21(i+vec2(1,0)),f.x),mix(hash21(i+vec2(0,1)),hash21(i+vec2(1)),f.x),f.y);
}
void fragment() {
    vec2 uv=(UV-.5)/.94+.5;
    // Small continuously varying motion: the head stays registered, hair and hems drift.
    float sway=pow(clamp(uv.y,0.0,1.0),2.0);
    uv.x+=sin(uv.y*5.5-clock*1.3)*.0025*sway;
    uv.y+=sin(uv.x*10.0+clock*.7)*.0014*sway;
    vec4 tex=texture(backs,clamp(uv,vec2(.001),vec2(.999)));
    float lum=dot(tex.rgb,vec3(.2126,.7152,.0722));
    vec3 hue=mix(vec3(.23,.73,1.0),vec3(.63,1.0,.53),smoothstep(.45,.56,uv.x));
    float n=noise2(uv*18.0+vec2(clock*.1,-clock*.2));
    // Reveal travels outward from the joined hands; filament detail leads the softer interior.
    float distance_from_hands=length((uv-vec2(.49,.5))*vec2(1.0,.76));
    float reveal=smoothstep(1.43,2.26,clock)*.84;
    float front=1.0-smoothstep(reveal-.08,reveal+.06,distance_from_hands+(n-.5)*.20);
    float filament=smoothstep(.20,.87,lum);
    float shimmer=.92+.08*sin(uv.y*62.0-clock*2.8+n*5.0);
    float disperse=smoothstep(2.91,3.84,clock);
    float dissolve=1.0-smoothstep(1.0-disperse-.15,1.0-disperse+.05,n);
    float envelope=smoothstep(1.43,1.65,clock)*(1.0-smoothstep(3.25,3.92,clock));
    float border=step(0.0,uv.x)*step(uv.x,1.0)*step(0.0,uv.y)*step(uv.y,1.0);
    float alpha=tex.a*front*envelope*dissolve*border;
    // Re-light from luminance: original solid cloth colors cannot turn this into opaque people.
    vec3 light=mix(hue,hue*.45+vec3(.64),filament)*(.10+filament*.87)*shimmer;
    COLOR=vec4(light,alpha*.82);
}
