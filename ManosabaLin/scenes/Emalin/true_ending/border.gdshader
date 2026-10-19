shader_type canvas_item;
render_mode unshaded, blend_add;
uniform vec2 viewport_size = vec2(1920.0,1080.0);
uniform float clock = 0.0;
uniform float gain = 1.0;
uniform float pulse = 0.0;

float bell(float distance,float width){return exp(-pow(distance/width,2.0));}
float ring_radius(float angle,float lane){
    return 1.10+lane*.056+.025*sin(angle*2.0-clock*.14+lane*.35)
          +.009*sin(angle*5.0+clock*.23+lane*.8);
}

void fragment(){
    // One scene-centered ellipse continues beyond all four screen edges.
    // The viewport itself crops it into corner arcs; no independent corner fans.
    vec2 q=(UV-vec2(.5))*2.0;
    float radius=length(q);
    if(radius>.86){
        float angle=atan(q.y,q.x);
        float aspect=viewport_size.x/viewport_size.y;
        // Approximate normal distance in screen-height units, keeping ribbon width stable.
        float normal_scale=.5/length(vec2(cos(angle)/aspect,sin(angle)));
        // Reunion field: silver-blue ribbons with a cool pink-white reflection.
        vec3 hue=mix(vec3(.48,.66,.94),vec3(.93,.76,.90),.30+.18*sin(angle*2.0-clock*.11));
        vec3 pearl=vec3(.89,.96,1.0);
        vec3 rgb=vec3(0.0);
        for(int i=0;i<3;i++){
            float k=float(i);
            float distance=(radius-ring_radius(angle,k))*normal_scale;
            float flow=.62+.38*pow(.5+.5*sin(angle*3.0-clock*.52+k*1.7),2.0);
            float strength=i==1?1.0:.63;
            float width=.0085+.0035*(.5+.5*sin(angle*4.0-clock*.34+k));
            float ribbon=bell(distance,width);
            float haze=bell(distance,.026);
            float core=bell(distance+.0018*sin(angle*11.0-clock*.63+k),.0020);
            // Offset fine strands slide through the luminous ribbon with varied gaps.
            float strand=bell(distance-.006-.0025*sin(angle*7.0+clock*.37+k),.0012);
            float strand_gate=smoothstep(-.15,.72,sin(angle*6.0-clock*.61+k*2.1));
            rgb+=(hue*(ribbon*.24+haze*.065)+pearl*(core*.60+strand*strand_gate*.20))*flow*strength;
        }
        // Inner filaments dip back into the frame to make partial curved edge links.
        float bridge_radius=1.10-.11*pow(.5+.5*sin(angle*4.0-clock*.12),2.0)
                           +.015*sin(angle*3.0+clock*.22);
        float bridge_distance=(radius-bridge_radius)*normal_scale;
        float bridge_gate=smoothstep(.05,.78,sin(angle*5.0-clock*.26+sin(angle*2.0)));
        rgb+=mix(hue,pearl,.60)*(bell(bridge_distance,.0020)*.30+bell(bridge_distance,.012)*.08)*bridge_gate;
        // The halo belongs to the ring, so it never becomes a flat screen-edge wash.
        rgb=vec3(1.0)-exp(-rgb*1.08);
        float reveal=smoothstep(4.05,5.1,clock);
        float breathing=.94+.035*sin(clock*.65)+.025*sin(clock*.91+.7);
        COLOR=vec4(rgb,gain*reveal*breathing*.96*(1.0+min(pulse,1.35)*.24));
    }else{
        COLOR=vec4(0.0);
    }
}
