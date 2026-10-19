shader_type canvas_item;
render_mode unshaded, blend_add;
uniform vec2 viewport_size=vec2(1920.0,1080.0);
uniform float clock=0.0;
uniform float gain=1.0;
uniform float pulse=0.0;
float bell(float d,float w){return exp(-d*d/(w*w));}
void fragment(){
    vec2 q=(UV-.5)*2.0;
    float r=length(q);
    if(r>.84){
        float a=atan(q.y,q.x),aspect=viewport_size.x/viewport_size.y;
        float normal_scale=.5/length(vec2(cos(a)/aspect,sin(a)));
        // Judgment field: #CC6666 rose-red over wine-red haze, with a warm core.
        vec3 garnet=vec3(.80,.40,.40),wine=vec3(.48,.10,.20),pearl=vec3(1.0,.66,.58),rgb=vec3(0.0);
        for(int i=0;i<3;i++){
            float k=float(i);
            // One offscreen ellipse; the viewport crops its continuous curved lanes.
            float radius=1.095+k*.052+.021*sin(a*2.0-clock*.13+k)+.007*sin(a*5.0+clock*.19);
            float d=(r-radius)*normal_scale;
            float flow=.55+.45*pow(.5+.5*sin(a*3.0-clock*.49+k*1.4),2.0);
            float width=.009+.0035*sin(a*4.0-clock*.28+k);
            float seam=d+.0015*sin(a*13.0-clock*.8+k);
            float gate=smoothstep(-.15,.74,sin(a*6.0-clock*.50+k*2.1));
            rgb+=(garnet*bell(d,width)*.29+wine*bell(d,.031)*.075
                +pearl*(bell(seam,.0021)*.60+bell(d-.006,.0011)*gate*.22))*flow*(i==1?1.0:.65);
        }
        float bridge=1.09-.105*pow(.5+.5*sin(a*4.0-clock*.11),2.0)+.013*sin(a*3.0+clock*.2);
        float d=(r-bridge)*normal_scale;
        float gate=smoothstep(.05,.76,sin(a*5.0-clock*.24+sin(a*2.0)));
        rgb+=pearl*(bell(d,.0018)*.30+bell(d,.012)*.065)*gate;
        float reveal=smoothstep(4.15,5.35,clock);
        float breath=.94+.035*sin(clock*.61)+.025*sin(clock*.87+.7);
        COLOR=vec4(vec3(1.0)-exp(-rgb*1.18),gain*reveal*breath*(1.0+min(pulse,1.0)*.29));
    }else{COLOR=vec4(0.0);}
}
