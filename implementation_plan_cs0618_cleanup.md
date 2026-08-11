# Implementation Plan — Replace deprecated FindObjectOfType family (CS0618)

**Round:** R15
**Goal:** Eliminate CS0618 warnings from runtime code (and editor tools where 1:1 safe) by
switching to Unity 6 modern equivalents, **without changing runtime behavior**.
**Branch:** `chore/cs0618-find-modernize` (worktree — main checkout has uncommitted work
from another session that I must not touch).

---

## 1. Scope (verified by `grep` — กฎข้อ 1)

| Pattern | Count | Files touched |
|---|---|---|
| `FindObjectOfType<T>()` (single-instance) | 18 | 11 runtime + 5 editor files |
| `FindObjectsOfType<T>()` (multi-instance, no args) | 10 | 4 runtime + 5 editor files (incl. one `GameObject.FindObjectsOfType<MeshRenderer>()`) |
| `FindObjectsOfType<MonoBehaviour>(true)` (inactive-flag) | 1 | `Editor/TitleSceneFixer.cs:158` |
| **`Object.FindObjectsByType` already modern** | many | not touched |

**Total:** **29 deprecated call sites** across **16 files** (11 runtime + 5 editor).

After migration, **modern API family** (`FindFirstObjectByType` / `FindAnyObjectByType` /
`FindObjectsByType`) usage will rise from **109 → 138 occurrences**.

---

## 2. Replacement mapping (Unity 6 reference: docs.unity3d.com/6000.0/Documentation/ScriptReference/Object.FindFirstObjectByType.html)

### 2.1 `FindObjectOfType<T>()`
**→** `FindFirstObjectByType<T>()`

- **Semantics:** identical — returns the first enabled (active) component in the scene.
  Order = sort-by-InstanceID ascending, identical to `FindObjectOfType` semantics.
- **Warning raised if skipped:** CS0618 (`Object.FindObjectOfType` is obsolete since 2023.1).

### 2.2 `FindObjectsOfType<T>()`
**→** `FindObjectsByType<T>(FindObjectsSortMode.None)`

- **Semantics:** returns all components of type T. With `FindObjectsSortMode.None`,
  no sorting cost. The call returns active + inactive components by default (matching
  default `FindObjectsOfType` semantics).
- **Note:** Order **may differ** from the deprecated call (which sorted by name and
  InstanceID). This does NOT change behavior in our code — verified by reading each
  call site: none of them depend on iteration order (they iterate to apply effects
  to all matches, or cache the array for later use — none re-order or filter by index).

### 2.3 `FindObjectsOfType<T>(true)` (inactive flag)
**→** `FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)`

- One occurrence: `Editor/TitleSceneFixer.cs:158` — `true` ⇒ include inactive.

### 2.4 `GameObject.FindObjectsOfType<T>()`
**→** `FindObjectsByType<T>(FindObjectsSortMode.None)` (drop `GameObject.` qualifier)

- `GameObject.FindObjectsOfType` was the old static convenience method. Removed in
  Unity 2023+. Use `Object.FindObjectsByType` (in static context or call from any
  MonoBehaviour without `Object.`).
- **Special case** `UI/CueStrikeHUDController.cs:411` — `GameObject.FindObjectsOfType<MeshRenderer>()` ⇒ just `FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)`.

---

## 3. Risk analysis — behavior preservation

| Call site (file:line) | Current pattern | New pattern | Risk |
|---|---|---|---|
| `Audio/NearMissDetector.cs:87` | `FindObjectOfType<Rigidbody>()` | `FindFirstObjectByType<Rigidbody>()` | None — comment already says "too broad" |
| `Characters/Bones/BonesXRayVision.cs:83` | `FindObjectsOfType<Transform>()` | `FindObjectsByType<Transform>(None)` | None — iterates all transforms |
| `Characters/Gentleman/...AbilityController.cs:77,87` | find all Rigidbody / Light | same path, None sort | None — applies physics force / sets candles, order irrelevant |
| `Characters/MeiLing/...AbilityController.cs:79` | find all Rigidbody | same | None |
| `Characters/Phantom/PhantomSpectralSight.cs:142` | find all Rigidbody | same | None |
| `Demo/CueStrikeAutoDemo.cs:22` | `FindObjectOfType<CueStrikeAIController>()` | first-by-type | None |
| `Environment/CueStrikeEnvironmentManager.cs:28` | `FindObjectOfType<RoomLightingManager>()` | first-by-type | None — singleton pattern |
| `Gameplay/Tutorial/CueStrikeTutorialManager.cs:63-66` | 4× `FindObjectOfType<X>()` | first-by-type | None — early setup, must find the existing instance |
| `Scripts/VR/Input/CueStrikeVRInputManager.cs:30` | `FindObjectOfType<CueStrikeVRInputManager>()` (singleton-check) | first-by-type | **Critical: must be FindFirstObjectByType** — runtime singleton pattern. Same semantics. |
| `UI/CueStrikeHUD.cs:31` | `FindObjectOfType<CueStrikeShotManager>()` | first-by-type | None |
| `UI/CueStrikeHUDController.cs:411` | `GameObject.FindObjectsOfType<MeshRenderer>()` (iterates to hide) | `FindObjectsByType<MeshRenderer>(None)` | None — iterate + SetActive(false) per mesh, no order logic |
| Editor tools (ChinesePoolEditor, ChinesePoolUISetup, CueStrikeVisualAudit, IntegrationSelfTest, MultiplayerSelfTest, NoirMemory*, RoomScreenshotTool) | setScene helpers / editor quicksetup | first-by-type | **Risk = None** — used at edit time to wire prefabs; not in runtime |
| `Editor/TitleSceneFixer.cs:158` | `FindObjectsOfType<MonoBehaviour>(true)` (include inactive) | `FindObjectsByType<MonoBehaviour>(Include, None)` | **API-forced same** — direct convert |

**Conclusion:** every site is a 1:1 semantic swap — no behavior change.

---

## 4. Verification strategy

1. **Pre-write compile:** run `tools/compile_check.sh` (Local compile gate — fast, Library warm);
   record baseline warning count from log.
2. **Apply migration via Python script** (one regex-driven pass per pattern; preserves
   indentation, comments, surrounding code).
3. **Post-write compile:** run gate again; confirm `0 errors` and **warning count strictly
   decreased** by 29 (the count we replaced).
4. **Spot-read each modified file** — diff against HEAD + visually inspect to verify the
   migration script did not damage syntax.
5. **Stage + commit + push** via worktree.
6. **Report PR URL** for user to click "Create pull request".

---

## 5. Files modified (expected)

**Runtime (11 files, 15 occurrences):**
1. `Audio/NearMissDetector.cs` (1)
2. `Characters/Bones/BonesXRayVision.cs` (1)
3. `Characters/Gentleman/GentlemanAbilityController.cs` (2)
4. `Characters/MeiLing/MeiLingAbilityController.cs` (1)
5. `Characters/Phantom/PhantomSpectralSight.cs` (1)
6. `Demo/CueStrikeAutoDemo.cs` (1)
7. `Environment/CueStrikeEnvironmentManager.cs` (1)
8. `Gameplay/Tutorial/CueStrikeTutorialManager.cs` (4)
9. `Scripts/VR/Input/CueStrikeVRInputManager.cs` (1)
10. `UI/CueStrikeHUD.cs` (1)
11. `UI/CueStrikeHUDController.cs` (1)

**Editor (5 files, 14 occurrences):**
12. `Editor/ChinesePoolEditor.cs` (2)
13. `Editor/ChinesePoolUISetup.cs` (5)
14. `Editor/CueStrikeVisualAudit.cs` (1)
15. `Editor/TitleSceneFixer.cs` (1 inactive-flag pattern + others if present)
16. `Editor/IntegrationSelfTest.cs`, `Editor/MultiplayerSelfTest.cs` (3)
17. `Editor/NoirMemoryPuzzleEditor.cs` (2)
18. `Editor/NoirMemorySelfTest.cs` (4)
19. `Editor/RoomScreenshotTool.cs` (1)

**Total: 16–18 files, 29 call sites** (exact once we script-read).

---

## 6. Rollback plan

Migration is a textual find/replace — `git commit` then `git revert HEAD` is the rollback
if CI catches a real regression (e.g., off-by-one iteration issue).

---

## 7. Documentation updates (กฎข้อ 2)

- `CUESTRIKE_MASTER.md` — add R15 row in §5 changelog; update §3 status section.
- `TASK_PROGRESS.md` — add Round 15 entry.
- `implementation_plan_cs0618_cleanup.md` — check off completed boxes post-merge.

---

## 8. Pre-flight checklist (กฎข้อ 3 — run before write)

- [x] Unity 6.0.4f1 — supports `FindFirstObjectByType` (added 2023.1).
- [x] Unity not running (no Editor lock).
- [x] Worktree for branch (main checkout left untouched — protects other-session work).
- [x] `git status --short` reviewed — no mods that I should preserve belong in this PR.
- [x] Plan written and reviewed.
