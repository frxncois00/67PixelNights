"""Generate original, deterministic default game audio (standard library only)."""
import math
import random
import struct
import wave
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / 'Assets/Resources/Audio'
OUT.mkdir(parents=True, exist_ok=True)
RATE = 22050
TAU = math.tau

def save(name, samples):
    peak = max(abs(x) for x in samples) or 1
    gain = .85 / peak
    with wave.open(str(OUT / (name + '.wav')), 'wb') as f:
        f.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        f.writeframes(b''.join(struct.pack('<h', int(max(-1, min(1, x * gain)) * 32767)) for x in samples))

def hz(note):
    return 440 * 2 ** ((note - 69) / 12)

def music(dark):
    # Wrap note tails into the beginning for a seamless, 32-second loop.
    length = RATE * 32
    samples = [0.] * length
    chords = [[48,52,55], [45,48,52], [41,45,48], [43,47,50]] if not dark else [[45,48,52], [41,45,48], [44,47,50], [40,43,47]]
    def note(start, duration, pitch, level, pluck=False):
        freq = hz(pitch)
        for i in range(int(duration * RATE)):
            t = i / RATE
            env = min(1, t / .035) * min(1, (duration-t) / .45)
            env *= math.exp(-t * 2) if pluck else math.sin(math.pi*t/duration)**.7
            value = math.sin(TAU*freq*t) + .18*math.sin(TAU*freq*2*t)
            samples[(int(start*RATE)+i) % length] += value * env * level
    for bar in range(8):
        chord = chords[bar % 4]
        for p in chord:
            note(bar*4, 5, p, .12 if dark else .08)
        note(bar*4, 4.5, chord[0]-12, .16)
        for step in range(4 if dark else 8):
            note(bar*4+step*(1 if dark else .5), 1.8, chord[step%3]+12, .045 if dark else .13, True)
        if dark:
            note(bar*4+2, 2, chord[1]+13, .025)
    save('NightMusic' if dark else 'DayMusic', samples)

def effect(name, duration, kind):
    rng = random.Random(135)
    samples = []
    ember_noise = 0.
    for i in range(int(duration*RATE)):
        t = i/RATE
        noise = rng.uniform(-1,1)
        if kind == 'wood':
            v = .7*math.sin(TAU*150*t)*math.exp(-t*24) + noise*.4*math.exp(-t*50)
        elif kind == 'stone':
            v = sum(math.sin(TAU*f*t)*math.exp(-t*d) for f,d in [(1450,20),(2380,30),(3710,40)])*.25 + noise*.15*math.exp(-t*90)
        elif kind == 'pickup':
            v = math.sin(TAU*(800*t+650*t*t))*math.exp(-t*12)
        elif kind == 'refuel':
            # Hollow wood landing followed by a smooth, low fire whoosh.
            clunk = (.65*math.sin(TAU*180*t)+.25*math.sin(TAU*310*t))*math.exp(-t*32)
            # Smooth the noise so the flame sounds soft rather than crackly.
            ember_noise = .94*ember_noise + .06*noise
            flame_time = max(0,t-.07)
            whoosh = ember_noise*3*min(1,flame_time/.12)*math.exp(-flame_time*7)
            v = clunk + whoosh
        elif kind == 'upgrade':
            # Three ascending bell notes with overlapping tails.
            v = 0
            for start, pitch in [(0,72),(.14,76),(.28,79),(.42,84)]:
                local = t-start
                if local >= 0:
                    envelope = min(1,local/.008)*math.exp(-local*4)
                    v += .28*envelope*(math.sin(TAU*hz(pitch)*local)+.2*math.sin(TAU*hz(pitch)*2*local))
        elif kind == 'mob':
            # A brief crack followed by a descending, heavy body impact.
            crack = noise * .8 * math.exp(-t * 95)
            body = math.sin(TAU * (85*t + 105*.018*(1-math.exp(-t/.018)))) * math.exp(-t*24)
            grit = noise * .18 * math.exp(-t*32)
            v = crack + .75 * body + grit
        else:
            v = .6*math.sin(TAU*(110*t-55*t*t))*math.exp(-t*18)+noise*.4*math.exp(-t*24)
        samples.append(v*min(1,t/.002)*min(1,(duration-t)/.015))
    save(name, samples)

music(False)
music(True)
for name,duration,kind in [('WoodHit',.3,'wood'),('StoneHit',.4,'stone'),('EnemyHit',.22,'mob'),('PlayerHurt',.35,'impact'),('Pickup',.24,'pickup'),('CampfireRefuel',.7,'refuel'),('CampfireUpgrade',1.4,'upgrade')]:
    effect(name,duration,kind)
print('Generated nine default WAV clips in', OUT)
