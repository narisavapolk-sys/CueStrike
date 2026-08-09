# CueStrike VR — Development Roadmap & AI Handoff Document

> **Project:** CueStrike VR (Snooker / 8-Ball / 9-Ball)  
> **Engine:** Unity 6 (URP 17.4)  
> **Target:** Meta Quest 2 / Quest 3  
> **Last Updated:** 2026-07-12 by Grave (เกรฟ) AI Agent  

---

## ⚠️ FOR AI AGENTS: READ THIS FIRST

This document describes the current state of CueStrike VR and what needs to be done.
If you are an AI agent continuing this work, follow the phase order below.
Check `PROGRESS.md` (same folder) for the latest status.

### Architecture Rules (MUST FOLLOW)
1. **Never modify core sync files** (`BallPhysics`, `ScoreManager`) directly — use event-driven patterns
2. **Use `[RuntimeInitializeOnLoadMethod]`** for physics components to avoid scene-file modification
3. **All Normcore scripts must have `#if` guards** — project may not have Normcore installed
4. **Canvas render mode must be WorldSpace** for VR — never use ScreenSpaceOverlay
5. **Jump shots: Snooker = FOUL**, Pool = ALLOWED (35-45° squeeze jump)

### Key Namespaces
- `CueStrike.Gameplay` — Rules, scoring, turns, potted ball tracking
- `CueStrike.VR` — VR-specific fixes, canvas fixer, comfort actions
- Root namespace — Most scripts (legacy, being migrated)

---

## PHASE 1 — Make It Buildable

### 1.1 XR Packages (CRITICAL — nothing works without these)
Install via `manifest.json` or Package Manager:
```
com.unity.xr.openxr
com.unity.xr.interaction.toolkit
com.unity.xr.hands  
com.unity.xr.management
com.unity.xr.meta-openxr
```

### 1.2 Normcore Multiplayer
Either install Normcore package OR wrap all references in `#if` guards:
- `CueStrikeBallSync.cs`
- `CueStrikeGameSync.cs`
- `CueStrikeGameSyncModel.cs`
- `CueStrikeNormcoreManager.cs`
- `CueStrikePlayerSync.cs`
- `CueStrikeVoiceManager.cs`

### 1.3 Audio Assets (PLACEHOLDERS EXIST — need AAA real clips)
- ✅ 9 placeholder WAV files generated via CueStrikeAudioGenerator.cs
- ✅ Audio system scripts complete (AudioManager, BallSoundController, PocketSoundDetector, NearMissDetector, ChampionshipCrowd)
- ✅ CharacterData has voiceClip + abilitySound fields
- ⏳ Need: 14 AudioManager clips + 20 Character clips + 9 Room Ambience + 7 Crowd clips = ~50 real .wav files
- 📝 Status: Architecture complete, sourcing real clips is the only blocker

### 1.4 3D Models (AAA COMPLETE — 10 chars + 9 props + balls + cue + 9 rooms)
- ✅ 10 Playable Characters: Somchay, MeiLing, Gentleman, PanPan, Finn, KingFlex, Tusker, Phantom, Cassidy, Bones
- ✅ 2 NPCs: UncleNok, BoPanda (Grand Hall)
- ✅ 9 Props: LuxuryChandelier, IndustrialLamp, ZenLantern, NeonSign_Strike, BarBottleSet, WarpPortalArch, SpaceConsole, HoloScreen, CrowdDummy
- ✅ Pool Balls: 16-ball FBX (CueStrike_PoolBalls_AAA.fbx)
- ✅ Cue Stick: CueStrike_Cue_AAA.fbx (6 parts: Shaft, Tip, Joint, Butt, Ring, Bumper)
- ✅ Table Textures: 9 PNGs (Felt, Wood, Cushion, Pocket, Diamond markers + normals)
- ✅ 9 AAA Rooms: ZenDojo, Cyberpunk, Luxury_DAY, Luxury_NIGHT, Industrial, SpaceNebula, WarpFantasy, GrandArena, NoirMemory
- ✅ Blender Pipeline: create_all_aaa_master.py runs all 4 scripts in one click

### 1.5 Compile Errors
Fix all CS errors before build. Common issues:
- Missing Normcore types
- Unity 6 API changes (e.g., `textureCompressionFormat` → `androidBuildSubtarget`)

---

## PHASE 2 — Multiplayer via Normcore

### Sync Architecture
- **Host Authority**: Host runs physics, broadcasts ball positions
- **PlayerSync**: XR rig pose (head + hands) replicated to all clients
- **CueSync**: Cue position/angle + strike events
- **TurnSync**: Whose turn, shot result, foul, next turn
- **ScoreSync**: Points, frames, match state, HUD updates

---

## PHASE 3 — Playability

- Async scene loading with loading screen
- Settings panel (volume, comfort vignette, snap/smooth turn, dominant hand)
- Ball number textures for pool modes
- Remove or repurpose `hub.unity`

---

## PHASE 4 — AAA Polish

- Post-processing volumes per room (bloom, vignette, color grading)
- Replay camera system
- Achievement system
- Online leaderboard
- VR spectator mode

---

## Existing Systems (Working)

| System | Script | Status |
|--------|--------|--------|
| Rules Engine | `CueStrikeRulesManager.cs` | ✅ Complete |
| Ball Physics | `CueStrikeBallPhysics.cs` | ✅ Complete |
| Felt Friction | `CueStrikeFeltFriction.cs` | ✅ Complete |
| Cushion Spin | `CueStrikeCushionPhysics.cs` | ✅ Complete |
| AI (4 levels) | `CueStrikeAIController.cs` + `CueStrikeAIStrategy.cs` (classes Easy/Medium/Hard/Expert) | ✅ Complete |
| HUD | `CueStrikeHUD.cs` + `CueStrikeHUDController.cs` | ✅ Complete |
| Scoreboard | `CueStrikeHolographicScoreboard.cs` | ✅ Complete |
| Potted Ball Tracker | `CueStrikePottedBallTracker.cs` | ✅ Complete |
| Room Manager | `CueStrikeRoomManager.cs` | ✅ Complete |
| Room Selection UI | MainMenu scene | ✅ Complete |
| APK Builder | `CueStrikeAPKBuilder.cs` | ✅ Complete |
| VR Canvas Fixer | `CueStrikeVRCanvasFixer.cs` | ✅ Complete |
| Break Tracker | `CueStrikeBreakTracker.cs` | ✅ Complete |
| Shot Manager | `CueStrikeShotManager.cs` | ✅ Complete |
| Turn Manager | `CueStrikeTurnManager.cs` | ✅ Complete |

---

## File Structure
```
Assets/CueStrike/
├── AI/           — AI opponent controllers
├── Audio/        — Audio manager scripts (NO audio files yet)
├── Branding/     — Logo assets
├── Characters/   — Character prefabs + ability controllers
├── Cues/         — Cue profile, rack, IK
├── Demo/         — Auto-demo system
├── Editor/       — Editor tools (scene builder, APK builder, setup scripts)
├── Environment/  — Environment manager
├── Gameplay/     — Rules, scoring, turns, potted ball tracker
├── Materials/    — 10 materials (felt, ball skins, wood)
├── Multiplayer/  — Normcore sync scripts
├── Physics/      — Felt friction, cushion physics
├── Prefabs/      — Tables, balls, cue, room props
├── Props/        — Decorative room props
├── RCA/          — Real Cue Adapter (calibration)
├── Rules/        — Game rules logic
├── Scenes/       — 9 scenes (MainMenu + 8 rooms)
├── Scripts/      — Core scripts (RoomManager, etc.)
├── Tables/       — Table scripts, pocket detection
├── Textures/     — 20 texture files
├── UI/           — HUD, scoreboard, menu controllers
└── VR/           — VR canvas fixer, comfort actions
```
