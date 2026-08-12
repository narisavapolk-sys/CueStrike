# Task Checklist — Pocket Diagnostics

## Code
- [x] `BallPottedTracker.cs`: public dynamic registration method + duplicate guard.
- [x] `ChinesePoolBallSetup.cs`: notify tracker immediately after rack spawn.
- [x] `Pocket.cs`: dispatch tracker notification before deactivation.
- [x] `PocketGameLoopBridge.cs`: keep GameManager/Bo wiring and fallback refresh.
- [x] `Editor/PocketPhysicsAuditor.cs`: guarded scene validation and deterministic audit entry point.

## Scene / Physics
- [x] `AAA_RoomDAY`: tracker, GameManager, BallSetup and bridge references assigned.
- [x] Pocket colliders are triggers and use the correct tag/layer.
- [x] Ball tag/layer and Rigidbody collision detection are correct.
- [ ] Physics layer matrix allows Ball ↔ Pocket trigger interaction.

## Verification
- [x] Unity compile gate 0 errors.
- [x] Editor auditor self-test passes.
- [ ] PlayMode diagnostic logs ball Y/velocity/tag/layer and confirms one OnBallPotted (controlled real-table trajectory still pending).
- [ ] Confirm GameManager score/turn and Bo announcement from a real AAA table shot.
- [x] Update `TASK_PROGRESS.md`, `CUESTRIKE_MASTER.md`, and setup report.
