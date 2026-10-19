shader_type canvas_item;
render_mode unshaded;
uniform vec2 target_uv=vec2(.75,.5);
uniform float strength=0.0;
varying float inherited_alpha;
void vertex(){inherited_alpha=COLOR.a;}
void fragment(){
    vec2 delta=(UV-target_uv)*vec2(1920.0,1080.0);
    float spot=exp(-.5*dot(delta/vec2(155.0,225.0),delta/vec2(155.0,225.0)));
    COLOR=vec4(0.0,0.0,0.0,strength*.70*inherited_alpha);
}
