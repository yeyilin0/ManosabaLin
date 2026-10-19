shader_type canvas_item;
render_mode unshaded, blend_add;
uniform vec2 viewport_size=vec2(1920.0,1080.0);
uniform float clock=0.0;
uniform float gain=1.0;
float bell(float d,float w){return exp(-d*d/(w*w));}
void fragment(){
    vec2 p=(UV-vec2(.5,.59))*vec2(viewport_size.x/viewport_size.y,1.0);
    vec3 rgb=vec3(0.0);
    float t=clock-3.08;
    for(int i=0;i<5;i++){
        float k=float(i),age=t-k*.145;
        if(age>0.0){
            float rot=k*.27-.17;
            vec2 q=mat2(vec2(cos(rot),sin(rot)),vec2(-sin(rot),cos(rot)))*p;
            q.y/=mix(.30,.88,k/4.0);
            float a=atan(q.y,q.x),r=length(q);
            float radius=.06+age*(.64+k*.055);
            float d=r-radius-.013*sin(a*5.0+age*2.0+k)-.006*sin(a*11.0-k);
            // Outside a ring's visible halo all four exponentials would be negligible.
            // Skip them coherently, particularly after a wave has left the viewport.
            if(abs(d)<.14 && age<1.80){
            float gate=.25+.75*smoothstep(-.30,.65,sin(a*2.0+k*1.7)+sin(a*5.0-k)*.30);
            float fade=smoothstep(0.0,.15,age)*(1.0-smoothstep(.90,1.80,age));
            float width=.012+.005*sin(a*3.0+k);
            vec3 hue=mix(vec3(.80,.28,.38),vec3(1.0,.80,.88),k/5.0);
            rgb+=hue*(bell(d,width)*.22+bell(d,.045)*.065)*gate*fade;
            rgb+=vec3(1.0,.92,.97)*bell(d+.002,.0027)*gate*fade*.65;
            rgb+=vec3(.50,.48,1.0)*bell(d-.008,.0028)*gate*fade*.12;
            rgb+=vec3(1.0,.52,.29)*bell(d+.010,.003)*gate*fade*.10;
            }
        }
    }
    // Two short localized contacts, not an opaque fullscreen white flash.
    float first=exp(-pow((clock-2.45)/.065,2.0));
    float second=exp(-pow((clock-3.05)/.085,2.0));
    if(first>.001) rgb+=vec3(1.0,.65,.77)*exp(-length(p-vec2(-.025,-.05))*12.0)*first*.48;
    if(second>.001) rgb+=vec3(1.0,.94,.94)*exp(-length(p-vec2(.075,-.13))*10.0)*second*.65;
    COLOR=vec4(vec3(1.0)-exp(-rgb),gain);
}
