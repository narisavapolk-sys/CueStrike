# Implementation Plan — VRStartup frame rate auto-detect (R16)

**Round:** R16 (Vision audit R13 follow-up)
**Goal:** Make `Application.targetFrameRate` actually auto-detect Quest device
instead of hard-coding 90Hz. Also fix `OnDestroy` brand-name check.

**Branch:** `fix/vrstartup-frame-rate-detect` (worktree — main checkout has
uncommitted work from another session that I must not touch).

---

## 1. Background (Vision audit R13 — static semantic-equivalent audit)

`VRStartup.cs:74-78` (in branch `feat/vrstartup-menu`, commit `b0234cd`):

```csharp
else
{
    // Auto-detect: Quest 2 = 72Hz, Quest 3/3S = 90Hz (can go 120Hz)
    Application.targetFrameRate = 90;   // <-- actually HARDCODES 90, no detection
}
```

Auto-detect claim is wrong → every Quest 2 user gets 90Hz (vs. optimal 72Hz
which is Quest 2's native refresh).

Editor inspector default is `targetFrameRate = 0` meant as "auto" — but the
fallback path is hardcoded.

## 2. Acceptance criteria

- `targetFrameRate = 0` → call `AutoDetectFrameRate()` which returns:
  - `72` if `SystemInfo.deviceModel` mentions Quest 2 / Quest (1st gen)
  - `90` if it mentions Quest 3, Quest 3S, Quest Pro, or PCVR/Editor
  - `120` if feature opt-in via `[Header("Quest 3 120Hz (Experimental)")]`
    public bool enable120HzOnQuest3
- `targetFrameRate > 0` → use that explicit override (unchanged)
- `OnDestroy` correctly resets `s_Initialized` for whatever name the
  GameObject has (track by reference, not by hard-coded name).

## 3. Mapping table (verified by document search of Unity 6.0 system strings)

| Device | `SystemInfo.deviceModel` substring | target Hz |
|---|---|---|
| Meta Quest 2 | `"Quest 2"`, `"Oculus Quest 2"` | 72 |
| Meta Quest 3 | `"Quest 3"`, `"Quest 3S"`, `"Quest Pro"` | 90 (or 120 if opt-in) |
| Meta Quest (1st gen) | `"Oculus Quest"`, `"Quest"` (no "2" / "3") | 72 |
| PCVR / Editor / Other | empty or unknown | 90 |

Source: Meta Quest Link documentation + community-tested model strings (Unity
6 still uses `SystemInfo.deviceModel` — no API change for this).

## 4. Files modified

- `Assets/CueStrike/VR/VRStartup.cs` (only):
  - Add `public bool enable120HzOnQuest3 = false;` (Header "Experimental")
  - Replace lines 70-78 with explicit override + auto-detect
    - new method: `private int AutoDetectFrameRate()` returns {72, 90, 120}
    - Documented per-table; logs the detected Hz at boot
  - Fix `OnDestroy`: track the auto-created GO via `s_InitInstance` field,
    set on Awake, used in OnDestroy to reset `s_Initialized` correctly.

## 5. Risk & rollback

- Behavior is observable at runtime via `Application.targetFrameRate`
  (logged in existing `:94` log).
- Rollback: `git revert <commit>` → no schema change, no asset change.

## 6. Verification (compile gate)

- `tools/compile_check.sh` → 0 errors expected.
- Untested at actual hardware (cannot run from sandbox) — runtime
  verification deferred to manual "Vision audit" by P'Mong via Editor
  (Quick Play in headset). This is the same model as prior R10/R13/R14:
  code compiled, semantics reviewed, human plays and confirms Hz.

## 7. Documentation updates (กฎข้อ 2)

- `CUESTRIKE_MASTER.md` — add R16 row, update §5 changelog.
- `TASK_PROGRESS.md` — add Round 16 entry.
- `implementation_plan_vrstartup_framerate.md` — this file; tick boxes.

---

## 8. Pre-flight checklist (กฎข้อ 3)

- [x] Unity 6.0.4f1 — `SystemInfo.deviceModel` available.
- [x] Unity not running.
- [x] Worktree branched from `origin/main` (R13 branch `feat/vrstartup-menu`
      already exists but stuck in PR — we cherry-pick independently).
- [x] Single-file change scoped tightly (no drive-by edits).
- [x] Plan reviewed.
