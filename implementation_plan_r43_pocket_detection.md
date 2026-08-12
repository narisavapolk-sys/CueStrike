# Implementation Plan — R43: Pocket Detection + ฟิสิกส์โต๊ะ AAA_RoomDAY

**วันที่:** 2026-08-12
**Branch:** `feat/r43-pocket-detection` → `main`
**กฎ:** กฎข้อ 1 (ตรวจของจริง) + กฎข้อ 5 (plan ก่อน, docs + compile ในรอบเดียวกัน)

## หมายเหตุชื่อ R
พี่สั่งว่า "R39" แต่ **R39 ถูกใช้ไปแล้ว** (Title scoreboard = PR #34) → งานนี้คือ **R43** ตาม roadmap

## เป้าหมาย
ให้ลูกตกลงหลุมได้จริงใน AAA_RoomDAY — เพิ่ม Pocket detection (BallPottedTracker + Pocket components) + ฟิสิกส์หลุมบนโต๊ะจริง — ต่อยอดจาก R38 BallSetup fix (ลูกมี Rigidbody + Collider แล้ว)

## สถานะจริงที่ตรวจพบ (กฎข้อ 1)

| รายการ | สถานะ | หลักฐาน |
|--------|--------|---------|
| `BallPottedTracker.cs` | ✅ มี (event-driven: OnBallPotted/OnBlackBallPotted/OnAllBallsPotted + SetPocketPositions + SetBallTransforms + radius + table surface) | Scripts/Gameplay |
| `Pocket.cs` | ✅ มี (trigger + tag "Pocket" + OnTriggerEnter → rules + tracker + deactivate ball) | Tables/Pocket.cs |
| ลูก spawn (R38) | ✅ Rigidbody + SphereCollider + ChinesePoolBallIdentifier (SetupBallComponents) | ChinesePoolBallSetup.cs |
| **tags "Ball"/"Pocket" ใน TagManager** | ❌ **ว่างเปล่า (`tags: []`)** — Pocket.cs ใช้ `CompareTag("Ball")` + `gameObject.tag = "Pocket"` → ไม่มี tags = ทำงานไม่ได้ | TagManager.asset |
| **Pocket component ใน AAA** | ❌ ไม่มี (โต๊ะ "AAA Table 12ft" มีแค่ BoxCollider 1 ตัว — หลุมไม่มี trigger) | scene |
| **BallPottedTracker ใน AAA** | ❌ ไม่มี | scene |
| **tag "Ball" บนลูก spawn** | ❌ ไม่ได้ set (BallSetup ไม่มี) | ChinesePoolBallSetup.cs |
| โต๊ะ AAA Table 12ft | position (0, 0.4, 0) scale (4, 0.5, 8) | scene line 73257 |

**ปัญหา:** ลูกมีฟิสิกส์ (R38) แต่ตกลงหลุมไม่ได้ — หลุมไม่มี trigger (Pocket component) + ไม่มี tracker ตอบสนอง + tags หาย

## งานจริง

1. **`PocketPhysicsSetup.cs`** (ใหม่, Editor): tool `Tools/CueStrike/Gameplay/150. Setup AAA Pocket Detection`
   - **เพิ่ม tags** "Ball" + "Pocket" ใน TagManager (ผ่าน SerializedObject กับ EditorBuildSettings? — ใช้ SerializedObject กับ TagManager asset)
   - **สร้าง pocket 6 จุด** บนโต๊ะ AAA (มุม 4 + กลางขอบสั้น 2) — GameObject + SphereCollider (isTrigger) + `Pocket` component (Pocket.cs) — ตำแหน่งตาม scale โต๊ะ (x=±1.8, z=±3.6 / z=0) + y=0.42
   - **เพิ่ม BallPottedTracker** component + assign pocket positions + ball transforms (ผ่าน public methods — ลูก spawn runtime → tracker ต้องรองรับ runtime find)
   - Idempotent: รันซ้ำ skip + self-test + batchmode

2. **`ChinesePoolBallSetup.cs`** (แก้เล็กน้อย): `SetupBallComponents` → `ball.tag = "Ball"` (Pocket.cs ใช้ CompareTag)

3. **Compile verify:** batchmode 0 errors (Library อุ่นบน main)
4. **รัน tool จริง** → AAA มี tags + 6 pockets + tracker
5. **Verify:** scene YAML มี Pocket ×6 + BallPottedTracker + TagManager มี Ball/Pocket + self-test + idempotency
6. **Docs:** CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md (R43 section)
7. **Commit + push + เปิด PR** ต่อ `main` (base = main ปัจจุบัน — ต้อง merge PR #37 (R42) ก่อน แตะ docs เดียวกัน)

## ผลลัพธ์ที่คาดหวัง
- ลูกวิ่งถึงหลุม → Pocket.OnTriggerEnter → BallPottedTracker.OnBallPotted → สกอร์/อีเวนต์จริง
- ลูกที่เข้าหลุมถูก deactivate (Pocket.cs) — เกมรู้ว่าลูกไหนหายไป

## ความเสี่ยง / หมายเหตุ
- งานนี้แตะ `ChinesePoolBallSetup.cs` (runtime) + `AAA_RoomDAY.unity` + TagManager + tool ใหม่
- Pocket.cs อ้าง CueStrikeRulesManager (ไม่มีใน AAA) — มี null-guard (?. ) → ไม่ crash แต่ rules.BallPotted ไม่ทำงาน → งานนี้ main deliverable = BallPottedTracker (event) ทำงาน
- **ต้อง merge PR #37 (R42) ก่อน** — แตะ docs เดียวกัน
- ไม่แตะ Snooker_Demo (R36 มี pockets แล้ว)
