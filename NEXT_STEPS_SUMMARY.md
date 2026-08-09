# CueStrike VR - Next Steps Summary & Recommendations

> **Generated:** 2026-08-05  
> **Phase:** Phase A Audio Completion → Phase B Animator + BoPanda Banter  
> **Status:** Foundation solid, pipeline working, content creation needed

---

## 📊 EXECUTIVE SUMMARY

| Area | Status | Notes |
|------|--------|-------|
| **3D Models (AAA)** | ✅ COMPLETE | 10 characters + 9 props + pool balls + cue + 9 room FBX |
| **Blender → Unity Pipeline** | ✅ COMPLETE | `create_all_aaa_master.py` + `CueStrikeAAAApplyAll.cs` |
| **Pink Material Fix** | ✅ COMPLETE | Auto URP/Lit conversion, Zero Pink Policy verified |
| **Self-Test System** | ✅ COMPLETE | Validates 10 abilities, materials, FBX/prefabs |
| **MCP Infrastructure** | ✅ COMPLETE | MCPTestEditor, MCP_FORCE_DASHBOARD working |
| **Audio System Architecture** | ✅ COMPLETE | 5 scripts, 14 slots, character fields, crowd system |
| **Audio Clips (Real)** | ❌ MISSING | **~50 clips needed** - this is the only blocker |

---

## 🎯 IMMEDIATE PRIORITY: PHASE A AUDIO (Week 1-2)

### Required Audio Clips (~50 total)

```
CueStrikeAudioManager (14):
├── hitSoft, hitMedium, hitHard, cushionHit, pocketHit, pocketRollClip
├── nearMissGasp, ambientRoom, chalkDust, miscued
├── ambientLoungeMusic, whooshShot, menuClick, menuHover

Character Voice + Ability (20 - 10 chars × 2):
├── Somchay, MeiLing, Gentleman, PanPan, Finn
├── KingFlex, Tusker, Phantom, Cassidy, Bones

Room Ambience (9 - one per AAA room):
├── ZenDojo, Cyberpunk, Luxury_DAY, Luxury_NIGHT, Industrial
├── SpaceNebula, WarpFantasy, GrandArena, NoirMemory

Crowd System (7+):
├── crowdApplause, crowdGroan, crowdMurmur (loop), crowdGaspClips[3+]
```

### Action Items for User:

1. **Source/Create Clips** - Use asset stores, record in-house, AI tools, or hire sound designer
2. **Format Specs:** .wav 44.1kHz 16-bit (or .ogg for Quest), SFX=Mono, Ambience=Stereo
3. **Place in:** `Assets/CueStrike/Audio/Clips/` (replace placeholders)
4. **Create CharacterData Assets** - 10 ScriptableObjects via `Create → CueStrike → Character Data`
5. **Assign in Inspector** - AudioManager, CharacterData, CrowdSystem, RoomManager

---

## 🔄 PHASE B: P9 ANIMATOR + BOPANDA BANTER (Week 2-3)

### P9 Animator Controller (9 States)
| State | Purpose |
|-------|---------|
| Idle | Standing waiting |
| Walk | Moving to shot |
| Aim | Leaning over table |
| Shoot | Strike animation |
| Celebrate | Pot success / frame win |
| Disappointed | Miss / foul |
| Speak | Voice lines / banter |
| Neutral | Default pose |
| Victory | Match win |

### BoPanda Banter System
- **Frame start/end** comments
- **Pot success** reactions (by ball type)
- **Foul** callouts
- **Century breaks** (100+ points) hype
- **Snooker escapes** commentary
- **Flukes** playful teasing
- **Near misses** gasps

### Uncle Nok Referee Integration
- Hook `UncleNokReferee.cs` → `ShotManager`, `RulesManager` events
- Voice announcements: "Foul - 4 points", "Ball in hand", "Frame to Player 1"

### Crowd Reactions
- Connect `CueStrikeCrowdSystem` applause/gasp/murmur to game events
- Spatial audio for GrandArena (84 spectators)

---

## 🎮 PHASE C: PLAYABILITY POLISH (Week 3-4)

| System | Status | Needed |
|--------|--------|--------|
| Shot Manager | ✅ Working | Fine-tune aim, power, spin, preview line |
| Rules (8/9/Snooker) | ⚠️ Partial | Foul detection, ball-in-hand, turn switching |
| Multiplayer (Photon) | ⚠️ Partial | Turn sync, voice chat (VoiceManager exists) |
| UI/HUD | ✅ Working | Finalize HUD, MainMenu, LobbyUI, NoirMemory |
| Character Abilities | ⚠️ Partial | Balance 10 abilities (cooldowns, VFX, audio) |

---

## 📋 PARALLEL: CHARACTER PREFABS & ANIMATIONS

### Already Ready (run `CharacterAAASetup.cs`):
- ✅ Humanoid rig setup (RigBuilder + TwoBoneIK for hands)
- ✅ Material assignment (URP/Lit)
- ✅ Prefab generation for 12 characters (10 players + BoPanda + UncleNok)

### Need Animation Clips:
- P9 states: Idle, Walk, Aim, Shoot, Celebrate, Disappointed, Speak, Neutral, Victory
- IK targets for cue interaction (LeftHand/RightHand)
- LOD groups for performance

---

## 🛠️ IMMEDIATE COMMANDS TO RUN

```bash
# 1. Generate all AAA assets (if not done)
# Open Blender 3.6 → Scripting → Paste create_all_aaa_master.py → Run ▶

# 2. Unity: Import & Configure All AAA
# Tools → CueStrike → Apply → Apply ALL AAA (Final Polish)

# 3. Verify Self-Test Passes
# Check console for "ALL PASSED ✅"

# 4. Fix Any Pink Materials
# Tools → CueStrike → Fix → Fix Pink Materials (URP Conversion)

# 5. Create Placeholder Audio (for testing while sourcing real clips)
# Tools → CueStrike → Generate → Create Placeholder Audio

# 6. Setup Character Prefabs
# Tools → CueStrike → Character System → Setup All AAA Characters
```

---

## 📁 KEY FILES REFERENCE

| File | Purpose |
|------|---------|
| `CueStrike_Project/BlenderScripts/README_BLENDER_WORKFLOW.md` | Complete pipeline docs |
| `CueStrike_Project/ROADMAP.md` | Phase breakdown with checkboxes |
| `CueStrike_Project/CHARACTER_SYSTEM_PLAN.md` | 12 character roster & abilities |
| `CueStrike_Project/AUDIO_SYSTEM_PLAN.md` | Audio architecture |
| `CueStrike_Project/AAA_WORLD_TOUR_IMPLEMENTATION.md` | Room decoration plan |
| `CueStrike_Project/TASK_PROGRESS_AUDIO.md` | **Audio task tracking (THIS DOC)** |
| `CueStrike_Project/task.md` / `task_mcp_unity.md` | Task tracking |

---

## 💡 STRATEGIC RECOMMENDATION

### START WITH PHASE A AUDIO because:
1. ✅ All 3D assets are ready — audio is the **only missing piece** for "content complete"
2. ✅ Audio integrates with **existing systems** (AudioManager, CrowdSystem, UncleNokReferee, CharacterAbilities)
3. ✅ **Low risk, high impact** — instantly makes the game feel "real"
4. ✅ Can be done **in parallel** with animator work

### THEN PHASE B ANIMATOR because:
1. Character prefabs exist but need animation clips + P9 controller
2. BoPanda/UncleNok banter brings **personality** (key differentiator for CueStrike)
3. Enables Phase C playtesting with **full feedback loop**

---

## 🚀 THE FOUNDATION IS SOLID. THE PIPELINE WORKS. NOW IT'S CONTENT + POLISH TIME! 🎱

### Next Session Should Focus On:
1. **User provides/creates ~50 audio clips** (or we use AI tools as interim)
2. **Create 10 CharacterData ScriptableObjects** with voice/ability clips
3. **Assign all clips in Inspector** and test in Play Mode
4. **Run verification checklist** from TASK_PROGRESS_AUDIO.md

Once audio is "content complete", we move to Phase B Animator + BoPanda Banter which brings the characters to life! 🐼🎱