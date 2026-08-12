# Implementation Plan — R38: Fix BallSetup — AI ยิงไม่ได้เพราะฉากไม่มีลูก

**วันที่:** 2026-08-12
**Branch:** `feat/r38-ballsetup-fix` → `main` (base `5053f17` = R37 merged)
**กฎ:** กฎข้อ 1 (ตรวจของจริง) + กฎข้อ 5 (plan ก่อน, docs + compile ในรอบเดียวกัน)

## เป้าหมาย
แก้ Vision audit blocker (R34/R37): AI (Chinese Pool Practice) ยิงไม่ได้จริง
**ต้นตอตัวจริง = AAA_RoomDAY ไม่มี `ChinesePoolBallSetup` component** →
`StartNewFrame()` error ("Cannot start frame — ChinesePoolBallSetup is null") →
ไม่มีลูกบนโต๊ะ → AI ไม่มีลูกให้ยิง (ต่อให้ modifier/refs ครบจาก R37)

## สถานะจริงที่ตรวจพบ (กฎข้อ 1 — ผ่าน PlayMode test จริง)

| รายการ | สถานะ | หลักฐาน |
|--------|--------|---------|
| `ChinesePoolGameManager` ใน AAA | ✅ มี | `AAA_RoomDAY.unity` |
| `ChinesePoolAIModifier` + refs (R37) | ✅ มีแล้ว | merged `5053f17` |
| `CueStrikePracticeAIBridge` + refs | ✅ มีแล้ว | merged |
| **`ChinesePoolBallSetup` component ใน AAA** | ❌ **ไม่มีเลย** (grep=0) | `AAA_RoomDAY.unity` |
| `ChinesePoolBallSetup` ในฉากอื่น | ❌ **ไม่มีฉากไหนเลย** (grep scenes = ว่าง) | ทั้งโปรเจกต์ |
| prefab ลูก: `Pool_CueBall` / `Pool_Ball_01..15` | ✅ มีครบ | `Prefabs/Balls/Pool/` |
| หลักฐาน error จริง (PlayMode test) | ❌ `[CueStrike] Cannot start frame — ChinesePoolBallSetup is null!` | test_r37.xml |

**ปัญหา:** BallSetup หายจากฉาก → `StartNewMatch` → `StartNewFrame` error → เกมไม่เริ่ม → ไม่มีลูก → AI ยิงไม่ได้

## งานจริง

1. **เขียน Editor tool `ChinesePoolBallSetupFixer.cs`** (`Assets/CueStrike/Editor/`):
   - `[MenuItem("Tools/CueStrike/AI/120. Fix ChinesePool BallSetup (AAA_RoomDAY)")]`
   - เปิด `AAA_RoomDAY.unity`
   - ถ้ายังไม่มี `ChinesePoolBallSetup` → สร้าง GameObject `ChinesePoolBallSetup` + component
   - assign prefabs (จาก `Prefabs/Balls/Pool/`):
     - `cueBallPrefab` = `Pool_CueBall.prefab`
     - `redBallPrefab` = `Pool_Ball_01.prefab` (สีแดง 1-7)
     - `yellowBallPrefab` = `Pool_Ball_09.prefab` (สีเหลือง 9-15)
     - `blackBallPrefab` = `Pool_Ball_08.prefab` (8-ball)
   - assign `ChinesePoolGameManager.ballSetup` → component (SerializedObject)
   - Idempotent: รันซ้ำไม่สร้างซ้ำ / skip ถ้ามีครบ
   - Self-test: BallSetup มี + prefabs 4 ตัวครบ + GameManager.ballSetup ≠ 0
   - Batchmode: `-executeMethod ChinesePoolBallSetupFixer.RunFromBatch`

2. **Compile verify:** batchmode 0 errors (Library อุ่นบน main)
3. **รัน tool จริง** → เพิ่ม BallSetup + assign prefabs + assign GameManager.ballSetup + บันทึก scene
4. **Verify:** scene YAML มี BallSetup + refs ≠ 0 + idempotency
5. **Docs:** CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md (R38 section)
6. **Commit + push + เปิด PR** ต่อ `main`

## ผลลัพธ์ที่คาดหวัง
- `StartNewFrame` ทำงาน → 16 ลูก spawn (15 + cue) → เกมเริ่มได้
- AI มีลูกให้ยิงจริง → `DecideCallShot` → `AddForce` → ลูกขยับ
- (หมายเหตุ: `SetupRack` เป็น stub — ลูก spawn จาก prefab มี Rigidbody+Collider ครบ)

## ความเสี่ยง / หมายเหตุ
- งานนี้แตะ `AAA_RoomDAY.unity` เท่านั้น
- ไม่แตะโค้ด runtime (BallSetup มี logic ครบ — แค่ฉากขาด component + prefabs)
- ยังมี backlog: ระบบ pocket detection (BallPottedTracker) / ฟิสิกส์โต๊ะ อาจยังไม่ครบใน AAA — แต่เกิน scope R38 (ยิงได้ก่อน = ก้าวแรก)
