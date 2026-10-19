shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float wing_phase=0.0;
varying vec4 tint;
void vertex(){tint=COLOR;}
vec2 folded_uv(vec2 uv){
    // Fold each wing around the body without flattening the antennae.
    float side=step(.5,uv.x);
    float aperture=.22+.78*pow(.5+.5*cos(wing_phase+side*.16),.65);
    float dx=uv.x-.5;
    float body=smoothstep(.008,.035,abs(dx));
    uv.x=.5+dx/mix(1.0,aperture,body);
    uv.y+=abs(dx)*sin(wing_phase)*.11*body;
    return uv;
}
// TEXTURE 只注入 vertex()/fragment() 入口函数作用域，自定义函数内不可见，必须作参数传入。
float silhouette(sampler2D tex,vec2 uv){
    vec2 p=folded_uv(uv);
    if(p.x<0.0||p.x>1.0||p.y<0.0||p.y>1.0)return 0.0;
    return texture(tex,p).a;
}
void fragment(){
    float a=silhouette(TEXTURE,UV);
    float glow=0.0;
    for(int i=0;i<8;i++){
        float angle=float(i)*6.2831853/8.0;
        vec2 d=vec2(cos(angle),sin(angle))*TEXTURE_PIXEL_SIZE;
        glow+=silhouette(TEXTURE,UV+d*5.0)*.08;
        glow+=silhouette(TEXTURE,UV+d*12.0)*.035;
    }
    // Keep each magic's hue in both the ink contour and its surrounding glow.
    vec3 color=tint.rgb*(a*1.7+glow*1.8);
    float coverage=clamp(a+glow,0.0,1.0);
    // Additive blending applies alpha once; do not square the translucent membrane.
    COLOR=vec4(color/max(coverage,.0001),tint.a*coverage);
}
