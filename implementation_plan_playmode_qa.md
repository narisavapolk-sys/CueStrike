# Implementation Plan — PlayMode QA Coverage (R23)

**Goal:** Automate runtime-critical paths that previously required manual Vision audit:
R14 CallShot cycle, R16 Quest frame-rate policy, and R17 scene wiring.

**Branch:** `test/playmode-r14-r16-r17`
**Dependencies included:** R13/R14 from branch base, plus R16 and R17 commits cherry-picked
so the suite tests the exact implementations under review.

## Scope

### R14 — CallShot cycle

Create a minimal runtime object graph with the real `ChinesePoolGameManager`,
`ChinesePoolUIManager`, and `ChinesePoolCallShotUI` MonoBehaviours. The test assembly
uses reflection to resolve the existing `Assembly-CSharp` types, avoiding a new runtime
assembly boundary in this legacy project. Assert:

1. A human `NextPlayer()` activates the panel.
2. The UI builds 15 ball buttons and 6 pocket buttons.
3. Selecting ball 3 + pocket 2 then Confirm fires the event path and stores
   `calledBallId=3`, `calledPocketId=2` in GameManager.
4. Cancel fires the cancellation path and clears both values.
5. An AI turn does not activate the panel.

### R16 — frame-rate policy

Extract the device-model decision into a deterministic private static policy helper called
by `VRStartup.AutoDetectFrameRate()`. Invoke it by reflection in tests because
`SystemInfo.deviceModel` is read-only.

| Model | 120Hz opt-in | Expected |
|---|---:|---:|
| Meta Quest 2 / Oculus Quest | false | 72 |
| Meta Quest 3 | false | 90 |
| Meta Quest 3 | true | 120 |
| Meta Quest 3S / Quest Pro | true | 90 |
| PC / unknown | false | 90 |

### R17 — scene reference integrity

Load `Title_NoksGrandHall` and `AAA_RoomDAY` additively in PlayMode and assert for each:

- exactly one `ChinesePoolCallShotUI` exists in the loaded scene;
- `RunSelfTest()` returns true and all 9 serialized UI refs are assigned;
- `ChinesePoolUIManager._callShotUI` points to that survivor;
- no orphan serialized duplicate component produces an invalid-script log.

Each scene is unloaded in teardown to isolate tests.

## Test assembly

`Assets/CueStrike/Tests/PlayMode/CueStrike.PlayModeTests.asmdef` is a dedicated
PlayMode test assembly. It references only `UnityEngine.TestRunner` plus NUnit and
resolves game types through reflection, so production scripts remain in the existing
predefined `Assembly-CSharp` assembly. `testAssemblies: true` makes Unity discover the
suite for `-testPlatform PlayMode`.

## Verification

- `tools/compile_check.sh` → **0 errors**.
- Unity batchmode PlayMode run with `-runTests -testPlatform PlayMode` → **12/12 passed, 0 failed**.
- Existing EditMode tests remain unchanged.
- Test XML/log output is ignored and not committed.
- The dedicated worktree's cold Unity import exceeded the local timeout without a
  compiler error; final compile and PlayMode verification used the project's warm
  Library after temporarily overlaying only this branch's relevant files, then restoring
  the main checkout byte-for-byte (apart from Unity-generated ignored logs).

## Risks / tradeoffs

- Full scene load is slower than YAML checks, but catches broken serialized refs at runtime.
- Reflection keeps the test assembly isolated, at the cost of failures reporting missing
  type/member names instead of compile-time symbols.
- R16 tests the deterministic policy matrix, while one real-device log check remains useful
  for headset integration.

## Checklist

- [x] Plan written before implementation.
- [x] Add R16 deterministic policy seam.
- [x] Add PlayMode integration tests and dedicated test assembly.
- [x] Add valid test and editor-tool `.meta` files.
- [x] Remove orphan R17 scene component blocks exposed by runtime loading.
- [x] Compile gate passes (0 errors).
- [x] PlayMode batch test passes (12/12).
- [x] Update `CUESTRIKE_MASTER.md` and `TASK_PROGRESS.md`.
- [ ] Commit and push branch.
