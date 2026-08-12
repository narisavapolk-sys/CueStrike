# Implementation Plan — R44 Pocket Game Loop

## Goal
ต่อ pocket detection เข้ากับ `ChinesePoolGameManager` ให้ลูกตกหลุมแล้วถูกประมวลผลเป็น shot result จริง: แต้ม, กติกา break/open-table, foul และการสลับเทิร์นใช้ flow เดียวกับการยิงปกติ

## Scope
- เพิ่ม `ChinesePoolGameManager.ProcessPottedBall(int)` เพื่อสร้าง `ShotResult` จาก ball group จริง แล้วเรียก `ProcessShotResult`.
- เพิ่ม `PocketGameLoopBridge` ต่อ `BallPottedTracker.OnBallPotted` → GameManager และ BoReferee; refresh ลูกที่ spawn runtime จาก `ChinesePoolBallSetup`.
- เพิ่ม `PocketGameLoopSetup` ผูก refs ใน `AAA_RoomDAY` แบบ idempotent.

## Guards
- ไม่ process cue ball (`ballId <= 0`).
- retry refs/ลูก spawn ทุก 0.5s.
- unsubscribe ตอน destroy.
- ใช้ GameManager shot pipeline เดิม ไม่เพิ่มระบบคะแนน/turn ซ้ำ.
