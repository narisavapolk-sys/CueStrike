# Implementation Plan — Pocket Diagnostics

## Goal
พิสูจน์และแก้ physical pocket loop ใน `AAA_RoomDAY` ก่อนเพิ่ม visual feature: ball spawn → Pocket trigger → BallPottedTracker → GameManager score/turn → Bo referee.

## Order
1. Merge PR #40/#41/#42 and sync main.
2. Add explicit spawned-ball registration contract from BallSetup to BallPottedTracker; keep a guarded polling fallback for scenes without the event.
3. Notify tracker before Pocket deactivates a ball; avoid duplicate event delivery.
4. Add guarded Editor diagnostic tool for AAA_RoomDAY that validates tags/layers/colliders/references and drives a deterministic runtime drop test with logs.
5. Verify compile, editor self-test, and PlayMode diagnostic evidence.

## Acceptance
- No `{fileID: 0}` ball references remain after rack spawn registration.
- Pocket collider is trigger and Ball/Pocket tags/layers are valid.
- Exactly one `OnBallPotted` event is raised.
- GameManager processes the pot through its normal shot path and Bo receives the event.
