# Task: CueStrike VR — R42 Referee Mode (Current) → R43+ Roadmap

## ✅ R42 — REFEREE MODE SWITCHER (Ready for PR)
- [x] BoRefereeEventBridge.cs: enum RefereeMode { ReplaceUncle, DuoWithUncle } + ApplyRefereeMode()
- [x] RefereeModeSetup.cs (Editor tool: ตั้งโหมด + batchmode + self-test 6/6)
- [x] 🐛 fix meta GUID R40 (BoReferee/Bridge ตรง prefab — component กลับมา)
- [x] Compile 0 errors + self-test 6/6 + idempotent
- [ ] รอ CI เขียว + merge


## ✅ R41 — SNOOKER AI DIFFICULTY UI (Ready for PR)
- [x] SnookerDifficultyUI.cs (ลอก R34: Canvas + 4 ปุ่ม + bridge.SetDifficulty + PlayerPrefs + highlight)
- [x] SnookerDifficultyUISetup.cs (Editor tool: ผูก component + bridge ref + idempotent + self-test 5/5)
- [x] Snooker_Demo.unity: SnookerDifficultyUI_Controller + _bridge ref
- [x] Compile 0 errors + self-test 5/5 + idempotent
- [ ] รอ CI เขียว + merge


**Current Objective (2026-08-12):** ต่อ AI opponent เข้ากับโหมด Practice — ลุงโน๊กเป็นคู่ซ้อม AI เลือกระดับ Easy/Medium/Hard/Expert จาก UI — ใช้ CueStrikeAIController ที่มีอยู่แล้ว

---

## ✅ R34 — PRACTICE AI (MERGED ✅ — PR #29)

### ✅ Done
- [x] ตรวจของจริง: `ChinesePoolAIModifier` มีครบแต่ไม่มีใครเรียก (dead), `CueStrikeAIController` ยิงผ่าน reflection แต่ฉากไม่มี ShotManager, ฉากที่มี GameManager = AAA_RoomDAY เท่านั้น
- [x] `CueStrikePracticeAIBridge.cs` — subscribe OnTurnChanged → isAiTurn → DecideCallShot → SetCallShot → DecideShotParameters → ยิงจริง (AddForce/ShotManager) → รอลูกหยุด → ProcessShotResult + fail-safe + SetAIDifficulty
- [x] `ChinesePoolMatchSetupUI.cs` — เพิ่มแถวเลือกระดับ AI (Easy/Medium/Hard/Expert) + PlayerPrefs + เรียก bridge ก่อนเริ่มแมตช์
- [x] `PracticeAISetup.cs` (Editor tool `Tools/CueStrike/AI/90. Setup Practice AI`) — ผูก bridge ลง AAA_RoomDAY + idempotent + self-test + batchmode
- [x] Compile verify: batchmode **0 errors** + self-test **10/10**

### ⏳ เหลือ (Vision audit / R35)
- [ ] Vision audit: เปิด AAA_RoomDAY → เลือก Practice + ระดับ AI → สังเกต AI ยิงเองตอนเทิร์นมัน
- [x] R37: แก้ AI blocker (เพิ่ม ChinesePoolAIModifier + assign refs) — ทำแล้ว (ดู section R37)
- [x] R36: Snooker AI (WBPS) — ทำแล้ว (ดู section R36)

## ✅ R36 — SNOOKER AI (WBPS) (MERGED ✅ — PR #31)
- AI เล่นสนุกเกอร์ได้: turn system P1↔P2 + เลือกลูกตามกฎ (red→color→color phase) + ghost-ball aim + AddForce + difficulty error
- แก้ WBPS: Rigidbody ให้ลูก + public accessors
- Editor tool `SnookerAISetup`: สร้างโต๊ะ + 6 หลุม + physics 22 ลูก + ผูก bridge (idempotent + self-test 6/6)
- ✅ Verify: compile 0 errors, self-test 6/6 PASS, idempotent ผ่าน

## ✅ R37 — CHINESEPOOL AI FIX (MERGED ✅ — PR #32)
- เพิ่ม ChinesePoolAIModifier ลง AAA_RoomDAY + assign GameManager.aiModifier + bridge.aiModifier
- Editor tool `ChinesePoolAIModifierSetup` (idempotent + self-test 3/3 + batchmode)
- ✅ Verify: compile 0 errors, self-test 3/3 PASS, idempotent ผ่าน

## 🎯 R38 — BALLSETUP FIX (Ready for PR)
- Vision audit เจอ blocker ตัวจริง: ไม่มี ChinesePoolBallSetup ใน AAA → เกมไม่เริ่มเฟรม → AI ยิงไม่ได้
- Editor tool `ChinesePoolBallSetupFixer`: เพิ่ม component + prefabs (Pool_CueBall/01/08/09) + assign GameManager.ballSetup
- ✅ Verify: compile 0 errors, self-test 6/6 PASS, idempotent ผ่าน

### ⏳ เหลือ (Vision audit / R41)
- [ ] R41: Vision audit ซ้ำ — AI ยิงแล้วลูกขยับจริง (PlayMode test + manual)
- [ ] R41: pocket detection / ฟิสิกส์โต๊ะใน AAA
- [ ] R41: difficulty selector ใน UI (Snooker) / Multiplayer room (Normcore) / Snooker AI difficulty UI


## ✅ R40 — BO REFEREE (Ready for PR)
- [x] BoReferee.cs (ลอก UncleNokReferee — PlayRandomClip + cooldown + FoulType)
- [x] BoRefereeEventBridge.cs (ลอก R31 signature จริง — GetFrameWinner + WBPS events)
- [x] BoVoicePinSetup tool: AudioSource 3D + refs + 14 clips + bridge + disable ลุง bridge
- [x] BoPanda_Prefab: BoReferee + 14 clips + bridge; UncleNok bridge disabled (ลุงกองเชียร์)
- [x] Compile 0 errors + self-test 18/18 + idempotent
- [ ] รอ CI เขียว + merge

## ✅ R39 — TITLE SCOREBOARD (Ready for PR)
- [x] ขยาย BoScoreboardSetup → loop 2 ฉาก (AAA_RoomDAY + Title_NoksGrandHall)
- [x] Title ได้ ChinesePoolScoreboard + UIManager._scoreboard assigned (300446121)
- [x] Compile 0 errors + self-test 3/3 ×2 + idempotent (รันซ้ำ skip)
- [ ] รอ CI เขียว + merge
- [ ] R38: Multiplayer room (Normcore)

## ✅ R35 — BO COMEDY SCOREBOARD (MERGED ✅ — PR #30)
- สร้าง `ChinesePoolScoreboard` จริงใน AAA_RoomDAY (พบว่าไม่มี — มีแค่ mesh ตกแต่ง) + ผูก `UIManager._scoreboard`
- Bo จะ subscribe OnScoreChanged → สกอร์ P1==P2>0 → Speak "ใครชนะนะ??"
- Editor tool `BoScoreboardSetup` (idempotent + self-test 3/3 + batchmode)
- ✅ Verify: compile 0 errors, self-test 3/3 PASS, idempotent ผ่าน

---

## 🎯 R33 — BOPANDA ลงห้องแข่ง (MERGED ✅ — PR #27)

### ✅ Done
- [x] ตรวจของจริง: BoPanda อยู่ในแค่ Title, tool R29 จัดการแค่ UncleNok
- [x] ขยาย `MascotScenePlacementSetup.cs` — เพิ่ม BoPanda placement (ฝั่งตรงข้ามลุงโน๊ก 0,0,4.6) + idempotent ตรวจชื่อ + self-test
- [x] วาง BoPanda ลง AAA_RoomDAY + Snooker_Demo
- [x] Compile verify: batchmode **0 errors** + tool **2/2 ฉาก** + idempotent (รันซ้ำ skip) + self-test **4/4**

### ⏳ เหลือ (R34 คู่ซ้อม AI / Vision audit)
- [ ] ต่อ AI opponent กับโหมด Practice (R34) — ลุงโน๊กคู่ซ้อม AI
- [ ] Vision audit: เปิด AAA_RoomDAY → เห็นลุงโน๊ก (0,0,-4.6) + โบ (0,0,4.6) ยืนคนละฝั่งโต๊ะ

---

## 🎯 R31 — REFEREE EVENT BRIDGE (MERGED ✅ — PR #28)

### ✅ Done
- [x] ตรวจของจริง: GameManager (`OnFrameWon`/`OnFoulCommitted`/`OnMatchOver`/`OnTurnChanged`/`OnPhaseChanged`) + WBPS (`OnBallPotted`/`OnFoulCommitted`/`OnFrameWon`) — มี Instance pattern ทั้งคู่; `UncleNokReferee` methods มีครบแต่ยังไม่มีใคร subscribe
- [x] `UncleNokRefereeEventBridge.cs` — subscribe events → เรียก referee methods (OnMatchStart/OnFrameStart/OnBallPotted/OnFoulCommitted) + fail-safe retry
- [x] Editor tool `RefereeEventBridgeSetup.cs` — `Tools/CueStrike/Mascots/80. Setup Referee Event Bridge` (PrefabUtility + idempotent + self-test + batchmode)
- [x] ผูก bridge เข้า UncleNok_Prefab — ฉากไหนมีลุงโน๊กได้ผลอัตโนมัติ
- [x] Compile verify: batchmode **0 errors** + self-test **5/5**

### ⏳ เหลือ (R35 คู่ซ้อม AI / เสียงจริง)
- [ ] หาเสียงกรรมการจริง (wav วางใน `Assets/CueStrike/Audio/Clips/`) — bridge พร้อมรับแล้ว ไม่ต้องแก้โค้ด
- [ ] โหมด Practice (เล่นคนเดียว) — กรรมการเป็นคู่ซ้อม AI (R35)

---

## 🎯 R32 — BO COMEDY DIRECTOR (MERGED ✅ — PR #26)

### ✅ Done
- [x] ตรวจของจริง: BoPanda prefab มี Animator + controller (triggers Disappointed/Speak/IsIdle) + `BoPandaBanter` + Scoreboard มี `OnScoreChanged`
- [x] `BoComedyDirector.cs` — Bo หลับ (idle 30s → Disappointed, ตื่นเมื่อลูกขยับ) + Bo มึนสกอร์เสมอ (OnScoreChanged → Speak + cooldown)
- [x] Editor tool `BoComedySetup.cs` — `Tools/CueStrike/Mascots/70. Setup Bo Comedy Director` (PrefabUtility + idempotent + self-test + batchmode)
- [x] ผูกเข้า BoPanda_Prefab — ฉากไหนมี Bo instance ได้ผลอัตโนมัติ
- [x] Compile verify: batchmode **0 errors** + self-test **7/7**

### ⏳ เหลือ (โมเมนต์ตลกขั้นสูง)
- [ ] ท่า animation ใหม่ (Sleep/Gasp/Dance) ผ่าน Blender — Bo ขโมยชอล์ก / กลัวลูกพุ่ง / กองเชียร์พลาด
- [ ] ใส่ BoPanda ในห้องแข่ง → Comedy มึนสกอร์ทำงานเต็มรูปแบบ (ตอนนี้ Bo อยู่ใน Title อย่างเดียว)

---

## 🎯 R30 — VOICE PINNING (MERGED ✅ — PR #25)

### ✅ Done
- [x] ตรวจของจริง: clips 14 ตัว **assign ครบแล้ว** ใน prefab แต่ **ไม่มี AudioSource** + refs 3 ตัวว่าง
- [x] Editor tool `UncleNokVoicePinSetup.cs` — `Tools/CueStrike/Mascots/60. Pin UncleNok Voice & Refs` (PrefabUtility.LoadPrefabContents + idempotent + self-test + batchmode)
- [x] เพิ่ม AudioSource (3D spatial) + assign `_animator`/`_audioSource`/`_homePosition` ใน UncleNok_Prefab
- [x] Compile verify: batchmode **0 errors** + tool รันจริง + self-test **12/12**
- [x] 3 ฉาก (Title/AAA_RoomDAY/Snooker_Demo) เป็น prefab instance → ได้ผลอัตโนมัติ ไม่ต้องแก้

### ⏳ เหลือ (R31 กรรมการจริง)
- [ ] ผูก UncleNokReferee กับ game events (OnFrameStart/OnBallPotted/OnFoulCommitted) — ประกาศคะแนน/ฟาวล์จริง
- [ ] Vision audit: เปิด Title → ลุงโน๊กพูด (voice 14 clips) ตอนเริ่มเกม/ลูกเข้าหลุม/ฟาวล์

---

## 🎯 R29 — MASCOT SCENE PLACEMENT (MERGED ✅ — PR #24)

### ✅ Done
- [x] ตรวจของจริง: BoPanda อยู่ใน Title (1.8, 0.4, -1.6) + Animator/controller ครบ → **animation เล่นได้**
- [x] พบว่า **UncleNok_Prefab ไม่ถูกวางในฉากไหนเลย** (มีแค่ `UncleNok_Placeholder` cube ใน Title) + ฉากห้องแข่งไม่มี mascot/referee
- [x] Editor tool `MascotScenePlacementSetup.cs` — `Tools/CueStrike/Mascots/50. Place Mascots in Scenes` (PrefabUtility.InstantiatePrefab + idempotent + self-test + batchmode)
- [x] วาง UncleNok_Prefab: Title (แทน placeholder, 0, 0.9, 2) + AAA_RoomDAY + Snooker_Demo (ริมโต๊ะ 0, 0, -4.6)
- [x] Compile verify: batchmode **0 errors** + tool **3/3 ฉาก** + idempotent (รันซ้ำ skip) + self-test **4/4**

### ⏳ เหลือ (R30 Voice Pinning)
- [ ] assign `_animator/_audioSource/_homePosition` ของ UncleNokReferee (ตอนนี้ animation เล่นได้ แต่ voice + home position ยังว่าง)
- [ ] Vision audit: เปิด Title → เห็นลุงโน๊ก+โบยืนริม (Idle animation หายใจ); เปิดห้องแข่ง → เห็น referee ริมโต๊ะ

---

## 🎯 R28 — SFX 9 ช่อง (MERGED ✅ — PR #23)

### ✅ Done
- [x] ตรวจของจริง: ไฟล์ SFX 9 ตัวมีอยู่แล้วใน `Audio/Clips/` (placeholder สังเคราะห์) แต่ **AudioManager อยู่ในแค่ Title scene** — ห้องแข่ง/เมนูไม่มีเสียง
- [x] `CueStrikeAudioManager.cs`: เพิ่ม `cueStrike` + `crowdAmbient` fields + `PlayCueStrike(intensity)` (volume/pitch ตามแรงยิง) + `PlayCrowdAmbient()` (loop)
- [x] Editor tool `CueStrikeSfxSceneSetup.cs` — `Tools/CueStrike/Audio/40. Setup SFX Channels`
  - เพิ่ม AudioManager + assign 9 clips + DynamicPhysicsSFX + crowd murmur ให้ **12 ฉากที่เล่นได้** (idempotent)
- [x] Compile verify: batchmode **0 errors** + tool รันจริง **12/12 ฉาก** + self-test **19/19 ผ่าน**
- [x] ตารางไฟล์ที่พี่ต้องหา (9 ช่อง) เขียนไว้ใน TASK_PROGRESS.md — วางไฟล์ทับชื่อเดิม ไม่ต้องแก้โค้ด

### ⏳ เหลือ (พี่ต้องทำ — หาเสียงจริง)
- [ ] หาเสียงจริง 9 ไฟล์ตามตารางใน `TASK_PROGRESS.md` → วางทับใน `Assets/CueStrike/Audio/Clips/`
- [ ] Vision audit: เปิดห้องแข่ง → ตียิง → ควรได้ยิน ball hit/cushion/pocket/cue + UI click/hover

---

## 🎯 R27 — CHARACTER ANIMATION (MERGED ✅ — PR #22)

### ✅ Done
- [x] ตรวจของจริง: rig Rigify **706 bones ชื่อเหมือนกันทุกตัวละคร** (UncleNok = Somchay), mesh skin กับ DEF-* bones (72 vgroups), prefab เป็น Somchay variant + Animator `m_Controller: 0` (ว่าง)
- [x] Blender script `create_character_animations_aaa.py` — pose DEF bones โดยตรง + keyframe เฉพาะที่ขยับ + prune 706→28-33 bones
- [x] Export 4 FBX (Idle 3s loop / Celebrate / Disappointed / Speak 2s) — 180-210KB ต่อตัว
- [x] Editor tool `CharacterAnimationSetup.cs` — remap clip paths → `Somchay_Rig/` prefix, สร้าง `.anim` 4 ตัว, อัปเดต controller (states + AnyState transitions + Idle default loop), assign ให้ UncleNok+BoPanda prefab, sync referee triggers (Announce→Speak, Disapprove→Disappointed, Thinking→Speak)
- [x] Compile verify: batchmode **0 errors** + self-test **5/5**

### ⏳ เหลือ (Vision audit — พี่ดูด้วยตา)
- [ ] เปิดเกม → ลุงโน๊ก/โบ ขยับ Idle (หายใจ) อัตโนมัติ; พอลูกเข้าหลุม → Celebrate; พลาด/ฟาวล์ → Disappointed; พูด → Speak

## 🎯 R24 — FIRST-TIME TUTORIAL (MERGED ✅ — PR #19)

### ✅ Done (merged 2026-08-11)
- [x] `CueStrikeFirstTimeFlow.cs` — PlayerPrefs first-time flag + 3 สไลด์ภาษาไทย + Skip + fail-safe auto-UI
- [x] ผูกเข้า `Title_NoksGrandHall.unity` + Editor tool `FirstTimeTutorialSetup`
- [x] Compile 0 errors + scene load clean + PR #19 merged (CI green)

---

## 🎯 R25 — MATCH FLOW (BEST-OF + WINNER) (MERGED ✅ — PR #20)

### ✅ Done (merged 2026-08-11)
- [x] Practice mode + Scoreboard frames + WINNER screen + Best-of dialog
- [x] PR #20 merged (CI green) — main ถึง 8b048b6

---

## 🎯 R26 — MODE SELECTION (SNOOKER 15/10/6 หลัก) (Ready for test)

### ✅ Done
- [x] ตรวจระบบเดิม: `CueStrikeTutorialManager` เป็น in-match validation (ต้องมีโต๊ะ/ShotManager) — ไม่เหมาะกับ Lobby onboarding
- [x] สร้าง `CueStrikeFirstTimeFlow.cs` — PlayerPrefs first-time flag + 3 สไลด์ภาษาไทย + Skip + fail-safe auto-UI
- [x] ผูก component เข้า `Title_NoksGrandHall.unity` (GameObject `FirstTimeTutorial`)
- [x] Editor tool `FirstTimeTutorialSetup.cs` — idempotent + guard 3 ชั้น + self-test + batchmode entry
- [x] Compile verify: batchmode **0 errors** + scene load 0 errors
- [x] Docs: `CUESTRIKE_MASTER.md`, `TASK_PROGRESS.md`, `implementation_plan_r24_title_tutorial.md`

### ✅ Done
- [x] `ChinesePoolGameManager` — practice mode (`StartNewMatch(0)`, `isPracticeMode`, `StartPracticeMatch`)
- [x] `ChinesePoolScoreboard` — `SetFrameScore` + ช่อง frames (P1/P2)
- [x] `ChinesePoolUIManager` — `SetFrameScore` / `OnFrameEnded` / `ShowMatchOver`
- [x] `ChinesePoolMatchSetupUI.cs` (ใหม่) — panel 5 ปุ่ม (Single Frame/3/5/7/Practice)
- [x] `ChinesePoolMatchEndScreen.cs` (ใหม่) — WINNER + เล่นอีกครั้ง/กลับเมนู
- [x] ผูก `MatchFlow` + `MatchEndScreen` เข้า `AAA_RoomDAY.unity`
- [x] Editor tool `ChinesePoolMatchFlowSetup` — idempotent + self-test + batchmode
- [x] Compile 0 errors + scene load clean + self-test 3/3

### ✅ Done
- [x] `CueStrikeWBPSRuleset.SetupRack()` — runtime rack builder (15→5 แถว/10→4/6→3)
- [x] `CueStrikeGameModeSelector` — static mode + red balls + scene mapping
- [x] `CueStrikeModeSelectionPanel` — 6 ปุ่มโหมด self-building UI
- [x] `MainMenuUIController` — `SelectModeAndLoad` + `modeButtons[]`
- [x] ผูก `ModeSelectionPanel` เข้า MainMenu.unity
- [x] Editor tool `CueStrikeModeSelectionSetup` — idempotent + self-test + batchmode
- [x] Compile 0 errors/0 warnings + rack test 6/10/15 PASS + self-test 3/3

### 🔄 To verify in Editor (Vision audit)
- [ ] MainMenu → เห็น panel เลือกโหมด (Snooker 15/10/6 หลัก)
- [ ] เลือก Snooker 6 → Snooker_Demo → เห็นลูกแดง 6 ลูก (สามเหลี่ยม 3 แถว)
- [ ] เลือก Chinese Pool → AAA_RoomDAY

## ⏭️ NEXT (R27–R31, per coach)
- **R27** Animation (Blender pipeline `create_character_aaa.py`): UncleNok/Bo idle/celebrate/disappointed/speak
- **R28** Voice Pinning: `UncleNokReferee` 14 clips → prefab variant
- **R29** Multiplayer room (Normcore host/join/sync) — แยกแผน
- **R30** SFX จริง (พี่หาเสียง → วาง Inspector ได้ทันที)
- **R31** (nice-to-have) ฉาก dedicated 8-Ball/9-Ball + ต่อ Best-of dialog ทุกโหมด

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