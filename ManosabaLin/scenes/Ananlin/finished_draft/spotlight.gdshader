shader_type canvas_item;
render_mode unshaded,blend_add;
uniform vec2 target_uv=vec2(.75,.5);
uniform float strength=0.0;
uniform float clock=0.0;
uniform float accent=0.0;
varying float inherited_alpha;
void vertex(){inherited_alpha=COLOR.a;}
void fragment(){
    vec2 size=vec2(1920.0,1080.0);
    vec2 source=vec2(.40,.02)*size;
    vec2 target=target_uv*size;
    vec2 p=UV*size-source;
    vec2 axis=normalize(target-source);
    float length_to_target=length(target-source);
    float along=dot(p,axis);
    float sideways=dot(p,vec2(-axis.y,axis.x));
    float travel=clamp(along/length_to_target,0.0,1.0);
    float width=mix(15.0,175.0,travel);
    // Gaussian falloff across the beam: no polygon sides or outlined cone.
    float beam=exp(-.5*sideways*sideways/(width*width));
    beam*=smoothstep(-15.0,40.0,along)*(1.0-smoothstep(length_to_target*.8,length_to_target+260.0,along));
    beam*=mix(.40,.14,travel);
    vec2 d=(UV-target_uv)*size/vec2(155.0,225.0);
    float spot=exp(-.5*dot(d,d))*.40;
    vec3 ink=vec3(.4,.4,.8);
    // Soft atmospheric striations within the Gaussian, never hard beam borders.
    float air=.95+.035*sin(sideways*.025+clock*.35)+.015*sin(along*.019-clock*.22);
    vec2 floor_d=(UV-target_uv-vec2(0.0,.17))*size/vec2(220.0,48.0);
    float floor_pool=exp(-.5*dot(floor_d,floor_d))*.19;
    float glow=(beam*air+spot+floor_pool)*strength*(1.0+accent*.3);
    COLOR=vec4(ink*glow,inherited_alpha);
}
