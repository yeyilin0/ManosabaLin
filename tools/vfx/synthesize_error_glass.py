"""Original knife/blackboard friction and brittle glass sounds; reads no recordings."""
import argparse
from pathlib import Path
import subprocess
import wave
import numpy as np
from synthesize_magic_charge import filtered_noise

ROOT=Path(__file__).resolve().parents[2]
RATE=48000

def render(name,duration,rng):
    n=round(duration*RATE)
    t=np.arange(n)/RATE
    stereo=np.zeros((n,2))
    fade=np.clip(t/.008,0,1)*np.clip((duration-t)/.08,0,1)
    if name=='knife_scrape':
        # Dry scraping moves left to right; aperiodic catches interrupt the drag.
        friction=filtered_noise(rng,n,550,5200)
        pressure=np.full(n,.28)
        for start,length in [(0,.25),(.31,.22),(.62,.16),(.84,.32),(1.24,.24),(1.57,.33)]:
            u=np.clip((t-start)/length,0,1)
            window=(t>=start)&(t<start+length)
            pressure+=window*(.25+.55*np.sin(u*np.pi)**2)
        scrape=friction*pressure*.18
        for start in [.27,.59,.82,1.19,1.52,1.94]:
            age=np.maximum(t-start,0)
            scrape+=filtered_noise(rng,n,1800,8500)*(t>=start)*np.exp(-age*100)*.055
        pan=.18+.64*(t/duration)
        stereo[:,0]=scrape*np.sqrt(1-pan)
        stereo[:,1]=scrape*np.sqrt(pan)
    else:
        count={'local_crack':14,'spread':24,'glass_burst':32}[name]
        for i in range(count):
            start=rng.uniform(0,.055) if name=='local_crack' else rng.uniform(0,.75 if name=='spread' else .16)
            age=np.maximum(t-start,0)
            envelope=(t>=start)*(1-np.exp(-age*2200))*np.exp(-age*rng.uniform(22,65))
            brittle=filtered_noise(rng,n,1900,10000)*envelope*.035
            ring=np.zeros(n)
            for frequency in [1730,2710,4310,6190]:
                ring+=np.sin(2*np.pi*frequency*rng.uniform(.93,1.08)*age)*np.exp(-age*rng.uniform(10,24))
            brittle+=ring*(t>=start)*.006
            pan=rng.uniform(.12,.88)
            stereo[:,0]+=brittle*np.sqrt(1-pan)
            stereo[:,1]+=brittle*np.sqrt(pan)
        if name=='glass_burst':
            for channel in range(2):
                stereo[:,channel]+=filtered_noise(rng,n,650,5500)*np.exp(-t*7)*.07
    stereo*=fade[:,None]
    peak={'knife_scrape':.55,'local_crack':.72,'spread':.52,'glass_burst':.78}[name]
    stereo*=peak/max(np.max(np.abs(stereo)),1e-8)
    stereo[0]=0
    stereo[-1]=0
    assert np.isfinite(stereo).all() and np.max(np.abs(stereo))<.8
    return stereo

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--ffmpeg',type=Path,required=True)
    args=parser.parse_args()
    dest=ROOT/'ManosabaLin/audio/error_glass'
    dest.mkdir(parents=True,exist_ok=True)
    rng=np.random.default_rng(407142)
    for name,duration in [('knife_scrape',2.0),('local_crack',1.0),('spread',1.0),('glass_burst',1.15)]:
        data=render(name,duration,rng)
        wav=dest/(name+'.wav')
        with wave.open(str(wav),'wb') as stream:
            stream.setnchannels(2)
            stream.setsampwidth(2)
            stream.setframerate(RATE)
            stream.writeframes((data*32767).astype('<i2').tobytes())
        subprocess.run([str(args.ffmpeg),'-hide_banner','-loglevel','error','-y','-i',str(wav),
                        '-c:a','libvorbis','-q:a','6',str(dest/(name+'.ogg'))],check=True)
        print(name,duration)

if __name__=='__main__':
    main()
