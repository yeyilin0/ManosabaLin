shader_type canvas_item;
render_mode unshaded, blend_add;
uniform sampler2D reunion_tex : source_color, filter_linear, repeat_disable;
uniform vec2 reunion_size = vec2(1024.0,1536.0);
uniform vec2 viewport_size = vec2(1920.0,1080.0);
uniform float clock = 0.0;
uniform float gain = 1.0;
float h21(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise2(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);return mix(mix(h21(i),h21(i+vec2(1,0)),f.x),mix(h21(i+vec2(0,1)),h21(i+1.0),f.x),f.y);}
void fragment(){
    // Fit the entire cutout, including its flowing hems, inside both viewport axes.
    float fit=min(viewport_size.x*.92/reunion_size.x,viewport_size.y*.94/reunion_size.y);
    vec2 image_size=reunion_size*fit;
    vec2 uv=(UV-vec2(.5))*viewport_size/image_size+vec2(.5);
    // A continuous deformation of one coherent embrace: no cross-fading extra hands.
    float settle=smoothstep(1.65,2.52,clock);
    float shoulders=exp(-pow((uv.y-.46)/.19,2.0));
    uv.x+=(uv.x-.5)*shoulders*.022*settle;
    uv.y+=shoulders*.004*sin(settle*3.14159);
    float tips=smoothstep(.30,.91,uv.y), edge=smoothstep(.15,.45,abs(uv.x-.5));
    uv.x+=sin(uv.y*8.0-clock*1.7)*.004*tips*(.3+edge);
    uv.y+=sin(uv.x*9.0-clock*.9)*.0018*tips;
    vec4 tex=texture(reunion_tex,clamp(uv,vec2(.001),vec2(.999)));
    float inside=step(0.0,uv.x)*step(uv.x,1.0)*step(0.0,uv.y)*step(uv.y,1.0);
    float lum=dot(tex.rgb,vec3(.2126,.7152,.0722));
    float n=noise2(uv*24.0+vec2(clock*.085,-clock*.14));
    float distance_from_hands=length((uv-vec2(.49,.53))*vec2(.9,.83));
    float reveal=smoothstep(1.25,2.05,clock)*.93;
    float front=1.0-smoothstep(reveal-.13,reveal+.055,distance_from_hands+(n-.5)*.16);
    float materialize=smoothstep(1.29,1.86,clock);
    float disperse=smoothstep(3.48,4.60,clock);
    float dissolve=1.0-smoothstep(1.0-disperse-.06,1.0-disperse+.12,n*.7+distance_from_hands*.5);
    // Use the same source-light exposure as Bad Ending's figures. The old
    // hue replacement and tone curve lifted dark fabric into a pale flat glow.
    float fine=noise2(uv*91.0+vec2(-clock*.15,clock*.32));
    float density=.86+.14*smoothstep(.25,.72,fine);
    vec3 pearl=vec3(1.0,.80,.89),garnet=vec3(.80,.24,.31);
    vec3 hue=mix(pearl,garnet,smoothstep(.44,.70,uv.x));
    float filaments=pow(.5+.5*sin(uv.y*105.0+n*14.0-clock*2.0),14.0);
    vec3 result=tex.rgb*(.45+lum*.24)*density+hue*filaments*.026;
    float alpha=tex.a*inside*front*materialize*dissolve;
    COLOR=vec4(result,alpha*gain);
}
