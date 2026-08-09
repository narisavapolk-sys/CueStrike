# Chinese Pool UI/Scoreboard — Implementation Plan
> **Project:** CueStrike VR Snooker (AAA Unity)
> **Phase:** 8.5 Chinese Pool Polish (P8 → 100%)
> **Date:** 2026-07-30

---

## Overview
Complete Chinese 8-Ball specific UI: Call Shot system, Group Display (Red/Yellow), 
and integrated Scoreboard. Makes P8 reach true 100%.

---

## Chinese 8-Ball Rules (Reference)
- 15 balls: 7 Red (1-7), 7 Yellow (8-14), 1 Black (15/8-ball)
- Break → Groups assigned by first potted ball
- Must CALL SHOT (ball + pocket) before shooting
- Clear your group → then pot 8-ball to win
- Pot 8-ball early = lose

---

## Files

| # | File | Path |
|---|------|------|
| 1 | `ChinesePoolCallShotUI.cs` | `Assets/CueStrike/Scripts/UI/ChinesePool/` |
| 2 | `ChinesePoolGroupDisplay.cs` | `Assets/CueStrike/Scripts/UI/ChinesePool/` |
| 3 | `ChinesePoolUIManager.cs` | `Assets/CueStrike/Scripts/UI/ChinesePool/` |
| 4 | `ChinesePoolUISetup.cs` | `Assets/CueStrike/Editor/` |
| 5 | `implementation_plan.md` | Project Root |
| 6 | `task.md` | Project Root |

---

## Setup Steps
1. Close Unity
2. Create folder `Assets/CueStrike/Scripts/UI/ChinesePool/`
3. Copy files 1-3 to that folder
4. Copy file 4 to `Assets/CueStrike/Editor/`
5. Run batchmode compile → 0 errors
6. Open Unity → `Tools → CueStrike → Apply → Setup Chinese Pool UI`
7. `Tools → CueStrike → Debug → Test Chinese Pool UI`

---

## UI Features
- **Call Shot Panel**: Select ball (1-15) + pocket (6 positions) → Confirm
- **Group Display**: Shows Red/Yellow assignment, remaining balls per group
- **8-Ball Warning**: Appears when only black ball remains
- **Foul Notification**: Auto-display with timeout
- **Turn Indicator**: Shows current player
- **Game State Text**: "OPEN TABLE", "GROUP ASSIGNED", "8-BALL TIME", etc.

---

## Integration Points
- Hooks into `CueStrikeRulesManager` for group assignment
- Hooks into `BallPottedTracker` for ball removal
- Hooks into `ChinesePoolScoreboard` for score updates