# Implementation Plan — R29: วาง UncleNok/BoPanda ลงฉากจริง + ยืนยัน Animation เล่น

> **กฎข้อ 5:** เขียน plan ก่อน implement — อัปเดตเอกสาร + verify compile ในรอบเดียวกัน
> **Branch:** `feat/r29-mascot-scenes` (base: `main` @ `a43fa94` รวม R28 SFX แล้ว)
> **วันที่:** 2026-08-12

---

## 1. เป้าหมาย (ตามคำสั่งพี่โม่ง)

ตรวจว่า UncleNok + BoPanda ถูกวางในฉากไหนบ้าง + animation ใหม่ (R27) จะเล่นในฉากนั้นจริงหรือไม่ — รายงาน + แก้ถ้าขาด

## 2. Findings จากการตรวจโค้ดจริง (กฎข้อ 1)

| # | รายการ | สถานะ |
|---|--------|--------|
| 1 | **BoPanda_Prefab** ใน Title (1.8, 0.4, -1.6) + prefab มี Animator + `UncleNok.controller` (R27 assign) | ✅ Animation **เล่นได้** |
| 2 | **UncleNok_Prefab ไม่ถูกวางในฉากไหนเลย** — มีแค่ `UncleNok_Placeholder` (cube, 0, 0.9, 2) ใน Title | ❌ Animation ไม่มีทางเล่น |
| 3 | prefab มี Animator + controller + UncleNokReferee (แต่ `_animator/_audioSource/_homePosition` ว่าง — R29 voice pinning แยก PR) | ⚠️ |
| 4 | ฉากห้องแข่งทั้ง 9 (AAA_RoomDAY, Snooker_Demo, ห้อง 8 ตัว) + MainMenu + Boot: **ไม่มี mascot/referee เลย** | ❌ |
| 5 | `CueStrikeMascotUncleNok` ใช้ `mascotAnimator` + `homePosition` — ไม่มีใน prefab (prefab ใช้ UncleNokReferee) | ℹ️ |

## 3. แผนงาน

### 3.1 เขียน Editor tool `MascotScenePlacementSetup.cs` (convention R24-R28)
- MenuItem: `Tools/CueStrike/Mascots/50. Place Mascots in Scenes`
- สำหรับแต่ละฉากเป้าหมาย:
  - **Title_NoksGrandHall**: ลบ `UncleNok_Placeholder` (cube) → วาง `UncleNok_Prefab` ที่ตำแหน่งเดิม (0, 0.9, 2) — ลุงโน๊กอยู่ฝั่งผู้เล่น, BoPanda อยู่ (1.8, 0.4, -1.6) ฝั่งเดิม
  - **AAA_RoomDAY** + **Snooker_Demo** (ฉากที่เล่นได้จริง): วาง `UncleNok_Prefab` เป็น referee ริมโต๊ะ
- Idempotent: ถ้ามี prefab instance อยู่แล้ว → ข้าม
- self-test: ตรวจทุกฉากมี mascot ครบ + Animator + controller

### 3.2 ทำไมวางแค่ 3 ฉาก?
- Title = Lobby หลัก (มี BoPanda อยู่แล้ว + งาน R24 tutorial) — ลุงโน๊กควรอยู่ที่นี่
- AAA_RoomDAY + Snooker_Demo = ฉากที่เล่นจริง (R26 mode selection โหลดได้) — referee ควรอยู่
- ห้อง 8 ตัว + MainMenu/Boot = ไม่ได้ถูกโหลดผ่าน flow ปัจจุบัน (ยกเว้นผ่าน scene picker) — ไม่บังคับตอนนี้

## 4. Verify

- [ ] Compile gate batchmode: 0 errors
- [ ] Scene load + Editor tool idempotent + self-test ผ่าน
- [ ] main checkout คืนสภาพสะอาด
- [ ] docs: CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md

## 5. ขอบเขตงาน (ไม่ทำ)

- ไม่ assign `_animator/_audioSource/_homePosition` ของ UncleNokReferee (งาน R29 voice pinning เดิม — แยก PR)
- ไม่เพิ่ม mascot ให้ห้อง 8 ตัว (เล่นผ่าน scene picker เท่านั้น)
- ไม่สร้าง animation ใหม่ (R27 มีครบแล้ว)
