shader_type canvas_item;
render_mode unshaded, blend_add;
uniform sampler2D figure_0 : source_color, filter_linear, repeat_disable;
uniform sampler2D figure_1 : source_color, filter_linear, repeat_disable;
uniform sampler2D figure_2 : source_color, filter_linear, repeat_disable;
uniform sampler2D figure_3 : source_color, filter_linear, repeat_disable;
uniform sampler2D figure_4 : source_color, filter_linear, repeat_disable;
uniform sampler2D figure_5 : source_color, filter_linear, repeat_disable;
uniform vec2 frame_size=vec2(1024.0,1024.0);
uniform vec2 viewport_size=vec2(1920.0,1080.0);
uniform float clock=0.0;
uniform float gain=1.0;
uniform float weapon_light_gain=1.55;
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise2(vec2 p){vec2 a=floor(p),b=fract(p);b=b*b*(3.0-2.0*b);return mix(mix(hash(a),hash(a+vec2(1,0)),b.x),mix(hash(a+vec2(0,1)),hash(a+1.0),b.x),b.y);}
vec2 registered_source_uv(vec2 canvas_uv,float index){
    // Fixed registration of the six 1254px cutouts against pose 1. Five stable
    // landmarks (two face centers, collar, shoulder and knee center) determine
    // one uniform scale + translation per image. Never use the changing wing,
    // sword or hair bounds as a pivot, and never animate/deform these offsets.
    float scale=1.0;
    vec2 offset=vec2(0.0);
    if(index>.5 && index<1.5){scale=1.0171486;offset=vec2(-.0023267,-.0100207);}
    else if(index>1.5 && index<2.5){scale=.9796053;offset=vec2(-.0026415,.0213234);}
    else if(index>2.5 && index<3.5){scale=1.0222189;offset=vec2(.0037717,-.0118313);}
    else if(index>3.5 && index<4.5){scale=.9850844;offset=vec2(.0204551,-.0007591);}
    else if(index>4.5){scale=1.0004006;offset=vec2(-.0348523,-.0103878);}
    return (canvas_uv-offset)/scale;
}
float segment_distance(vec2 p,vec2 a,vec2 b){
    vec2 axis=b-a;
    float along=clamp(dot(p-a,axis)/max(dot(axis,axis),.0001),0.0,1.0);
    return length(p-a-axis*along);
}
float weapon_mask(vec2 uv,float index){
    // These areas follow the isolated sprites. Amplify only existing luminous
    // rib/blade pixels; no extra outlines, texture reads or enlarged alpha.
    float ribs=(1.0-smoothstep(.25,.30,uv.x))*smoothstep(.25,.31,uv.y)*(1.0-smoothstep(.59,.64,uv.y));
    if(index>2.5 && index<3.5){
        ribs=max(ribs,smoothstep(.64,.68,uv.x)*smoothstep(.28,.32,uv.y)*(1.0-smoothstep(.75,.80,uv.y)));
    }
    float sword=0.0;
    if(index>.5){
        vec2 guard=vec2(.60,.415),tip=vec2(.446,.714),pommel=vec2(.70,.25);
        if(index>1.5 && index<2.5){tip=vec2(.454,.68);}
        else if(index>2.5 && index<3.5){guard=vec2(.48,.533);tip=vec2(.44,.65);pommel=vec2(.528,.365);}
        else if(index>3.5 && index<4.5){guard=vec2(.49,.547);tip=vec2(.44,.70);pommel=vec2(.553,.398);}
        else if(index>4.5){guard=vec2(.542,.545);tip=vec2(.495,.665);pommel=vec2(.597,.398);}
        sword=1.0-smoothstep(.018,.041,segment_distance(uv,guard,tip));
        sword=max(sword,1.0-smoothstep(.032,.067,length(uv-guard)));
        sword=max(sword,1.0-smoothstep(.009,.018,segment_distance(uv,guard,pommel)));
        // Leave the gripping hand at its existing exposure.
        vec2 grip=guard+(pommel-guard)*.27;
        float hand=1.0-smoothstep(.7,1.15,length((uv-grip)/vec2(.047,.042)));
        sword*=1.0-hand;
    }
    return max(ribs,sword);
}
vec4 pose(vec2 uv,float index){
    float inside=step(0.0,uv.x)*step(uv.x,1.0)*step(0.0,uv.y)*step(uv.y,1.0);
    // Every pose has its own transparent image. Clamp to pixel centers so linear
    // filtering cannot reach another pose or reveal hidden RGB outside its alpha.
    vec2 half_texel=vec2(.5)/frame_size;
    vec2 sample_uv=clamp(uv,half_texel,vec2(1.0)-half_texel);
    vec4 c;
    if(index<.5){c=texture(figure_0,sample_uv);}
    else if(index<1.5){c=texture(figure_1,sample_uv);}
    else if(index<2.5){c=texture(figure_2,sample_uv);}
    else if(index<3.5){c=texture(figure_3,sample_uv);}
    else if(index<4.5){c=texture(figure_4,sample_uv);}
    else{c=texture(figure_5,sample_uv);}
    c.a*=inside;return c;
}
void fragment(){
    vec2 cell=frame_size;
    float fit=min(viewport_size.x*.85/cell.x,viewport_size.y*.88/cell.y);
    vec2 uv=(UV-vec2(.5,.51))*viewport_size/(cell*fit)+.5;
    float index=clock<2.10?0.0:(clock<2.45?1.0:(clock<3.05?2.0:(clock<3.60?3.0:(clock<4.18?4.0:5.0))));
    vec2 source_uv=registered_source_uv(uv,index);
    // Test source coverage after registration: a translated wing must not be
    // cropped by the old unregistered canvas boundary.
    if(source_uv.x<0.0 || source_uv.x>1.0 || source_uv.y<0.0 || source_uv.y>1.0){
        COLOR=vec4(0.0);
    }else{
    // Play the six supplied poses directly. No local warps, pose interpolation,
    // cloth sway or crossfades: every hand and silhouette stays exactly in its frame.
    vec4 tex=pose(source_uv,index);
    float lum=dot(tex.rgb,vec3(.2126,.7152,.0722));
    float n=noise2(uv*22.0+vec2(clock*.07,-clock*.18));
    float fine=noise2(uv*91.0+vec2(-clock*.15,clock*.32));
    float form=smoothstep(.94,1.35,clock);
    float radius=length((uv-vec2(.52,.44))*vec2(.92,.8));
    float front=1.0-smoothstep(form*.85-.10,form*.85+.10,radius+(n-.5)*.22);
    float dissolve=smoothstep(4.65,5.80,clock);
    float remaining=1.0-smoothstep(1.0-dissolve-.09,1.0-dissolve+.09,n*.72+radius*.5);
    vec3 pearl=vec3(1.0,.80,.89),garnet=vec3(.80,.24,.31);
    vec3 hue=mix(pearl,garnet,smoothstep(.44,.70,uv.x));
    float filaments=pow(.5+.5*sin(uv.y*105.0+n*14.0-clock*2.0),14.0);
    // No opaque portrait: cloth becomes sparse light planes; anatomy remains translucent.
    float face=exp(-dot((source_uv-vec2(.39,.27))*vec2(7,10),(source_uv-vec2(.39,.27))*vec2(7,10)));
    face+=exp(-dot((source_uv-vec2(.65,.23))*vec2(7,10),(source_uv-vec2(.65,.23))*vec2(7,10)));
    float hands=exp(-dot((source_uv-vec2(.52,.46))*vec2(7,10),(source_uv-vec2(.52,.46))*vec2(7,10)));
    float legibility=(face*.10+hands*.16)*smoothstep(1.50,2.10,clock);
    // Each frame is itself woven from luminous hair/fabric filaments (the same
    // material reference as Friendship/TrueEnding), not a recolored opaque portrait.
    float density=.86+.14*smoothstep(.25,.72,fine);
    vec3 rgb=tex.rgb*(.45+lum*.24)*density;
    float bright_material=smoothstep(.46,.82,lum);
    rgb*=1.0+(weapon_light_gain-1.0)*weapon_mask(source_uv,index)*bright_material;
    rgb+=hue*(filaments*.026+legibility*.18);
    float pulse=exp(-pow((clock-2.45)/.065,2.0))*.09+exp(-pow((clock-3.05)/.075,2.0))*.12;
    rgb+=hue*pulse*tex.a;
    // Preserve source coverage exactly; expanding alpha would expose hidden RGB.
    COLOR=vec4(rgb,tex.a*front*remaining*gain);
    }
}
