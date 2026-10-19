"""Original soft wing flutter and dry felt-marker friction; no sampled video audio."""
from pathlib import Path
import wave
import numpy as np

out=Path(__file__).resolve().parents[2]/'ManosabaLin/scenes/Ananlin/draft_butterflies'
sr=44100
rng=np.random.default_rng(1847)
def save(name,signal):
    signal=np.tanh(signal)
    stereo=np.column_stack((signal,np.roll(signal,23)*.94))
    with wave.open(str(out/name),'wb') as f:
        f.setnchannels(2); f.setsampwidth(2); f.setframerate(sr)
        f.writeframes((stereo*26000).astype('<i2').tobytes())
def filtered(n,width):
    noise=rng.normal(size=n)
    return np.convolve(noise,np.ones(width)/width,mode='same')
def airy(n,low,high):
    # Smooth band shaping removes the low rumble without a harsh treble shelf.
    freq=np.fft.rfftfreq(n,1/sr)
    shape=(1-np.exp(-(freq/low)**4))*np.exp(-(freq/high)**4)
    return np.fft.irfft(np.fft.rfft(rng.normal(size=n))*shape,n=n)
t=np.arange(int(sr*2.4))/sr
flutter=np.zeros_like(t)
for onset in np.arange(.08,2.3,.14):
    age=t-onset
    envelope=np.exp(-((age-.024)/.016)**2)
    flutter+=envelope*airy(len(t),1700,7200)*.105
flutter*=np.minimum(t/.04,1)*np.minimum((2.4-t)/.18,1)
save('butterfly_flight.wav',flutter)
t=np.arange(int(sr*1.45))/sr
# Light felt-nib texture: retain fine paper grain, remove heavy rubbing frequencies.
grain=airy(len(t),1200,6200)
pressure=.5+.12*np.sin(t*17)+.08*np.sin(t*41+1)+.06*np.sin(t*79)
env=np.clip(t/.04,0,1)*np.clip((1.45-t)/.16,0,1)
save('marker_on_paper.wav',grain*pressure*env*.20)
print('Generated original stereo wing flutter and felt-marker paper friction.')
