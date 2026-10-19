shader_type canvas_item;
render_mode unshaded, blend_add;

uniform vec2 viewport_size = vec2(1920.0,1080.0);
uniform float clock = 0.0;
uniform float gain = 1.0;

float bell(float d,float width){return exp(-d*d/(width*width));}

void fragment(){
    // A single oversized ellipse runs outside every side of the viewport. The
    // four visible corner arcs remain connected offscreen, never corner fans.
    vec2 q=(UV-vec2(.5))*2.0;
    float radius=length(q);
    if(radius>.86){
        float angle=atan(q.y,q.x);
        float aspect=viewport_size.x/max(viewport_size.y,1.0);
        float normal_scale=.5/length(vec2(cos(angle)/aspect,sin(angle)));
        vec3 blue=vec3(.20,.80,1.0);
        vec3 green=vec3(.54,.94,.59);
        vec3 pearl=vec3(.84,1.0,.93);
        vec3 rgb=vec3(0.0);
        for(int i=0;i<3;i++){
            float lane=float(i);
            float center=1.096+lane*.052+.020*sin(angle*2.0-clock*.12+lane*.5)
                +.006*sin(angle*5.0+clock*.19+lane*.7);
            float d=(radius-center)*normal_scale;
            float tide=.5+.5*sin(angle*3.0-clock*.47+lane*1.7);
            float flow=.57+.43*tide*tide;
            float width=.008+.003*(.5+.5*sin(angle*4.0-clock*.26+lane));
            vec3 hue=mix(blue,green,.5+.5*sin(angle-clock*.13+lane*.6));
            // Iridescence is a reflection on narrow strands, not a rainbow wash.
            vec3 sheen=mix(pearl,vec3(.91,.77,1.0),.10+.09*sin(angle*5.0-clock*.31));
            float core=bell(d+.0015*sin(angle*11.0-clock*.67+lane),.0019);
            float strand=bell(d-.006-.002*sin(angle*7.0+clock*.31+lane),.0011);
            float strand_gate=smoothstep(-.08,.78,sin(angle*6.0-clock*.55+lane*2.1));
            float weight=i==1?1.0:.64;
            rgb+=(hue*(bell(d,width)*.31+bell(d,.027)*.060)
                +sheen*(core*.60+strand*strand_gate*.22))*flow*weight;
            // A handful of elongated glints travel on the ring itself. Analytic
            // angular envelopes avoid particle nodes, textures or per-frame RNG.
            float glint=pow(max(0.0,sin(angle*4.0-clock*.79+lane*2.2)),62.0);
            rgb+=sheen*bell(d,.0038)*glint*.11*weight;
        }
        // Short curved links occasionally dip inside the middle of an edge;
        // the central play area remains completely transparent.
        float bridge=1.097-.105*pow(.5+.5*sin(angle*4.0-clock*.10),2.0)
            +.013*sin(angle*3.0+clock*.17);
        float distance=(radius-bridge)*normal_scale;
        float gate=smoothstep(.06,.79,sin(angle*5.0-clock*.23+sin(angle*2.0)));
        vec3 bridge_hue=mix(blue,green,.5+.5*sin(angle+clock*.12));
        rgb+=mix(bridge_hue,pearl,.58)*(bell(distance,.0018)*.29
            +bell(distance,.012)*.070)*gate;
        float reveal=smoothstep(0.0,.62,clock);
        float breath=.95+.028*sin(clock*.59)+.022*sin(clock*.83+.8);
        COLOR=vec4(vec3(1.0)-exp(-rgb*1.12),gain*reveal*breath);
    }else{
        COLOR=vec4(0.0);
    }
}
