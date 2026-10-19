shader_type canvas_item;
render_mode unshaded;
uniform sampler2D court_tex : source_color, filter_linear, repeat_disable;
uniform sampler2D stage_tex : source_color, filter_linear, repeat_disable;
uniform vec2 stage_size = vec2(1536.0,1024.0);
uniform vec2 viewport_size = vec2(1920.0,1080.0);
uniform float clock = 0.0;
uniform float gain = 1.0;
float bell(float d,float w){return exp(-d*d/(w*w));}
void fragment(){
    float aspect=viewport_size.x/viewport_size.y;
    float fade=1.0-smoothstep(4.85,5.90,clock);
    vec3 court_rgb=vec3(0.0);
    float court_alpha=0.0;
    // The full room is used only during the final outward expansion.
    if(clock>=3.13){
        float push=smoothstep(3.13,4.60,clock);
        float depth=smoothstep(.45,.98,UV.y);
        vec2 crop=vec2(min(1.0,aspect/2.0),min(1.0,2.0/aspect));
        vec2 room_uv=(UV-vec2(.5,.56))*crop/(1.0+push*(.012+depth*.035))+vec2(.5,.56);
        room_uv.x+=sin(clock*.55)*push*(.001+depth*.003);
        court_rgb=texture(court_tex,room_uv).rgb*1.15;
        float radius=length((UV-vec2(.5,.59))*vec2(aspect,1.0));
        float furthest=length(vec2(aspect*.5,.59));
        float wave=smoothstep(3.13,4.52,clock)*(furthest+.20);
        float contour=.025*sin(UV.x*19.0+UV.y*7.0)+.012*sin(UV.x*43.0-UV.y*13.0);
        float reveal=1.0-smoothstep(wave-.14,wave+.06,radius+contour);
        court_alpha=reveal*smoothstep(3.13,3.55,clock)*fade*.92;
        vec2 floor_uv=vec2((room_uv.x-.5)/max(.16,room_uv.y-.45),room_uv.y);
        float beam=pow(.5+.5*cos(floor_uv.x*23.0),14.0);
        float floor_mask=smoothstep(.67,.76,room_uv.y)*(1.0-smoothstep(.91,1.0,room_uv.y));
        court_rgb+=vec3(.32,.14,.22)*beam*floor_mask*.24;
        court_rgb+=vec3(.60,.28,.36)*bell(radius-wave+.09,.055)*.16;
    }
    vec3 stage_rgb=vec3(0.0);
    float stage_alpha=0.0;
    float light_alpha=0.0;
    if(clock<4.65){
        float fit=min(viewport_size.x*.96/stage_size.x,viewport_size.y*.99/stage_size.y);
        vec2 uv=(UV-vec2(.5,.50))*viewport_size/(stage_size*fit)+.5;
        if(uv.x>=0.0 && uv.x<=1.0 && uv.y>=0.0 && uv.y<=1.0){
            // Three separate architectural groups: one inward-facing left arch,
            // a smaller distant inner-right arch and an inward-facing outer-right arch.
            float left_group=1.0-smoothstep(.38,.46,uv.x);
            float outer_right=smoothstep(.75,.82,uv.x);
            float near_group=max(left_group,outer_right);
            float depth=mix(.36,1.0,near_group);
            float push=smoothstep(.80,3.10,clock);
            vec2 sample_uv=(uv-vec2(.5,.62))/(1.0+push*.007*depth)+vec2(.5,.62);
            sample_uv.x+=sin(clock*.40)*push*.0012*depth;
            vec4 scene=texture(stage_tex,clamp(sample_uv,vec2(0.0),vec2(1.0)));
            float delay=mix(.24,.10,outer_right)*(1.0-left_group);
            float rise=smoothstep(.12+delay,1.18+delay,clock);
            float front=1.13-rise*1.32;
            float appear=smoothstep(front-.13,front+.13,uv.y)*smoothstep(.03,.25,clock);
            float stage_fade=1.0-smoothstep(3.40,4.60,clock);
            float lum=dot(scene.rgb,vec3(.2126,.7152,.0722));
            float light_arrival=smoothstep(.22,1.30,clock);
            // The painted jamb/ledge shadows carry the dimensional lighting.
            // Let the outer faces settle slightly deeper as the central light
            // arrives, without dimming the whole scene or tracing every pane.
            float outer_face=left_group*(1.0-smoothstep(.10,.22,uv.x))
                +outer_right*smoothstep(.89,.98,uv.x);
            stage_rgb=scene.rgb*mix(.84,.95,near_group)*(1.0-outer_face*light_arrival*.12);
            float inward_edge=bell(uv.x-.303,.043)+bell(uv.x-.775,.038);
            stage_rgb+=vec3(.8)*inward_edge*lum*light_arrival*.035;
            // Broad, slowly moving highlights run only along the sparse outer
            // ribbons baked into the cutout; the glass stays calm and legible.
            float outer_ribbon=max(1.0-smoothstep(.10,.23,uv.x),smoothstep(.87,.98,uv.x));
            float flow_wave=.5+.5*sin(uv.y*8.0+clock*1.20+uv.x*2.3);
            float flow_glint=pow(lum,3.0)*outer_ribbon*smoothstep(.32,.90,flow_wave);
            stage_rgb+=vec3(.80,.78,.84)*flow_glint*.20*light_arrival;
            float impact=bell(clock-2.45,.085)*.045+bell(clock-3.05,.095)*.06;
            float lectern=smoothstep(.51,.76,uv.y);
            stage_rgb+=vec3(1.0,.58,.70)*impact*lectern*lum;
            stage_alpha=scene.a*appear*stage_fade*.96;

            // Feathered #CCCCCC light enters through the empty space between
            // the arches. Gaussian shoulders avoid a visible cone outline.
            float light_x=uv.x-.485-.009*sin(uv.y*3.6+clock*.13);
            float spread=.048+uv.y*.075;
            float core=bell(light_x,spread);
            float halo=bell(light_x,spread*2.0);
            float threads=.72+.16*sin(uv.x*101.0+uv.y*1.8-clock*.19)
                +.12*sin(uv.x*163.0-uv.y*.9+clock*.12);
            float vertical=smoothstep(.0,.075,uv.y)*(1.0-smoothstep(.67,.98,uv.y));
            light_alpha=(core*.22*threads+halo*.055)*vertical
                *smoothstep(.22,1.30,clock)*stage_fade;
        }
    }
    // Composite in premultiplied space, then return straight alpha. Transparent
    // pixels in the apparition must not tint the original battlefield.
    float behind_alpha=light_alpha+court_alpha*(1.0-light_alpha);
    vec3 behind_rgb=vec3(.8)*light_alpha+court_rgb*court_alpha*(1.0-light_alpha);
    float alpha=stage_alpha+behind_alpha*(1.0-stage_alpha);
    vec3 color=(stage_rgb*stage_alpha+behind_rgb*(1.0-stage_alpha))/max(alpha,.00001);
    COLOR=vec4(color,alpha*gain);
}
