shader_type canvas_item;
render_mode unshaded, blend_add;
uniform vec2 viewport_size = vec2(1920.0,1080.0);
uniform float clock = 0.0;
uniform float gain = 1.0;
float h21(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise2(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);return mix(mix(h21(i),h21(i+vec2(1,0)),f.x),mix(h21(i+vec2(0,1)),h21(i+1.0),f.x),f.y);}
void fragment(){
    vec2 p=(UV-vec2(.5,.49))*vec2(viewport_size.x/viewport_size.y,1.0);
    float r=length(p),theta=atan(p.y,p.x);
    float envelope=smoothstep(2.9,3.25,clock)*(1.0-smoothstep(4.65,5.3,clock));
    vec3 rgb=vec3(0);
    for(int i=0;i<4;i++){
        float k=float(i),age=max(0.0,clock-3.02-k*.145);
        float front=.075+age*(.67+k*.045);
        float n=noise2(vec2(theta*2.6+k*8.0,r*8.0-clock*.55));
        float warp=.027*sin(theta*3.0+clock*1.3+k)+.019*(n-.5);
        float width=.068+k*.015;
        float band=exp(-pow((r-front+warp)/width,2.0));
        float ridge=exp(-pow((r-front+warp)/.0075,2.0))*smoothstep(.30,.7,n);
        float silk=exp(-pow((r-front+warp+.021)/.0022,2.0))*smoothstep(.34,.72,n);
        vec3 hue=mix(vec3(.83,.20,.32),vec3(1.0,.78,.91),.5+.5*sin(theta*1.4+k+clock*.4));
        rgb+=(hue*(band*.34+ridge*.32)+vec3(1.0,.9,.97)*silk*.20)*smoothstep(0.0,.20,age);
    }
    float center=exp(-r*r/.028)*sin(smoothstep(2.97,4.05,clock)*3.14159);
    float broad=exp(-r*r/.4)*sin(smoothstep(3.1,4.7,clock)*3.14159);
    rgb+=vec3(1.0,.84,.92)*(center*.38+broad*.18);
    COLOR=vec4(rgb,envelope*gain);
}
