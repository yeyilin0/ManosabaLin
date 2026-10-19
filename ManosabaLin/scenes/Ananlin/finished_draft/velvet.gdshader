shader_type canvas_item;
render_mode unshaded;
uniform float clock=0.0;
uniform float opacity=1.0;
void fragment(){
    vec2 uv=UV;
    // The lower hem is pulled first; tension travels upward to the suspended rail.
    float height=clamp(uv.y,0.0,1.0);
    float delay=.35+.48*(1.0-height);
    float pull=smoothstep(delay,2.3,clock);
    float reach=.035+.72*pow(height,.85);
    float edge=pull*reach;
    float settle=max(clock-2.0,0.0);
    float motion=sin(pull*3.14159)+sin(settle*3.8)*exp(-settle*1.35)*.16*pull;
    edge+=sin(height*4.5-clock*2.4)*.025*motion*height;
    float cloth_x=(uv.x-edge)/max(1.0-edge,.2);
    float cover=smoothstep(edge-.004,edge+.004,uv.x);
    // Move the painted weave, brocade and broad folds together, preserving their detail.
    vec2 sample_uv=vec2(cloth_x,uv.y);
    // Lift the lower fabric slightly toward the pull point; preserve the pinned top.
    sample_uv.y+=pull*.085*height*height*(1.0-cloth_x);
    sample_uv.x+=sin(height*6.0-clock*2.0+cloth_x*3.0)*.012*motion*height;
    sample_uv.y+=sin(cloth_x*8.0-clock*1.8)*.008*motion*height;
    // Low amplitude settling travels down the hanging cloth, never moves its pinned rail.
    sample_uv.x+=sin(height*8.0-clock*.7)*.0018*pull*height;
    vec4 cloth=texture(TEXTURE,clamp(sample_uv,vec2(.001),vec2(.999)));
    float edge_shadow=1.0-.18*exp(-abs(uv.x-edge)*100.0);
    vec3 lit_cloth=clamp(cloth.rgb*3.10+vec3(.045,.035,.065),vec3(0.0),vec3(1.0));
    float folded_light=.94+.06*cos(cloth_x*34.0+height*2.0);
    float lip=exp(-abs(uv.x-edge)*115.0)*pull;
    float sheen=pow(max(cos(cloth_x*13.0-height*2.2+motion*.35),0.0),5.0);
    vec3 velvet=lit_cloth*edge_shadow*folded_light;
    velvet+=vec3(.17,.13,.31)*sheen*.20+vec3(.27,.22,.45)*lip*.25;
    COLOR=vec4(velvet,cloth.a*opacity*smoothstep(0.0,.4,clock)*cover*.96);
}
