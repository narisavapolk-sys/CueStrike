import os

files = [
    'Assets/CueStrike/Materials/AAA/FBX/LuxuryChandelier/Bulb_2.mat.meta',
    'Assets/CueStrike/Materials/AAA/FBX/LuxuryChandelier/Crystal_2.mat.meta',
    'Assets/CueStrike/Materials/AAA/FBX/LuxuryChandelier/Gold_2.mat.meta',
    'Assets/CueStrike/Materials/AAA/FBX/NeonSign_Strike/NeonBlue_2.mat.meta',
    'Assets/CueStrike/Materials/AAA/FBX/NeonSign_Strike/NeonFrame_2.mat.meta',
    'Assets/CueStrike/Materials/AAA/FBX/NeonSign_Strike/NeonPink_2.mat.meta',
    'Assets/CueStrike/Materials/AAA/FBX/SpaceConsole/ConsoleButton_2.mat.meta',
    'Assets/CueStrike/Materials/AAA/FBX/SpaceConsole/ConsoleDark_2.mat.meta',
    'Assets/CueStrike/Materials/AAA/FBX/SpaceConsole/ConsoleHull_2.mat.meta',
    'Assets/CueStrike/Materials/AAA/FBX/SpaceConsole/HoloScreen_2.mat.meta',
]

for p in files:
    with open(p, 'rb') as fh:
        raw = fh.read()
    print('===', p, 'SIZE:', len(raw))
    print(repr(raw[:200]))