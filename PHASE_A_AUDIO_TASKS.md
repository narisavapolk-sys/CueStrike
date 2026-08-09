# 📊 Phase A Audio Completion - Task Checklist

## Overview
Complete the audio system by replacing synthetic placeholder clips with AAA-quality real audio files and assigning them to all systems.

---

## ✅ ALREADY COMPLETE
- [x] CueStrikeAudioManager.cs - Singleton with 14 clip slots
- [x] CueStrikeAudioGenerator.cs - Generates 9 procedural WAV placeholders
- [x] 9 placeholder WAV files generated in Assets/CueStrike/Audio/Clips/
- [x] BallSoundController.cs exists
- [x] PocketSoundDetector.cs exists  
- [x] NearMissDetector.cs exists
- [x] CueStrikeChampionshipCrowd.cs with gasp clips
- [x] CueStrikeDynamicPhysicsSFX.cs for physics-based audio
- [x] CharacterData has voiceClip and abilitySound fields
- [x] Character prefabs exist for all 10 characters

---

## 🎯 PHASE A AUDIO TASKS (Priority Order)

### 1. Source/Create Real Audio Clips (Week 1)

#### AudioManager Clips (14 needed)
- [ ] **hitSoft** - Gentle ball-ball collision
- [ ] **hitMedium** - Medium impact ball-ball collision
- [ ] **hitHard** - Hard impact ball-ball collision
- [ ] **cushionHit** - Ball hitting cushion/rail
- [ ] **pocketHit** - Ball dropping into pocket (thud)
- [ ] **pocketRollClip** - Ball rolling in wooden return track
- [ ] **nearMissGasp** - Crowd sharp intake of breath
- [ ] **ambientRoom** - Generic room tone (AC hum, air)
- [ ] **chalkDust** - Chalk scraping on cue tip
- [ ] **miscued** - Miscue sound (cue slipping)
- [ ] **ambientLoungeMusic** - Background jazz/lounge loop
- [ ] **whooshShot** - Power shot cue whoosh
- [ ] **menuClick** - UI button click
- [ ] **menuHover** - UI button hover

#### Character Voice & Ability Clips (20 needed - 10 chars × 2)
- [ ] Somchay - voiceClip
- [ ] Somchay - abilitySound
- [ ] MeiLing - voiceClip
- [ ] MeiLing - abilitySound
- [ ] Gentleman - voiceClip
- [ ] Gentleman - abilitySound
- [ ] PanPan - voiceClip
- [ ] PanPan - abilitySound
- [ ] Finn - voiceClip
- [ ] Finn - abilitySound
- [ ] KingFlex - voiceClip
- [ ] KingFlex - abilitySound
- [ ] Tusker - voiceClip
- [ ] Tusker - abilitySound
- [ ] Phantom - voiceClip
- [ ] Phantom - abilitySound
- [ ] Cassidy - voiceClip
- [ ] Cassidy - abilitySound
- [ ] Bones - voiceClip
- [ ] Bones - abilitySound

#### Room Ambience Clips (9 needed - one per AAA room)
- [ ] ZenDojo_ambient
- [ ] Cyberpunk_ambient
- [ ] Luxury_ambient
- [ ] Industrial_ambient
- [ ] SpaceNebula_ambient
- [ ] WarpFantasy_ambient
- [ ] GrandArena_ambient
- [ ] DAY_ambient
- [ ] NoirMemory_ambient

#### Crowd System Clips (for CueStrikeChampionshipCrowd)
- [ ] applause_1, applause_2, applause_3
- [ ] cheer_1, cheer_2
- [ ] gasp_1, gasp_2, gasp_3 (for near-miss)
- [ ] murmur_loop (ambient crowd)

### 2. Assign Clips in Unity Inspector (Week 1-2)

- [ ] Create CueStrikeAudioManager GameObject in each scene (or DontDestroyOnLoad)
- [ ] Drag all 14 AudioManager clips into Inspector
- [ ] Create CharacterData ScriptableObjects for each character
- [ ] Assign voiceClip and abilitySound to each CharacterData
- [ ] Assign CharacterData to each character prefab
- [ ] Assign room-specific ambientRoom clips per scene via RoomManager

### 3. Update Audio Scripts (if needed)

- [ ] Verify BallSoundController uses AudioManager clips correctly
- [ ] Verify PocketSoundDetector uses pocketHit + pocketRollClip
- [ ] Verify NearMissDetector uses nearMissGasp
- [ ] Verify CueStrikeChampionshipCrowd has gaspClips array populated
- [ ] Add per-room ambient support to RoomManager

### 4. Testing & Validation

- [ ] Play Mode: Test ball-ball collisions (soft/medium/hard)
- [ ] Play Mode: Test cushion hits
- [ ] Play Mode: Test pocket drops (thud + roll)
- [ ] Play Mode: Test near-miss gasp triggering
- [ ] Play Mode: Test chalk sound on cue prep
- [ ] Play Mode: Test miscue sound
- [ ] Play Mode: Test power shot whoosh
- [ ] Play Mode: Test UI click/hover sounds
- [ ] Play Mode: Test ambient room tone loops
- [ ] Play Mode: Test lounge music plays
- [ ] Play Mode: Test character voice lines
- [ ] Play Mode: Test character ability sounds
- [ ] Play Mode: Test crowd applause/cheer/gasp
- [ ] Verify spatial audio works in VR (3D positioning)
- [ ] Verify mute toggle works for all sounds
- [ ] Verify volume scaling works

### 5. Documentation Updates

- [ ] Update AUDIO_SYSTEM_PLAN.md with actual clip names used
- [ ] Update ROADMAP.md Phase A status
- [ ] Document audio file specs (sample rate, format, naming convention)

---

## 📁 File Structure Reference

```
Assets/CueStrike/Audio/
├── CueStrikeAudioManager.cs          ← Main manager (14 clip slots)
├── CueStrikeAudioGenerator.cs        ← Placeholder generator (Editor)
├── BallSoundController.cs            ← Per-ball audio
├── PocketSoundDetector.cs            ← Pocket trigger
├── NearMissDetector.cs               ← Near-miss gasp
├── CueStrikeChampionshipCrowd.cs     ← Crowd reactions
├── CueStrikeDynamicPhysicsSFX.cs     ← Physics-based SFX
└── Clips/                            ← PUT REAL .wav FILES HERE
    ├── hitSoft.wav
    ├── hitMedium.wav
    ├── hitHard.wav
    ├── cushionHit.wav
    ├── pocketHit.wav
    ├── pocketRollClip.wav
    ├── nearMissGasp.wav
    ├── ambientRoom.wav (generic)
    ├── chalkDust.wav
    ├── miscued.wav
    ├── ambientLoungeMusic.wav
    ├── whooshShot.wav
    ├── menuClick.wav
    ├── menuHover.wav
    ├── ZenDojo_ambient.wav
    ├── Cyberpunk_ambient.wav
    ├── Luxury_ambient.wav
    ├── Industrial_ambient.wav
    ├── SpaceNebula_ambient.wav
    ├── WarpFantasy_ambient.wav
    ├── GrandArena_ambient.wav
    ├── DAY_ambient.wav
    ├── NoirMemory_ambient.wav
    ├── applause_1.wav
    ├── applause_2.wav
    ├── applause_3.wav
    ├── cheer_1.wav
    ├── cheer_2.wav
    ├── gasp_1.wav
    ├── gasp_2.wav
    ├── gasp_3.wav
    ├── murmur_loop.wav
    └── CharacterVoice/
        ├── Somchay_voice.wav
        ├── Somchay_ability.wav
        ├── MeiLing_voice.wav
        ├── MeiLing_ability.wav
        ├── Gentleman_voice.wav
        ├── Gentleman_ability.wav
        ├── PanPan_voice.wav
        ├── PanPan_ability.wav
        ├── Finn_voice.wav
        ├── Finn_ability.wav
        ├── KingFlex_voice.wav
        ├── KingFlex_ability.wav
        ├── Tusker_voice.wav
        ├── Tusker_ability.wav
        ├── Phantom_voice.wav
        ├── Phantom_ability.wav
        ├── Cassidy_voice.wav
        ├── Cassidy_ability.wav
        ├── Bones_voice.wav
        └── Bones_ability.wav
```

---

## 🎯 AI Generation Prompts (for reference)

### Ball Collision
> "Extreme close-up, high-quality sound of two professional phenolic resin pool balls colliding, sharp 'clack', indoor pool hall acoustics, no background noise"

### Pocket Drop
> "Heavy pool ball dropping into a leather-lined pocket, muffled thud, followed by rolling in wooden track, realistic"

### Crowd Applause
> "Atmospheric crowd applause in a large luxury billiard hall, polite clapping, occasional hushed 'ooh' and 'aah', echoing space, VR 360 soundscape"

### Near-Miss Gasp
> "A crowd of spectators sharply inhaling in collective shock and disappointment, brief 'oohh' gasp after a near-miss shot, hushed murmuring, indoor arena"

---

## ⚠️ Technical Requirements

- Format: .wav, 44.1kHz, 16-bit
- SFX: Mono
- Ambient/Music: Stereo
- Loopable: ambientRoom, ambientLoungeMusic, murmur_loop, room ambiences
- Spatial: All SFX should work with Unity 3D Audio (spatialBlend = 1)

---

## 📝 Notes

- Placeholder clips already exist in Clips/ - replace them one by one
- Don't delete synth fallback system (CueStrikeRealisticAudioSynth.cs)
- Character prefabs already have AudioSource components
- Test on Meta Quest 2/3 for spatial audio verification