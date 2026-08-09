# 🔗 Implementation Plan — Complete Call-Shot Wiring (R11)

> **Project:** CueStrike VR Billiards | **Date:** 2026-08-09
> **อนุมัติโดย:** พี่โม่ง (Project Owner) | **ผู้ทำ:** Buffy (Freebuff Dev Agent)
> **อ้างอิง:** `AI_TOOLS_MANDATE.md` (กฎข้อ 2, 4, 5) | **สถานะ:** 📝 Plan — รอลงมือ

---

## 🎯 เป้าหมาย

ต่อวงจร call-shot ให้ครบ: เมื่อผู้เล่นเลือก ball + pocket ใน `ChinesePoolCallShotUI` → ส่งไป `ChinesePoolGameManager.SetCallShot`; กดยกเลิก → `ClearCallShot` (ต่อจาก R7 ที่รวม class และแก้ `FindFirstObjectByType` แล้ว)

## 🔍 ข้อเท็จจริง (กฎข้อ 1)

| รายการ | ผล |
|--------|-----|
| `OnShotCalled` | `event System.Action<int, int>` (ballNumber, pocketIndex) — `ChinesePoolCallShotUI.cs:14` |
| `OnCallShotCancelled` | `event System.Action` — `:15` |
| `SetCallShot(int ballId, int pocketId)` | public, validate ball 1-15 / pocket 0-5 — `GameManager.cs:260` |
| `ClearCallShot()` | public — `GameManager.cs:281` |
| จุดหา `callShotUI` | `AutoWireReferences()` (เรียกจาก `Start`) — หลัง block ค้นหา |
| `OnDestroy` | มีแล้ว (`Instance = null`) — ใส่ unsubscribe ที่นี่ |
| Ruleset `:266` เรียก `SetCallShot` ใน `OnBallPotted` | เป็นคนละความหมาย (บันทึกผลลูกที่หลุม) — **ไม่แตะ** |

## 📋 ขั้นตอน

- [x] **ขั้น 1 — เขียน plan นี้** + update todos
- [x] **ขั้น 2 — แก้ `ChinesePoolGameManager.cs`**:
  - ใน `AutoWireReferences()` หลังหา `callShotUI` → subscribe `OnShotCalled += SetCallShot` + `OnCallShotCancelled += ClearCallShot`
  - ใน `OnDestroy()` → unsubscribe ทั้งคู่ (event hygiene)
- [x] **ขั้น 3 — Compile verify** → **0 errors** (`compile_check.sh` exit 0)
- [x] **ขั้น 4 — อัปเดตเอกสาร (กฎข้อ 2)**: `CUESTRIKE_MASTER.md` (§5) + `TASK_PROGRESS.md` (Round 11) + plan checkboxes
- [x] **ขั้น 5 — Commit + push origin**

## ⚠️ หมายเหตุ / งานถัดไป

- **ฝั่ง "แสดง UI" ยังไม่มี trigger**: ไม่มีใครเรียก `callShotUI.ShowCallShot` / `UIManager.ShowCallShot` — ต้องออกแบบเกมโฟลว์ (เมื่อถึงตาต้องเรียก → โชว์ panel) เป็นงานถัดไป
- UI ในฉากบาง instance ยังมี field ว่าง (`_callShotPanel: {fileID: 0}`) — ต้อง assign ใน Editor + Vision audit (กฎข้อ 4) ก่อนเล่นจริง

## ✅ Definition of Done (กฎข้อ 4)

- [x] `OnShotCalled` → `SetCallShot`, `OnCallShotCancelled` → `ClearCallShot` (subscribe + unsubscribe)
- [x] Compile **0 errors**
- [x] เอกสารอัปเดตครบในรอบเดียวกัน

---
*Plan นี้เขียนก่อนลงมือตามกฎข้อ 5 — 2026-08-09*
