"""Original theater SFX: velvet pull, airy butterfly flutter, magical written impact."""
from pathlib import Path
import wave
import numpy as np

OUT=Path(__file__).resolve().parents[2]/'ManosabaLin/scenes/Ananlin/finished_draft'
SR=44100
rng=np.random.default_rng(72103)

def noise(n,low,high):
    f=np.fft.rfftfreq(n,1/SR)
    band=(1-np.exp(-(f/low)**4))*np.exp(-(f/high)**4)
    return np.fft.irfft(np.fft.rfft(rng.normal(size=n))*band,n=n)

def save(name,left,right):
    stereo=np.column_stack((left,right))
    t=np.arange(len(stereo))/SR
    edge=np.clip(t/.003,0,1)*np.clip((t[-1]-t)/.008,0,1)
    stereo*=edge[:,None]
    stereo=np.tanh(stereo)
    # Only reduce peaks; retain intentional relative loudness among these sounds.
    stereo*=min(1.0,.84/max(np.max(np.abs(stereo)),1e-6))
    with wave.open(str(OUT/name),'wb') as f:
        f.setnchannels(2);f.setsampwidth(2);f.setframerate(SR)
        f.writeframes((stereo*32767).astype('<i2').tobytes())

t=np.arange(int(1.95*SR))/SR
env=np.clip(t/.12,0,1)*np.clip((1.95-t)/.3,0,1)
# Broad soft cloth friction, with uneven pulls and tiny creases; no mechanical hum.
pressure=.5+.19*np.sin(t*4.3)+.1*np.sin(t*11.7+1)+.04*np.sin(t*35)
cloth=noise(len(t),380,4300)*env*pressure*.23
creases=np.zeros_like(t)
for onset in [.12,.37,.71,1.02,1.38,1.67]:
    creases+=noise(len(t),1300,6500)*np.exp(-((t-onset)/.025)**2)*.035
pan=np.clip(t/1.95,0,1)
save('curtain_pull.wav',(cloth+creases)*(1-.22*pan),(cloth+creases)*(.78+.22*pan))

t=np.arange(int(2.6*SR))/SR
flutter=np.zeros_like(t)
for onset in np.arange(.07,2.5,.16):
    onset+=rng.uniform(-.025,.025)
    pulse=np.exp(-((t-onset)/rng.uniform(.014,.022))**2)
    flutter+=noise(len(t),1900,7500)*pulse*rng.uniform(.07,.11)
flutter*=np.clip(t/.05,0,1)*np.clip((2.6-t)/.12,0,1)
save('theater_butterflies.wav',flutter,flutter*.88+noise(len(t),2200,7200)*.006)

t=np.arange(int(.68*SR))/SR
# Crisp paper stroke and a short magical knock, followed by delicate resonant glints.
scratch=noise(len(t),1400,6800)*np.exp(-t*12)*.20
knock=np.sin(2*np.pi*(620*t-240*t*t))*np.exp(-t*38)*.16
glints=np.zeros_like(t)
for freq,amp in [(1760,.045),(2640,.024),(3520,.012)]:
    glints+=np.sin(2*np.pi*freq*t)*np.exp(-t*7)*amp
fade=np.clip(t/.004,0,1)*np.clip((.68-t)/.10,0,1)
hit=(scratch+knock+glints)*fade
save('written_impact.wav',hit,hit*.92)
print('Generated original curtain pull (1.95s), butterflies (2.6s), written impact (.68s).')
