# Task: CueStrike VR - Phase A Audio Completion & Next Phases

**Objective:** Complete Phase A Audio (source ~50 real audio clips, assign, test), then proceed to Phase B (P9 Animator + BoPanda Banter) and Phase C (Playability Polish).

---

## 🎯 CURRENT PRIORITY: PHASE A AUDIO COMPLETION (Week 1-2)

### ✅ COMPLETED - Audio Architecture & Setup
- [x] Audio system architecture analysis complete
- [x] CueStrikeAudioManager reviewed (14 clip slots)
- [x] BallSoundController reviewed (velocity-based hit sounds)
- [x] PocketSoundDetector reviewed (trigger-based pocket sounds)
- [x] NearMissDetector reviewed (near-miss gasp detection)
- [x] CueStrikeChampionshipCrowd reviewed (crowd reactions)
- [x] CharacterData audio fields verified (voiceClip, abilitySound)
- [x] Placeholder clips generated (9 WAV files via CueStrikeAudioGenerator.cs)
- [x] ROADMAP.md updated with current audio status
- [x] TASK_PROGRESS_AUDIO.md created with detailed requirements
- [x] NEXT_STEPS_SUMMARY.md created with recommendations
- [x] TASK_PROGRESS.md master tracker created

### 🔄 IN PROGRESS - Source Real Audio Clips (~50 total)
- [ ] **14 AudioManager clips** (hitSoft, hitMedium, hitHard, cushionHit, pocketHit, pocketRollClip, nearMissGasp, ambientRoom, chalkDust, miscued, ambientLoungeMusic, whooshShot, menuClick, menuHover)
- [ ] **20 Character clips** (10 chars × voiceClip + abilitySound)
- [ ] **9 Room Ambience clips** (one per AAA room)
- [ ] **7+ Crowd clips** (applause, groan, murmur loop, gasp variations)

### ⏳ PENDING - After Clips Sourced
- [ ] Create 10 CharacterData ScriptableObjects
- [ ] Assign all clips in Unity Inspector (AudioManager, CharacterData, CrowdSystem, RoomManager)
- [ ] Set up CueStrikeAudioManager in all room scenes (or DontDestroyOnLoad)
- [ ] Implement room ambience switching logic
- [ ] Play mode testing & validation
- [ ] Documentation updates

---

## 🎯 PHASE B: P9 ANIMATOR + BOPANDA BANTER (Week 2-3)

### P9 Animator Controller
- [ ] Create Animator Controller asset
- [ ] Add 9 states: Idle, Walk, Aim, Shoot, Celebrate, Disappointed, Speak, Neutral, Victory
- [ ] Create transitions with parameters
- [ ] Assign to all 10 character prefabs

### BoPanda Banter System
- [ ] Design banter event system
- [ ] Implement frame start/end comments
- [ ] Implement pot success reactions
- [ ] Implement foul callouts
- [ ] Implement century break hype
- [ ] Implement snooker escape commentary
- [ ] Implement fluke teasing
- [ ] Implement near miss gasps

### Uncle Nok Referee Integration
- [ ] Hook UncleNokReferee.cs to ShotManager events
- [ ] Hook UncleNokReferee.cs to RulesManager events
- [ ] Add voice announcement clips
- [ ] Test in play mode

### Crowd Reactions
- [ ] Connect CueStrikeCrowdSystem to game events
- [ ] Test spatial audio in GrandArena
- [ ] Verify 84 spectator performance

---

## 🎮 PHASE C: PLAYABILITY POLISH (Week 3-4)

### Shot Manager Polish
- [ ] Fine-tune cue aiming
- [ ] Fine-tune power charge
- [ ] Fine-tune spin (english)
- [ ] Fine-tune preview line

### Rules System
- [ ] Complete 8-ball rules
- [ ] Complete 9-ball rules
- [ ] Complete Snooker rules
- [ ] Foul detection
- [ ] Ball-in-hand logic
- [ ] Turn switching

### Multiplayer
- [ ] Photon/Fusion integration
- [ ] Turn-based sync
- [ ] Voice chat (CueStrikeVoiceManager exists)

### UI/HUD
- [ ] Finalize CueStrikeHUD.cs
- [ ] Finalize MainMenu
- [ ] Finalize LobbyUI
- [ ] Finalize NoirMemory results

### Character Abilities
- [ ] Balance 10 unique abilities
- [ ] Set cooldowns
- [ ] Add visual FX
- [ ] Add audio cues

---

## 📦 PARALLEL: CHARACTER PREFABS & ANIMATIONS

### Ready to Run (CharacterAAASetup.cs)
- [ ] Run "Setup All AAA Characters" menu item
- [ ] Verify 12 prefabs created
- [ ] Verify Humanoid rig on all
- [ ] Verify URP/Lit materials
- [ ] Verify IK targets (LeftHand/RightHand)

### Need Animation Clips
- [ ] Create/acquire P9 animation clips
- [ ] Assign to Animator Controller
- [ ] Test IK with cue interaction
- [ ] Add LOD groups

---

## 🛠️ IMMEDIATE COMMANDS (Run in Order)

```bash
# 1. Blender: Generate all AAA assets
# Open Blender 3.6 → Scripting → Paste create_all_aaa_master.py → Run ▶

# 2. Unity: Import & Configure All AAA
# Tools → CueStrike → Apply → Apply ALL AAA (Final Polish)

# 3. Verify Self-Test
# Check console for "ALL PASSED ✅"

# 4. Fix Pink Materials (if any)
# Tools → CueStrike → Fix → Fix Pink Materials (URP Conversion)

# 5. Generate Placeholder Audio
# Tools → CueStrike → Generate → Create Placeholder Audio

# 6. Setup Character Prefabs
# Tools → CueStrike → Character System → Setup All AAA Characters
```

---

## 📁 KEY REFERENCE FILES

| File | Purpose |
|------|---------|
| `TASK_PROGRESS_AUDIO.md` | Detailed audio task tracking |
| `NEXT_STEPS_SUMMARY.md` | Strategic overview & recommendations |
| `ROADMAP.md` | Phase breakdown with checkboxes |
| `CHARACTER_SYSTEM_PLAN.md` | 12 character roster & abilities |
| `AUDIO_SYSTEM_PLAN.md` | Audio architecture |
| `AAA_WORLD_TOUR_IMPLEMENTATION.md` | Room decoration plan |

---

## 💡 STRATEGIC FOCUS

**NOW:** Phase A Audio - Source ~50 clips, assign, test
**NEXT:** Phase B Animator - P9 controller + BoPanda banter + Uncle Nok
**THEN:** Phase C Playability - Rules, multiplayer, UI, abilities

**The foundation is solid. The pipeline works. Now it's content + polish time! 🎱**

---

## ⚠️ LEGACY TASK (From Previous task.md - COMPLETED)

The original task.md items (Pink Material Fix + IK Posture Assist) have been **completed**:
- ✅ Pink Materials fixed via `CueStrikeAAAApplyAll.cs` (`FixPinkMaterialsMenu`) → URP/Lit conversion
- ✅ IK Posture Assist implemented via `CharacterIKAssist.cs` + `StanceReferenceData.cs`
- ✅ Editor automation via `CharacterAAASetup.cs` + `CueStrikeAAAApplyAll.cs`
- ✅ Self-test via `CharacterAAASelfTest.cs`
- ✅ All verified via batchmode compile (0 errors)