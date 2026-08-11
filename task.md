# Task: CueStrike VR — R24 First-Time Tutorial (Current) → R25–R30 Roadmap

**Current Objective (coach-approved, 2026-08-11):** เปิดเกม (Boot) → Title (Lobby) → ผู้เล่นครั้งแรก
ต้องผ่าน Tutorial สอนจับไม้คิว/เล็ง/ยิง ก่อนเข้าเมนู; เคยเล่นแล้ว / กด Skip → เข้า Lobby ทันที

## 🎯 R24 — FIRST-TIME TUTORIAL (IN PROGRESS → Ready for test)

### ✅ Done
- [x] ตรวจระบบเดิม: `CueStrikeTutorialManager` เป็น in-match validation (ต้องมีโต๊ะ/ShotManager) — ไม่เหมาะกับ Lobby onboarding
- [x] สร้าง `CueStrikeFirstTimeFlow.cs` — PlayerPrefs first-time flag + 3 สไลด์ภาษาไทย + Skip + fail-safe auto-UI
- [x] ผูก component เข้า `Title_NoksGrandHall.unity` (GameObject `FirstTimeTutorial`)
- [x] Editor tool `FirstTimeTutorialSetup.cs` — idempotent + guard 3 ชั้น + self-test + batchmode entry
- [x] Compile verify: batchmode **0 errors** + scene load 0 errors
- [x] Docs: `CUESTRIKE_MASTER.md`, `TASK_PROGRESS.md`, `implementation_plan_r24_title_tutorial.md`

### 🔄 To verify in Editor (Vision audit)
- [ ] เปิด Title scene → Enter Play Mode → สไลด์ 1 ปรากฏ (ครั้งแรก)
- [ ] กด ถัดไป ×3 → เริ่มเล่น → ครั้งต่อไปไม่โชว์ (PlayerPrefs)
- [ ] ResetTutorialFlag → โชว์อีกครั้ง; ปุ่ม ข้าม → เข้า Lobby ทันที

## ⏭️ NEXT (R25–R30, per coach)
- **R25** จบเกม Best-of Flow: Single Frame / Best of 3/5/7 / Practice + scoreboard + WINNER screen
- **R26** เลือกโหมดจริงจากเมนู: SNOOKER 15/10/6 (หลัก) + 8-Ball/9-Ball/Chinese Pool
- **R27** Animation (Blender pipeline `create_character_aaa.py`): UncleNok/Bo idle/celebrate/disappointed/speak
- **R28** Voice Pinning: `UncleNokReferee` 14 clips → prefab variant
- **R29** Multiplayer room (Normcore host/join/sync) — แยกแผน
- **R30** SFX จริง (พี่หาเสียง → วาง Inspector ได้ทันที)

---

# (งานเก่า) Phase A Audio Completion & Next Phases

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