# 🎯 Implementation Plan — Merge ChinesePoolCallShotUI (2 เวอร์ชัน → 1)

> **Project:** CueStrike VR Billiards | **Date:** 2026-08-09
> **อนุมัติโดย:** พี่โม่ง (Project Owner) | **ผู้ทำ:** Buffy (Freebuff Dev Agent)
> **อ้างอิง:** `AI_TOOLS_MANDATE.md` (กฎข้อ 2, 4, 5) | **สถานะ:** 📝 Plan — รอลงมือ

---

## 🎯 เป้าหมาย

รวม `ChinesePoolCallShotUI` สองเวอร์ชัน (ต่าง namespace) เป็นตัวเดียวที่ `GameManager` + `UIManager` ใช้ร่วมกัน — แก้บั๊ก `FindFirstObjectByType` หาไม่เจอ (GameManager ค้นหาเวอร์ชัน Gameplay แต่ฉากมีเวอร์ชัน UI)

## 🔍 ผลวิเคราะห์ (กฎข้อ 1 — verify ทุกไฟล์)

| เวอร์ชัน | ไฟล์ | สถานะ |
|----------|------|-------|
| **Gameplay** (ns `CueStrike.Gameplay.ChinesePool`) | `Scripts/ChinesePool/ChinesePoolCallShotUI.cs` (280L, TMP) | ❌ **ไม่มีใครใช้จริง**: API `ShowCallShotUI/HideCallShotUI/ShowCallShot(static)` ไม่มี caller; ฟีเจอร์ highlight พัง (`GetBallIdFromButtonIndex` return -1); GUID `c743997f…` = 0 ref ใน prefab/scene/asset; GameManager อ้างแค่ field + Find (ไม่เคยเรียก API) |
| **UI** (ns `CueStrike.UI.ChinesePool`) | `Scripts/UI/ChinesePool/ChinesePoolCallShotUI.cs` (249L, legacy Text) | ✅ **ตัวจริงที่ใช้**: อยู่ใน 2 scenes (`AAA_RoomDAY`, `Title_NoksGrandHall`) พร้อม serialized fields; UIManager เรียก `ShowCallShot(bool, int)`; UISetup AddComponent; GUID `0d69029a…` |

**Consumer map:**
- `ChinesePoolGameManager.cs` (ns Gameplay) → field + `FindFirstObjectByType<ChinesePoolCallShotUI>()` — **ปัจจุบันหาเวอร์ชัน Gameplay (ไม่มีในฉาก) → เจอ null = บั๊ก**
- `ChinesePoolUIManager.cs` (ns UI) → `_callShotUI?.ShowCallShot(isOpenTable, playerGroup)` — ใช้เวอร์ชัน UI ✓
- `Editor/ChinesePoolUISetup.cs` → `AddComponent<UI.ChinesePool.ChinesePoolCallShotUI>` — เวอร์ชัน UI ✓
- `CueStrikeChinesePoolRuleset.cs:266` → `_chinesePoolMgr.SetCallShot(ballId, pocketIndex)` — เรียกผ่าน GameManager (ไม่เกี่ยวกับ class UI)
- `ChinesePoolMatchState.cs:6` → แค่ doc comment

## 🏗️ การออกแบบ (เลือก "keep UI version" เพราะฉากผูก GUID ไว้)

1. **เก็บ** `Scripts/UI/ChinesePool/ChinesePoolCallShotUI.cs` (namespace `CueStrike.UI.ChinesePool` เดิม — scene data ปลอดภัย ไม่แตะ GUID/field names)
2. **ลบ** `Scripts/ChinesePool/ChinesePoolCallShotUI.cs` + `.meta` (dead code — API ไม่มี caller, highlight พัง)
3. **แก้ `ChinesePoolGameManager.cs`**: เพิ่ม `using CueStrike.UI.ChinesePool;` → `FindFirstObjectByType<ChinesePoolCallShotUI>()` resolve ไปที่เวอร์ชัน UI ที่อยู่ในฉาก → **เจอจริง** (บั๊กหาย)
4. ผลลัพธ์: class เดียว `CueStrike.UI.ChinesePool.ChinesePoolCallShotUI` — GameManager + UIManager ใช้ร่วมกัน

> ⚠️ **API ของเวอร์ชัน Gameplay ที่ถูก drop** (`ShowCallShotUI(playerIndex, balls[], pockets[], callback)`): ไม่มี caller ไหนใช้เลย + ตัว implementation พัง — ไม่มีผลต่อฟีเจอร์ปัจจุบัน

## 📋 ขั้นตอน

- [x] **ขั้น 1 — เขียน plan นี้** + update todos
- [x] **ขั้น 2 — ลบ** `Scripts/ChinesePool/ChinesePoolCallShotUI.cs` + `.meta` (GUID `c743997f…` verify 0 ref แล้ว)
- [x] **ขั้น 3 — แก้ `ChinesePoolGameManager.cs`**: เพิ่ม `using CueStrike.UI.ChinesePool;`
- [x] **ขั้น 4 — Compile verify** batchmode → **0 errors** (`compile_check.sh` exit 0)
- [x] **ขั้น 5 — Verify scene data**: scenes ยังอ้าง GUID `0d69029a…` เดิม ✓ (2 scenes)
- [x] **ขั้น 6 — อัปเดตเอกสาร (กฎข้อ 2)**: `CUESTRIKE_MASTER.md` (§5) + `TASK_PROGRESS.md` (section) + plan checkboxes
- [x] **ขั้น 7 — Commit** (ทดสอบ compile gate path จริง)

## ⚠️ ความเสี่ยง & มาตรการ

| ความเสี่ยง | มาตรการ |
|-----------|---------|
| Scene data พังถ้าเปลี่ยน class ผิดตัว | ✅ เก็บไฟล์ที่ GUID อยู่ในฉาก (UI version) — ไม่แตะ namespace/field names |
| GameManager หา class ไม่เจอระหว่างแก้ | เพิ่ม `using` ในไฟล์เดียวกับที่ลบไฟล์เก่า → compile หลังจบทั้งคู่ |
| Ambiguity จาก 2 namespace | ลบ Gameplay version ก่อน → เหลือ class เดียว ไม่มีทางชน |
| ฟีเจอร์ call-shot ยังไม่ต่อครบวงจร | `OnShotCalled` ยังไม่มี subscriber → **นอก scope งานนี้** (บันทึกเป็นงานถัดไป: ผูก `OnShotCalled` → `GameManager.SetCallShot`) |

## ✅ Definition of Done (กฎข้อ 4)

- [x] เหลือ `ChinesePoolCallShotUI` ตัวเดียว (ns `CueStrike.UI.ChinesePool`)
- [x] `GameManager.FindFirstObjectByType` resolve ไปเวอร์ชันในฉาก — compile **0 errors**
- [x] เอกสารอัปเดตครบในรอบเดียวกัน

---
*Plan นี้เขียนก่อนลงมือตามกฎข้อ 5 — 2026-08-09*
