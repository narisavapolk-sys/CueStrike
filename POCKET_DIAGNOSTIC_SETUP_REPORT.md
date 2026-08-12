# Pocket Diagnostics Setup Report

## PR / Git hygiene
- PR #40 Referee Mode Selector UI: merged into main after CI SUCCESS (`a40ebcd`).
- PR #41 Pocket Game Loop: merged into main after CI SUCCESS (`4ae5508`).
- PR #42 BoReferee audit/frame-start voice: merged into main after CI SUCCESS (`21d71da`).
- Local main synced to `origin/main` at the start of diagnostics.
- Generated TestResults artifact removed. Remaining untracked directories/meta are pre-existing Unity/editor artifacts and were not deleted because ownership is ambiguous.

## Changes in this diagnostic round
- `ChinesePoolBallSetup.BallsSpawned`: explicit event emitted immediately after rack creation.
- `BallPottedTracker.RegisterSpawnedBalls`: dynamic binding API with duplicate-safe event dispatch.
- `PocketGameLoopBridge`: subscribes to BallSetup spawn event and registers runtime transforms; fallback refresh remains for legacy scenes.
- `PocketPhysicsAuditor`: guarded AAA scene preflight; repairs Pocket tags, validates triggers, tags, layer matrix and required components.
- `Pocket`: tracker notification occurs before ball deactivation.

## Verification evidence
- Compile gate: **0 errors**.
- PocketPhysicsAuditor: **19 passed, 0 failed**; repaired six serialized Pocket tags.
- R43PocketTriggerPlayModeTests: **PASS** — real Rigidbody entered Pocket trigger, `OnBallPotted ball=1 player=0`, ball deactivated.

## Remaining manual/physics audit
The earlier AAA table shot at a real scene pocket did not reach the trigger (`eventRaised=False`, `ballActive=True`). The deterministic trigger test passes, but the table's actual shot trajectory/collider geometry still needs a controlled drop test with per-frame Y/velocity logging before claiming 100% physical pocketing.
