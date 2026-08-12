# Implementation Plan — R39: วาง ChinesePoolScoreboard ลง Title_NoksGrandHall

**วันที่:** 2026-08-12
**Branch:** `feat/r39-title-scoreboard` → `main` (base `f30fdc0` = R38 merged)
**กฎ:** กฎข้อ 1 (ตรวจของจริง) + กฎข้อ 5 (plan ก่อน, docs + compile ในรอบเดียวกัน)

## เป้าหมาย
วาง `ChinesePoolScoreboard` ลง Title_NoksGrandHall เพื่อให้ Bo Comedy "มึนสกอร์เสมอ" ทำงานใน lobby เหมือนห้องแข่ง (AAA_RoomDAY)

## สถานะจริงที่ตรวจพบ (กฎข้อ 1)

| รายการ | สถานะ | หลักฐาน |
|--------|--------|---------|
| `ChinesePoolScoreboard` component ใน Title | ❌ **ไม่มีเลย** (grep=0) | `Title_NoksGrandHall.unity` |
| `ChinesePoolUIManager` ใน Title | ✅ มี (grep=1) | Title |
| BoPanda instance ใน Title | ✅ มี (grep=1) | Title (R29/R33 วางไว้) |
| `BoComedyDirector` (ใน BoPanda prefab) | ✅ prefab instance ได้ผลอัตโนมัติ | prefab มี (R32) |
| `BoScoreboardSetup` tool (R35) | ✅ มี — แต่ hardcode เฉพาะ AAA_RoomDAY | `BoScoreboardSetup.cs` |
| PR #33 (R38) | ✅ merged `f30fdc0` | — |

**ปัญหา:** Title ไม่มี scoreboard → Bo `FindAnyObjectByType<ChinesePoolScoreboard>` หาไม่เจอ → "มึนสกอร์" ไม่เกิดใน lobby (เหมือน AAA ก่อน R35)

## งานจริง

1. **ขยาย `BoScoreboardSetup.cs`** (R35 tool) ให้รองรับ 2 ฉาก:
   - เปลี่ยนจาก hardcode `AAA_RoomDAY` → array `{AAA_RoomDAY, Title_NoksGrandHall}`
   - `Run()` วนทุกลฉาก: สร้าง scoreboard + ผูก `ChinesePoolUIManager._scoreboard` (ถ้ามี UIManager ในฉาก) + self-test
   - Idempotent: รันซ้ำ skip (มีแล้ว)
   - Self-test: scoreboard มี + UIManager ref (ถ้ามี) + BoComedyDirector

2. **Compile verify:** batchmode 0 errors (Library อุ่นบน main)
3. **รัน tool จริง** → วาง scoreboard ลง Title (+ AAA skip — มีแล้ว)
4. **Verify:** Title scene YAML มี ChinesePoolScoreboard + UIManager._scoreboard ≠ 0 + idempotency
5. **Docs:** CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md (R39 section)
6. **Commit + push + เปิด PR** ต่อ `main`

## ผลลัพธ์ที่คาดหวัง
- Bo ใน Title (lobby) จะ subscribe `OnScoreChanged` ได้ → เมื่อสกอร์ P1==P2>0 → `SetTrigger("Speak")` = มึน "ใครชนะนะ??"
- Lobby มี scoreboard UI ด้วย (ต่อยอด R25)

## ความเสี่ยง / หมายเหตุ
- งานนี้แตะ `BoScoreboardSetup.cs` (มีใน main) + `Title_NoksGrandHall.unity`
- ไม่แตะ AAA_RoomDAY (มี scoreboard แล้ว — tool skip)
- ไม่แตะโค้ด runtime — ต่อยอดจาก R35 โดยตรง
