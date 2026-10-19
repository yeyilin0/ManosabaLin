shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float clock = 0.0;
uniform float gain = 1.0;
uniform vec2 viewport_size = vec2(1920.0,1080.0);
float h21(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise2(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);return mix(mix(h21(i),h21(i+vec2(1,0)),f.x),mix(h21(i+vec2(0,1)),h21(i+1.0),f.x),f.y);}
vec2 core(float side, float t){
    float enter=smoothstep(0.0,.84,t), orbit=smoothstep(.62,1.48,t), close=smoothstep(1.05,1.82,t);
    float angle=side*3.14159265+orbit*2.0;
    vec2 start=vec2(side>.5?-.57:.57,side>.5?.10:-.10);
    vec2 ring=vec2(cos(angle)*.21,sin(angle)*.155);
    return mix(mix(start,ring,enter),vec2(0),close);
}
void fragment(){
    vec2 p=(UV-vec2(.5,.485))*vec2(viewport_size.x/viewport_size.y,1.0);
    vec3 rgb=vec3(0);float env=smoothstep(0.0,.2,clock)*(1.0-smoothstep(1.68,2.32,clock));
    for(int c=0;c<2;c++){
        float side=float(c);vec2 center=core(side,clock);
        vec2 q=p-center;float a=atan(q.y,q.x), d=length(q);
        float rough=1.0+.12*sin(a*3.0+clock*4.0)+.06*sin(a*7.0-clock*3.0);
        float radius=.078+sin(smoothstep(0.0,1.8,clock)*3.14159)*.024;
        float bead=exp(-pow(d/(radius*rough),2.0)*2.0);
        float skirt=exp(-d*d/.0315)*.28+exp(-d*d/.117)*.055;
        vec3 hue=c==0?vec3(1.0,.52,.71):vec3(.62,.09,.17);
        vec3 hot=c==0?vec3(1.0,.92,.96):vec3(1.0,.46,.51);
        rgb+=hue*skirt+mix(hue,hot,pow(bead,1.8))*bead;
        // Three drifting filament shells attached to each deformed light core.
        for(int j=0;j<3;j++){
            float k=float(j);float ring=radius*(1.15+k*.29)*(1.0+.13*sin(a*3.0+clock*(2.0+k)));
            float arc=pow(max(0.0,sin(a*2.0+clock*2.7+k*2.0)),3.0);
            rgb+=mix(hue,hot,.32)*exp(-pow((d-ring)/.0048,2.0))*.48*arc;
        }
        // Evaluate past core positions: the energy stream bends with the core's trajectory.
        for(int j=1;j<=12;j++){
            float back=float(j)/12.0, age=back*.72;
            vec2 old=core(side,max(0.0,clock-age));
            old+=vec2(sin(back*10.0+clock*3.0),cos(back*12.0-clock*2.0))*.027*back;
            float width=mix(.039,.009,back), fall=1.0-back;
            rgb+=mix(hue,hot,.24)*exp(-dot(p-old,p-old)/(width*width))*.14*fall;
        }
    }
    float r=length(p*vec2(1.0,1.13)),theta=atan(p.y,p.x);
    float weave=smoothstep(.54,1.2,clock)*(1.0-smoothstep(1.56,2.08,clock));
    for(int j=0;j<4;j++){
        float k=float(j),radius=.19+k*.016+.006*sin(theta*3.0+clock*2.0+k);
        float n=noise2(vec2(theta*2.0-clock*.42,k*3.0+clock*.2));
        float line=exp(-pow((r-radius)/(.0015+n*.002),2.0));
        float haze=exp(-pow((r-radius)/.02,2.0));
        vec3 hue=mix(vec3(.7,.17,.26),vec3(1.0,.76,.86),.5+.5*sin(theta+clock*.85+k));
        rgb+=hue*(line*.34+haze*.045)*smoothstep(.23,.72,n)*weave;
    }
    COLOR=vec4(rgb,env*gain);
}
