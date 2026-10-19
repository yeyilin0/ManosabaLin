"""Original glass fracture topology. Reference image is not loaded or copied.

Unequal Voronoi fragments form a connected planar crack network; kinked edges,
parallel splinter veins and short branches provide brittle-glass detail.
"""
import json
import math
from pathlib import Path
import random

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT/'ManosabaLin/scenes/Hiro/error_glass'
W, H = 1920, 1080
rng = random.Random(604136)


def clip(poly, normal, offset):
    result = []
    for a, b in zip(poly, poly[1:]+poly[:1]):
        da = a[0]*normal[0]+a[1]*normal[1]-offset
        db = b[0]*normal[0]+b[1]*normal[1]-offset
        if da <= 0:
            result.append(a)
        if (da <= 0) != (db <= 0):
            u = da/(da-db)
            result.append([a[0]+(b[0]-a[0])*u, a[1]+(b[1]-a[1])*u])
    return result


def distance_to_cut(p):
    # Blade traverses the whole screen: upper-left to lower-right.
    return abs(p[0]-(160+p[1]*1.48)) / math.sqrt(1+1.48**2)


def build():
    seeds = [[rng.uniform(0,W),rng.uniform(0,H)] for _ in range(42)]
    stress=[]
    for _ in range(7):
        y=rng.uniform(40,1040)
        stress.append([160+y*1.48+rng.uniform(-150,150),y,rng.uniform(120,310)])
    for _ in range(22):
        cx,cy,radius=rng.choice(stress)
        seeds.append([max(0,min(W,cx+rng.uniform(-radius*.6,radius*.6))),
                      max(0,min(H,cy+rng.uniform(-radius*.6,radius*.6)))])
    cells=[]
    edges={}
    for index,p in enumerate(seeds):
        poly=[[0,0],[W,0],[W,H],[0,H]]
        for q in seeds:
            if q is p:
                continue
            normal=[q[0]-p[0],q[1]-p[1]]
            offset=(q[0]**2+q[1]**2-p[0]**2-p[1]**2)/2
            poly=clip(poly,normal,offset)
            if not poly:
                break
        if len(poly)<3:
            continue
        cells.append({'points':poly,'center':p,'spin':rng.uniform(-2.4,2.4)})
        for a,b in zip(poly,poly[1:]+poly[:1]):
            key=tuple(sorted([(round(a[0],2),round(a[1],2)),(round(b[0],2),round(b[1],2))]))
            if key in edges or math.dist(a,b)<8:
                continue
            # Screen frame itself is not a crack.
            if any(abs(a[k]-v)<.01 and abs(b[k]-v)<.01 for k,v in [(0,0),(0,W),(1,0),(1,H)]):
                continue
            length=math.dist(a,b)
            nx,ny=-(b[1]-a[1])/length,(b[0]-a[0])/length
            points=[a]
            for j in range(1,max(2,int(length/38))):
                u=j/max(2,int(length/38))
                kink=rng.uniform(-4,4)*math.sin(u*math.pi)
                points.append([a[0]+(b[0]-a[0])*u+nx*kink,a[1]+(b[1]-a[1])*u+ny*kink])
            points.append(b)
            midpoint=[(a[0]+b[0])/2,(a[1]+b[1])/2]
            local=any(((midpoint[0]-cx)/radius)**2+((midpoint[1]-cy)/(radius*rng.uniform(.55,1.2)))**2<1 for cx,cy,radius in stress)
            edges[key]={'points':points,'distance':distance_to_cut(midpoint),'local':local,
                        'width':rng.uniform(.7,2.0),'seed':rng.random(), 'veins':[]}
            for _ in range(max(1,int(length/75))):
                u=rng.uniform(.12,.85)
                start=[a[0]+(b[0]-a[0])*u,a[1]+(b[1]-a[1])*u]
                direction=rng.choice([-1,1])
                reach=rng.uniform(12,55)
                split=[start[0]+nx*reach*direction+(b[0]-a[0])*.05,
                       start[1]+ny*reach*direction+(b[1]-a[1])*.05]
                tip=[split[0]+nx*reach*.25*direction,split[1]+ny*reach*.25*direction]
                edges[key]['veins'].append([start,split,tip])
            # Bundled splinters follow main cracks and rejoin them: glass-like
            # branching double edges rather than a clean cellular/tile outline.
            for end,sign in [(a,1),(b,-1)]:
                tx,ty=(b[0]-a[0])/length*sign,(b[1]-a[1])/length*sign
                for _ in range(rng.randint(3,7)):
                    reach=min(length*.6,rng.uniform(20,85))
                    offset=rng.uniform(-11,11)
                    mid=[end[0]+tx*reach*.48+nx*offset,end[1]+ty*reach*.48+ny*offset]
                    tip=[end[0]+tx*reach,end[1]+ty*reach]
                    edges[key]['veins'].append([end,mid,tip])
    # Irregular cumulative knife timing creates friction, catches, then short slips.
    cut=[]
    durations=[]
    for i in range(47):
        y=-35+i/46*(H+70)
        x=160+y*1.48+math.sin(i*.91)*8+rng.uniform(-7,7)
        if i%9==5:
            x+=rng.uniform(10,24)
        cut.append([x,y])
        durations.append(rng.uniform(.25,1.3)*(2.6 if i%9==5 else 1))
    total=sum(durations[:-1])
    times=[0]
    for duration in durations[:-1]:
        times.append(times[-1]+duration/total*2)
    data={'size':[W,H],'cut':cut,'cut_times':times,'edges':list(edges.values()),'cells':cells}
    DEST.mkdir(parents=True,exist_ok=True)
    (DEST/'fracture.json').write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
    # Procedural pattern preview only; same topology as the actual effect.
    from PIL import Image,ImageDraw
    preview=Image.new('RGB',(W,H),(8,8,12))
    draw=ImageDraw.Draw(preview)
    for edge in data['edges']:
        points=[tuple(p) for p in edge['points']]
        draw.line(points,fill=(94,41,45),width=7)
        draw.line(points,fill=(204,102,102),width=3)
        draw.line(points,fill=(249,224,224),width=1)
        for vein in edge['veins']:
            draw.line([tuple(p) for p in vein],fill=(176,158,164),width=1)
    draw.line([tuple(p) for p in cut],fill=(255,235,235),width=3)
    preview_path=ROOT/'.local/error_glass_preview/pattern.png'
    preview_path.parent.mkdir(parents=True,exist_ok=True)
    preview.save(preview_path)
    assert len(data['edges'])>100 and len(cells)>40
    print(f'Generated {len(cells)} unequal fragments, {len(edges)} connected cracks, original splinter veins.')


if __name__=='__main__':
    build()
