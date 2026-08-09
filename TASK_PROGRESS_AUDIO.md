# Phase A Audio Completion - Task Progress

## Current Status: Phase A - Audio Assets (Week 1-2)

### ✅ COMPLETED
- [x] Audio system architecture analysis (CueStrikeAudioManager, BallSoundController, PocketSoundDetector, NearMissDetector, CueStrikeChampionshipCrowd)
- [x] CharacterData audio fields verified (voiceClip, abilitySound)
- [x] Placeholder clips exist (9 generated WAV files via CueStrikeAudioGenerator.cs)
- [x] PHASE_A_AUDIO_TASKS.md created with complete checklist
- [x] ROADMAP.md updated with current audio status

### 🔄 IN PROGRESS
- [ ] Source/Create real audio clips (14 AudioManager + 20 Character + 9 Room Ambience + 7 Crowd = ~50 clips)

### ⏳ PENDING
- [ ] Create CharacterData ScriptableObjects for all 10 playable characters
- [ ] Assign clips in Unity Inspector (AudioManager, CharacterData, CrowdSystem, RoomManager)
- [ ] Update audio scripts if needed (room ambience switching)
- [ ] Testing & validation in Play Mode
- [ ] Documentation updates

---

## Audio Clip Requirements Summary:

### AudioManager (14 clips - CueStrikeAudioManager.cs)
| Clip | Description | Spatial | Loop |
|------|-------------|---------|------|
| hitSoft | Soft ball-ball impact | 3D | No |
| hitMedium | Medium ball-ball impact | 3D | No |
| hitHard | Hard ball-ball impact | 3D | No |
| cushionHit | Ball hitting cushion | 3D | No |
| pocketHit | Ball dropping in pocket | 3D | No |
| pocketRollClip | Ball rolling in pocket | 3D | No |
| nearMissGasp | Near miss crowd gasp | 3D | No |
| ambientRoom | Room ambient loop | 2D | Yes |
| chalkDust | Chalk application | 3D | No |
| miscued | Cue miscue sound | 3D | No |
| ambientLoungeMusic | Lounge background music | 2D | Yes |
| whooshShot | Cue swing whoosh | 3D | No |
| menuClick | UI button click | 2D | No |
| menuHover | UI button hover | 2D | No |

### Character Voice + Ability (20 clips - CharacterData assets)
| Character | voiceClip | abilitySound |
|-----------|-----------|--------------|
| Somchay | Somchay_voice.wav | Somchay_ability.wav |
| MeiLing | MeiLing_voice.wav | MeiLing_ability.wav |
| Gentleman | Gentleman_voice.wav | Gentleman_ability.wav |
| PanPan | PanPan_voice.wav | PanPan_ability.wav |
| Finn | Finn_voice.wav | Finn_ability.wav |
| KingFlex | KingFlex_voice.wav | KingFlex_ability.wav |
| Tusker | Tusker_voice.wav | Tusker_ability.wav |
| Phantom | Phantom_voice.wav | Phantom_ability.wav |
| Cassidy | Cassidy_voice.wav | Cassidy_ability.wav |
| Bones | Bones_voice.wav | Bones_ability.wav |

### Room Ambience (9 clips - one per AAA room)
| Room | Ambience Clip | Loop |
|------|---------------|------|
| ZenDojo | ZenDojo_ambience.wav | Yes |
| Cyberpunk | Cyberpunk_ambience.wav | Yes |
| Luxury_DAY | Luxury_DAY_ambience.wav | Yes |
| Luxury_NIGHT | Luxury_NIGHT_ambience.wav | Yes |
| Industrial | Industrial_ambience.wav | Yes |
| SpaceNebula | SpaceNebula_ambience.wav | Yes |
| WarpFantasy | WarpFantasy_ambience.wav | Yes |
| GrandArena | GrandArena_ambience.wav | Yes |
| NoirMemory | NoirMemory_ambience.wav | Yes |

### Crowd System (7+ clips - CueStrikeChampionshipCrowd.cs)
| Clip | Description |
|------|-------------|
| crowdApplause | Applause on pot |
| crowdGroan | Groan on foul |
| crowdMurmur | Ambient murmur loop |
| crowdGaspClips[] | Array of gasp variations (3+) |

---

## Technical Specs:
- **Format:** .wav, 44.1kHz, 16-bit (or .ogg for Quest)
- **SFX:** Mono | **Ambient/Music:** Stereo
- **Loopable:** ambientRoom, ambientLoungeMusic, crowdMurmur, all room ambiences
- **Spatial:** All SFX with spatialBlend = 1 (3D)
- **Volume ranges:** SFX 0.7-1.0, Ambient 0.1-0.3, Music 0.3-0.5

---

## Immediate Next Steps (Priority Order):

### 1. Source/Create Audio Clips (REQUIRED FROM USER/ARTIST)
The project needs **~50 real audio files**. Options:
- **Record in-house** (recommended for character voices/abilities)
- **Purchase from asset stores** (AudioJungle, Unity Asset Store, Freesound.org)
- **AI-generated** (ElevenLabs for voices, Mubert/Suno for ambience)
- **Hire sound designer**

### 2. Create CharacterData ScriptableObjects
For each of the 10 playable characters:
```csharp
// Create in Assets/CueStrike/Characters/Resources/CharacterData/
CreateAssetMenu → CueStrike → Character Data
- characterName: "Somchay"
- voiceClip: [drag clip]
- abilitySound: [drag clip]
- characterPrefab: [assign after CharacterAAASetup runs]
- portrait: [assign]
- abilityControllerType: "SomchayAbilityController"
```

### 3. Set Up CueStrikeAudioManager in Scenes
- Add CueStrikeAudioManager to each room scene (or DontDestroyOnLoad)
- Assign all 14 clips in Inspector
- Test with: `CueStrikeAudioManager.Instance.PlayHitSoft(position)`

### 4. Assign Room Ambience
- Either via RoomManager (on scene load) or AudioManager
- Need to implement room ambience switching logic

### 5. Assign Crowd Clips
- Add CueStrikeChampionshipCrowd to GrandArena scene
- Assign applause, groan, murmur, gasp clips

---

## Testing Checklist:
- [ ] Play mode: Shoot ball → hitSoft/hitMedium/hitHard play based on velocity
- [ ] Ball hits cushion → cushionHit plays
- [ ] Ball pockets → pocketHit + pocketRollClip play
- [ ] Near miss → nearMissGasp plays
- [ ] Cue strike → whooshShot plays
- [ ] Chalk applied → chalkDust plays
- [ ] Miscue → miscued plays
- [ ] UI clicks → menuClick/menuHover play
- [ ] Room loads → correct ambience plays
- [ ] Character ability → abilitySound plays
- [ ] Character voice → voiceClip plays on events
- [ ] Crowd reacts to pot/foul/near-miss

---

## Notes:
- Placeholder clips in `Assets/CueStrike/Audio/Clips/` — replace one by one
- Keep synth fallback (`CueStrikeRealisticAudioSynth.cs`) for development
- Character prefabs already have AudioSource components (added by CharacterAAASetup)
- Test spatial audio on Meta Quest 2/3 for verification
- Consider Addressables for audio loading optimization later