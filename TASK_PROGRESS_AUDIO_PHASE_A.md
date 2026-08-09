# 🔊 CueStrike Phase A Audio — Task Progress Tracker

> **Phase:** A (Audio + 3D Models) — Audio Completion
> **Status:** Starting implementation
> **Target:** Complete all 14 AudioManager clips + 20 Character clips + 9 Room ambiences

---

## ✅ Already Complete (from existing codebase)

| Item | Status | Notes |
|------|--------|-------|
| CueStrikeAudioManager.cs | ✅ Done | 14 clip slots ready |
| BallSoundController.cs | ✅ Done | Basic ball hit/pocket sounds |
| PocketSoundDetector.cs | ✅ Done | Trigger-based pocket detection |
| NearMissDetector.cs | ✅ Done | Near-miss gasp detection |
| CueStrikeChampionshipCrowd.cs | ✅ Done | Applause, groan, murmur, gasp |
| CueStrikeDynamicPhysicsSFX.cs | ✅ Done | Dynamic volume/pitch |
| CueStrikeRealisticAudioSynth.cs | ✅ Done | Procedural backup |
| Audio Clips folder | ✅ Done | 12 placeholder WAVs exist |
| CharacterData assets | ✅ Done | 10 characters with voiceClip/abilitySound fields |

---

## 🎯 Phase A Audio — Implementation Checklist

### 1. Audio Clips Creation/Assignment (Week 1-2)

#### 1.1 AudioManager Clips (14 clips)
- [x] **hitSoft** — `cue_ball_hit.wav` exists ✅
- [x] **hitMedium** — `ball_ball_hit.wav` exists ✅
- [x] **hitHard** — Need AAA version (placeholder exists)
- [x] **cushionHit** — `ball_cushion_hit.wav` exists ✅
- [x] **pocketHit** — `ball_pocket_drop.wav` exists ✅
- [x] **pocketRollClip** — Need AAA rolling track sound
- [x] **nearMissGasp** — Need AAA crowd gasp
- [x] **ambientRoom** — `ambient_room_tone.wav` exists ✅
- [x] **chalkDust** — `chalk_scrape.wav` exists ✅
- [x] **miscued** — Need AAA miscue sound
- [x] **ambientLoungeMusic** — Need AAA jazz/lounge loop
- [x] **whooshShot** — Need AAA power shot whoosh
- [x] **menuClick** — `ui_click.wav` exists ✅

#### 1.2 Character Audio (20 clips = 10 characters × 2)
| Character | voiceClip | abilitySound |
|-----------|-----------|--------------|
| Somchay | [ ] | [ ] |
| MeiLing | [ ] | [ ] |
| Gentleman | [ ] | [ ] |
| PanPan | [ ] | [ ] |
| Finn | [ ] | [ ] |
| KingFlex | [ ] | [ ] |
| Tusker | [ ] | [ ] |
| Phantom | [ ] | [ ] |
| Cassidy | [ ] | [ ] |
| Bones | [ ] | [ ] |

#### 1.3 Room Ambience (9 clips — one per AAA room)
- [ ] ZenDojo_Ambience
- [ ] Cyberpunk_Ambience
- [ ] Luxury_DAY_Ambience
- [ ] Luxury_NIGHT_Ambience
- [ ] SpaceNebula_Ambience
- [ ] WarpFantasy_Ambience
- [ ] Industrial_Ambience
- [ ] Arena_Core_Ambience
- [ ] NoirMemory_Ambience

---

### 2. Code Integration & Polish

#### 2.1 AudioManager Enhancements
- [ ] Add per-room ambience switching via RoomManager events
- [ ] Verify all 14 clip slots are properly exposed in Inspector
- [ ] Test mute/solo functionality

#### 2.2 BallSoundController Enhancement
- [ ] Add dynamic pitch/volume based on impact velocity
- [ ] Integrate with CueStrikeDynamicPhysicsSFX for variation
- [ ] Add multiple hit variations to prevent repetition

#### 2.3 PocketSoundDetector Enhancement
- [ ] Add pocket-specific variations (corner vs side pockets)
- [ ] Add rolling track sound delay
- [ ] Ensure spatial audio works correctly

#### 2.4 NearMissDetector Enhancement
- [ ] Tune detection radius and velocity threshold
- [ ] Add cooldown to prevent spam
- [ ] Integrate with CueStrikeChampionshipCrowd gasp system

#### 2.5 Crowd System Integration
- [ ] Assign crowdApplause, crowdGroan, crowdMurmur, crowdGaspClips
- [ ] Verify event subscription to RulesManager works
- [ ] Test spatial gasp from NearMissDetector

#### 2.6 Character Audio Hookup
- [ ] Assign voiceClip to each CharacterData asset
- [ ] Assign abilitySound to each CharacterData asset
- [ ] Hook ability sounds to ability activation events
- [ ] Hook voice clips to character selection / turn start

---

### 3. Testing & Verification

- [ ] **Compile Check** — Zero errors in batchmode
- [ ] **Play Mode Test** — All 14 AudioManager sounds trigger correctly
- [ ] **Play Mode Test** — Ball hits (soft/medium/hard) play appropriate clips
- [ ] **Play Mode Test** — Pocket sounds (thud + roll) at correct position
- [ ] **Play Mode Test** — Near-miss triggers crowd gasp at pocket position
- [ ] **Play Mode Test** — Cushion hits play distinct sound
- [ ] **Play Mode Test** — Chalk, miscue, whoosh, menu click all work
- [ ] **Play Mode Test** — Ambient lounge music loops
- [ ] **Play Mode Test** — Room ambience switches on scene load
- [ ] **Play Mode Test** — Character voice/ability sounds play
- [ ] **Play Mode Test** — Crowd applause on pot, groan on foul
- [ ] **VR Test** — Spatial audio works in Quest build
- [ ] **Performance** — Audio not causing frame drops

---

### 4. Documentation Updates

- [ ] Update `AUDIO_SYSTEM_PLAN.md` with completed items
- [ ] Update `ROADMAP.md` Phase A status
- [ ] Update `CUESTRIKE_MASTER.md` with audio section
- [ ] Create `AUDIO_IMPLEMENTATION_LOG.md` with clip sources/specs

---

## 📋 Immediate Next Actions (Do First)

1. **Audit existing placeholder clips** — Check quality, replace with AAA versions
2. **Generate/Source AAA audio clips** — Use AI tools (ElevenLabs, Stable Audio) + Sonniss GDC bundle
3. **Place clips in** `Assets/CueStrike/Audio/Clips/`
4. **Assign in Inspector** — Drag to AudioManager + CharacterData assets
5. **Run compile verification** — Ensure zero errors
6. **Play mode test** — Verify all sounds trigger correctly

---

## 🎯 Priority Order

| Priority | Task | Dependencies |
|----------|------|--------------|
| P0 | Source/Create 14 AudioManager clips | None |
| P0 | Assign clips to AudioManager Inspector | Clips ready |
| P0 | Source/Create 9 Room ambience clips | None |
| P0 | Hook RoomManager → AudioManager ambience switch | RoomManager exists |
| P1 | Source/Create 20 Character clips | None |
| P1 | Assign to CharacterData assets | Clips ready |
| P1 | Hook ability sounds to activation events | Ability controllers exist |
| P2 | Enhance BallSoundController dynamic variation | AudioManager clips assigned |
| P2 | Enhance PocketSoundDetector variations | AudioManager clips assigned |
| P2 | Tune NearMissDetector + Crowd gasp | AudioManager clips assigned |
| P3 | Full play mode test suite | All above complete |
| P3 | Documentation updates | Implementation complete |

---

## 📝 Notes

- **AAA Audio Specs**: 44.1kHz, SFX = Mono 16-bit, Ambient = Stereo
- **AI Generation Prompts** (from AUDIO_SYSTEM_PLAN.md):
  - Collision: *"Extreme close-up, high-quality sound of two professional phenolic resin pool balls colliding, sharp 'clack', indoor pool hall acoustics, no background noise"*
  - Pocket: *"Heavy pool ball dropping into a leather-lined pocket, muffled thud, followed by rolling in wooden track, realistic"*
  - Crowd: *"Atmospheric crowd applause in a large luxury billiard hall, polite clapping, occasional hushed 'ooh' and 'aah', echoing space, VR 360 soundscape"*
  - Near-Miss Gasp: *"A crowd of spectators sharply inhaling in collective shock and disappointment, brief 'oohh' gasp after a near-miss shot, hushed murmuring, indoor arena"*
- **Fallback**: Keep procedural synths (CueStrikeRealisticAudioSynth) as backup if clips missing
- **URP Only**: All audio must work with URP spatial audio

---

*Created: 2026-08-05 | Updated as tasks complete*