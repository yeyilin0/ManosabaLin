"""Original fireball flare + forward magical jet, timed to 3.75–4.08s.

No reference recording is read. Produces a short hot pressure flare followed by
a wide airy flame rush and a faint crystalline edge; not a sustained bass/rev tone.
"""
import argparse
from pathlib import Path
import subprocess
import wave

import numpy as np
from synthesize_magic_charge import filtered_noise

ROOT=Path(__file__).resolve().parents[2]
RATE=48000
DURATION=.33


def synthesize():
    rng=np.random.default_rng(641703)
    count=round(RATE*DURATION)
    t=np.arange(count)/RATE
    burst=(1-np.exp(-t*850))*np.exp(-t*38)
    jet_age=np.maximum(t-.033,0)
    jet=(1-np.exp(-jet_age*70))*np.exp(-jet_age*5.5)
    end=np.clip((DURATION-t)/.065,0,1)
    stereo=np.zeros((count,2))

    # Broad, warm pressure transient: its low component decays before the jet.
    warm=filtered_noise(rng,count,130,2500)*burst*.18
    flare=filtered_noise(rng,count,900,5800)*burst*.09
    body=np.sin(2*np.pi*82*t)*burst*.055
    mono=warm+flare+body
    # Short, irregular flame microfractures live only at the eruption, not a rhythm.
    for _ in range(7):
        at=rng.uniform(.007,.065)
        age=np.maximum(t-at,0)
        click=(t>=at)*(1-np.exp(-age*1400))*np.exp(-age*250)
        mono+=filtered_noise(rng,count,1400,7300)*click*.018

    pan=.15+.7*np.clip((t-.04)/.22,0,1)
    for channel in range(2):
        air=filtered_noise(rng,count,800,6400)
        flame=filtered_noise(rng,count,330,2200)
        stream=(air*.10+flame*.075)*jet
        stereo[:,channel]+=mono*.72
        stereo[:,channel]+=stream*np.sqrt(pan if channel==1 else 1-pan)
    # A brief magical shimmer accompanies the projectile without engine-like glide.
    shimmer=(np.sin(2*np.pi*1760*t)+.45*np.sin(2*np.pi*2637.02*t))*jet*.012
    stereo+=shimmer[:,None]
    dry=stereo.copy()
    delay=round(.027*RATE)
    stereo[delay:]+=dry[:-delay,::-1]*.13
    stereo*=end[:,None]**2
    stereo*=.78/max(float(np.max(np.abs(stereo))),1e-8)
    stereo[0]=0
    stereo[-1]=0
    assert np.isfinite(stereo).all() and np.max(np.abs(stereo))<=.781
    return stereo


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--ffmpeg',type=Path,required=True)
    args=parser.parse_args()
    dest=ROOT/'ManosabaLin/audio/explosion_magic'
    dest.mkdir(parents=True,exist_ok=True)
    sound=synthesize()
    wav=dest/'fireball_launch_synth.wav'
    with wave.open(str(wav),'wb') as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes((sound*32767).astype('<i2').tobytes())
    subprocess.run([str(args.ffmpeg),'-hide_banner','-loglevel','error','-y','-i',str(wav),
                    '-c:a','libvorbis','-q:a','6',str(dest/'fireball_launch_synth.ogg')],check=True)
    print(f'Original synthetic fireball launch: {DURATION:.2f}s, stereo 48kHz, peak=.780')


if __name__=='__main__':
    main()
