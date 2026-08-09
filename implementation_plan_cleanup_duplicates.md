# 🧹 Implementation Plan — Duplicate Cleanup (House Cleaning R3)

> **Project:** CueStrike VR Billiards | **Date:** 2026-08-09
> **อนุมัติโดย:** พี่โม่ง (Project Owner) | **ผู้ทำ:** Buffy (Freebuff Dev Agent)
> **อ้างอิง:** `AI_TOOLS_MANDATE.md` (กฎข้อ 2, 4, 5) | **สถานะ:** 📝 Plan — รอลงมือ

---

## 🎯 เป้าหมาย

จัดการงานค้างจาก **House Cleaning R2** (TASK_PROGRESS.md): ไฟล์ซ้ำ 5 คู่ + ไฟล์ขยะ `UnityEngine.XR.Hands.cs` (0 ไบต์) — verify reference ทีละคู่ตามที่ R2 กำหนด แล้วลบเฉพาะที่พิสูจน์ว่าไม่มีใครใช้

## 🔍 ผล verify (กฎข้อ 1 — ใช้ tool ตรวจจริงทุกคู่)

### เกณฑ์ตัดสิน
- **ลบ** = ไม่มี reference ในโค้ด (`.cs`) + ไม่มี GUID ใน prefab/scene/asset
- **เก็บ** = มี reference จริง (โค้ด/ฉาก) — ไม่ใช่ duplicate แท้

| คู่ | เวอร์ชัน A | เวอร์ชัน B | ผลตรวจ | การตัดสินใจ |
|----|-----------|-----------|--------|-------------|
| **CrowdSystem** | `Characters/` (1150L, ns `CueStrike.Characters`, GUID `963b769b…`) | `MascotSystem/` (395L, ns `CueStrike.MascotSystem`, GUID `500f6119…`) | ⚠️ **วิเคราะห์ครั้งแรกผิด**: `CueStrikeMascotManager.cs` อยู่ใน ns `CueStrike.Characters` → ชื่อ unqualified `CueStrikeCrowdSystem` resolve ไปที่เวอร์ชัน A (same-namespace) แม้จะมี `using CueStrike.MascotSystem` (ใช้สำหรับ type อื่น); เวอร์ชัน A มี `enum CrowdReactionType` ที่ MascotManager ใช้; เวอร์ชัน B ไม่มี ref + GUID=0 + ไม่มี `CrowdReactionType` | 🗑️ **ลบ B** (เก็บ A) — ตรวจ compile จับ error CS0426 ได้ก่อน commit แล้วแก้ถูก |
| **BallSync** | `Multiplayer/` (79L, guarded, GUID `ee67a4b7…`) | `Scripts/Multiplayer/Normcore/` (251L, **ไม่มี guard**, GUID `4461496d…`) | ทั้ง 2 ไม่มี ref ภายนอก (เวอร์ชัน B อ้างตัวเองใน self-test เท่านั้น); GUID ทั้งคู่ = 0 ใน prefab/scene/asset; เวอร์ชัน B ผิดกฎข้อ 4 (Normcore ต้องมี `#if CUESTRIKE_NORMCORE`); มีแค่ self-test menu `Tools/CueStrike/Debug/Test Ball Sync` (ไม่ได้อยู่ในรายการ tools เอกสาร) | 🗑️ **ลบ B** (เก็บ A แบบ guarded) |
| **GameSync** | `Multiplayer/` (162L, guarded, GUID `9d0989d6…`) | `Scripts/Multiplayer/Normcore/` (377L, **ไม่มี guard**, GUID `3ee40b82…`) | เวอร์ชัน A ถูกใช้จริง: `Editor/CueStrikeMultiplayerSetup.cs` (fully-qualified) + `CueStrikeGameSyncModel.cs`; เวอร์ชัน B ไม่มี ref ภายนอก + ผิดกฎข้อ 4; self-test menu `Tools/CueStrike/Debug/Test Game Sync` | 🗑️ **ลบ B** (เก็บ A) |
| **NormcoreManager** | `Multiplayer/` (100L, guarded, GUID `1389c4d4…`) | `Scripts/Multiplayer/Normcore/` (566L, GUID `903dddea…`) | **ใช้ทั้งคู่**: A ← `Editor/CueStrikeMultiplayerSetup.cs`; B ← `Editor/CueStrikeNormcoreSetup.cs`, `Editor/IntegrationSelfTest.cs`, `Editor/MultiplayerSelfTest.cs` (fully-qualified + `using`) | ✅ **เก็บทั้งคู่** (ไม่ใช่ duplicate แท้) |
| **CallShotUI** | `Scripts/ChinesePool/` (280L, ns `CueStrike.Gameplay.ChinesePool`, GUID `c743997f…`) | `Scripts/UI/ChinesePool/` (249L, ns `CueStrike.UI.ChinesePool`, GUID `0d69029a…`) | **ใช้ทั้งคู่**: A ← `ChinesePoolGameManager.cs` (FindFirstObjectByType ใน ns เดียวกัน); B ← `ChinesePoolUIManager.cs` + `Editor/ChinesePoolUISetup.cs` + **GUID อยู่ใน 2 scenes** (`AAA_RoomDAY`, `Title_NoksGrandHall`) | ✅ **เก็บทั้งคู่** (ไม่ใช่ duplicate แท้) |
| **XR Hands stub** | `RCA/UnityEngine.XR.Hands.cs` (0 ไบต์) | — | ไฟล์ว่าง ประกาศอะไรไม่ได้ → ไม่มีทางถูกอ้างถึง; XR Hands จริง (1.5.0) อยู่ใน manifest แล้ว | 🗑️ **ลบ** |

### สรุป: ลบ 4 ไฟล์ (+ .meta), เก็บ 2 คู่ (NormcoreManager, CallShotUI) พร้อมบันทึกเหตุผล
> 🔑 **บทเรียน:** ตรวจ namespace resolution ของ caller (same-namespace ชนะ `using`) — อย่าด่วนสรุปจาก `using` อย่างเดียว

## 📋 ขั้นตอน

- [x] **ขั้น 1 — เขียน plan นี้** + update todos
- [x] **ขั้น 2 — ยืนยัน GUID 0 refs อีกรอบ** ทั่ว Assets (ทุกนามสกุล) ก่อนลบจริง — 3 GUIDs = 0 refs ✓
- [x] **ขั้น 3 — ลบ 4 ไฟล์ + .meta**: `RCA/UnityEngine.XR.Hands.cs`, `Characters/CueStrikeCrowdSystem.cs`, `Scripts/Multiplayer/Normcore/CueStrikeBallSync.cs`, `Scripts/Multiplayer/Normcore/CueStrikeGameSync.cs` — ลบแล้ว 8 ไฟล์ ✓
- [x] **ขั้น 4 — อัปเดตเอกสาร (กฎข้อ 2)**: `TASK_PROGRESS.md` (ตาราง 5 คู่ → สถานะใหม่) + `CUESTRIKE_MASTER.md` (§5 + P11) — ชี้ `CueStrikeCrowdSystem.cs` ไปที่ `MascotSystem/` ✓
- [x] **ขั้น 5 — Compile verify**: batchmode 0 errors
- [x] **ขั้น 6 — Commit**

## ⚠️ ความเสี่ยง & มาตรการ

| ความเสี่ยง | มาตรการ |
|-----------|---------|
| ลบไฟล์ที่ถูกอ้างทางอ้อม | ✅ ตรวจ GUID ทุกไฟล์ใน Assets (ไม่ใช่แค่ prefab/unity/asset) + grep class name ทุก .cs |
| self-test menus หาย (`Test Ball Sync`, `Test Game Sync`) | บันทึกใน docs — ทดแทนด้วย `MultiplayerSelfTest.cs` / `IntegrationSelfTest.cs` ที่ยังอยู่ |
| duplicate "ซ่อน" หลัง cleanup | เอกสารบันทึกชัดเจนว่า 2 คู่ที่เก็บไว้ **ไม่ใช่ duplicate แท้** (ต่าง namespace, ต่าง consumer, อ้างถึงจริง) |

## ✅ Definition of Done (กฎข้อ 4)

- [x] ลบไฟล์ที่ไม่มี ref ครบ 4 (+ .meta) — compile ยัง **0 errors**
- [x] เอกสารอัปเดต: ตาราง R2 ไม่อ้างไฟล์ที่ลบแล้ว
- [x] Commit สะอาด (ไม่รวมไฟล์ unrelated)

---
*Plan นี้เขียนก่อนลงมือตามกฎข้อ 5 — 2026-08-09*
