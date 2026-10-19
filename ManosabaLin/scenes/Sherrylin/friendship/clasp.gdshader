shader_type canvas_item;
render_mode unshaded;
uniform sampler2D hands : source_color, filter_linear, repeat_disable;
uniform float clock = 0.0;
vec4 pose(vec2 uv,float index) {
    float valid=step(0.0,uv.x)*step(uv.x,1.0)*step(0.0,uv.y)*step(uv.y,1.0);
    vec2 q=clamp(uv,vec2(.001),vec2(.999));
    vec4 c=texture(hands,vec2((q.x+index)/3.0,q.y));
    c.a*=valid;
    return c;
}
void fragment() {
    // Each atlas cell is square. Cuffs are clipped by the organic portal, not a rectangle.
    vec2 p=(UV-vec2(.5,.515))*vec2(1920.0,1080.0);
    float approach=smoothstep(.56,1.25,clock);
    vec2 uv=p/760.0+vec2(.5,.72);
    float side=uv.x<.5 ? -1.0 : 1.0;
    uv.x-=side*(1.0-approach)*.035;
    uv.y+=sin(uv.x*3.14159)*.013*sin(approach*3.14159);
    // Wrist movement eases before finger closure; the final clasp has a tiny settling movement.
    uv.y+=sin(clamp(clock-1.45,0.0,.45)*9.0)*.008*(1.0-smoothstep(1.65,2.0,clock));
    // Never alpha-crossfade different fingers: overlapping silhouettes looked like extra hands.
    // Sample exactly one registered pose, deforming the wrist/finger region within that pose.
    float index=clock<1.23 ? 0.0 : (clock<1.46 ? 1.0 : 2.0);
    float curl=smoothstep(.84,1.23,clock);
    float palm=exp(-pow((uv.y-.61)/.19,2.0))*smoothstep(.13,.35,uv.x)*(1.0-smoothstep(.69,.90,uv.x));
    if(index<.5) {
        uv.x+=side*curl*.024*palm;
        uv.y-=curl*.010*palm;
    } else if(index<1.5) {
        float close=smoothstep(1.23,1.46,clock);
        uv.x+=side*(close-.5)*.016*palm;
        uv.y+=(close-.5)*.012*palm;
    }
    vec4 result=pose(uv,index);
    vec3 rgb=result.rgb;
    float contact=exp(-length(p-vec2(8,5))/170.0)*smoothstep(1.43,2.13,clock);
    vec3 hue=mix(vec3(.28,.84,1),vec3(.67,1,.48),smoothstep(-110.0,110.0,p.x));
    rgb+=hue*contact*.72;
    // Brief contact glints accompany finger closure without duplicating the hand silhouettes.
    float touch=exp(-pow((clock-1.23)/.058,2.0))*.18+exp(-pow((clock-1.46)/.055,2.0))*.24;
    rgb+=hue*exp(-length(p-vec2(8,5))/95.0)*touch;
    float radius=340.0+(sin(atan(p.y,p.x)*7.0+clock*.8)+sin(atan(p.y,p.x)*13.0-clock)) *3.0;
    float portal=1.0-smoothstep(radius-28.0,radius+10.0,length(p));
    float alpha=result.a*portal*smoothstep(.48,.82,clock)*(1.0-smoothstep(2.17,2.76,clock));
    COLOR=vec4(rgb,alpha);
}
