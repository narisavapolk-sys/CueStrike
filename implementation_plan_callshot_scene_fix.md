# Implementation Plan: CallShot UI Scene Fix (Round 15)

**Date:** 2026-08-10 | **Rule 5:** Plan before action | **Rule 1:** All facts verified with tools

## Objective
แก้ field ว่างของ `ChinesePoolCallShotUI` ใน 2 ฉาก (`AAA_RoomDAY`, `Title_NoksGrandHall`)
ให้ call-shot panel ทำงานจริง (เปิด PR ตามที่สั่ง)

## Facts (verified 2026-08-10)
- `ChinesePoolCallShotUI` (ns `CueStrike.UI.ChinesePool`, GUID `0d69029a…`) มี 9 serialized refs:
  `_callShotPanel` (GO), `_titleText`/`_instructionText`/`_selectedBallText`/`_selectedPocketText` (Text ×4),
  `_ballSelectionGrid`/`_pocketSelectionGrid` (Transform ×2), `_confirmButton`/`_cancelButton` (Button ×2)
- **ทั้ง 2 ฉากมี pattern เหมือนกัน (copy-paste):** `CallShot_Panel` (GO `1107091477`) มี
  component `ChinesePoolCallShotUI` **2 ตัวซ้อน**:
  - `1107091479` — **ว่างทั้ง 9 refs** (`_callShotPanel: {fileID: 0}`)
  - `1107091480` — **ครบทั้ง 9 refs** (ชี้ลูกๆ ของ panel: `1935717004`, `1361074516`, …)
- **`ChinesePoolUIManager._callShotUI` ชี้ไปที่ตัวว่าง** (`1107091479`) — ทั้ง 2 ฉาก (AAA:27041, Title:28484)
  → `ShowCallShot` เรียกบนตัวว่าง → `_callShotPanel == null` → **panel ไม่เคยโชว์** (ต้นตอที่แท้จริง)
- `GameManager.FindFirstObjectByType<ChinesePoolCallShotUI>()` เจอตัวว่างก่อน (อยู่ในรายการแรกของ GO เดียวกัน)
- ฉากมี ref ของ `1479` แค่ 3 จุด: UIManager._callShotUI + GO.m_Component + block ตัวเอง
- **AAA_RoomDAY.unity ใน main checkout มีงาน uncommitted ของอีก session** → ต้องทำงานใน worktree (จาก origin/main สะอาด)

## Design (fix = ลบตัวซ้ำ ไม่ใช่ assign ใส่ตัวว่าง)
1. **ลบ component ว่าง** (`_callShotPanel == null`) เฉพาะเมื่อ GO นั้นมีตัวครบ (`_callShotPanel != null`) ซ้อนอยู่
2. **ย้าย `UIManager._callShotUI`** จากตัวที่ถูกลบ → ไปชี้ตัวที่รอด (ตัวครบ)
3. ผลลัพธ์: เหลือ 1 component ต่อฉาก (ครบ 9 refs) + UIManager ชี้ถูก → GameManager/FindFirstObjectByType เจอตัวจริง
4. ทำผ่าน **Editor tool + batchmode** (ตาม convention R4/R13 — ไม่แตะ YAML มือ)

**นอก scope:** field ว่างอื่นของ UIManager (`_scoreboard`, `_gameStateText`, `_foulNotification`, `_currentPlayerText`, `_turnIndicator`) — บันทึกเป็น follow-up

## Steps
1. เขียน plan นี้ (Rule 5)
2. สร้าง `Assets/CueStrike/Editor/CallShotUISceneFixer.cs`:
   - เมนู `Tools/CueStrike/Fix/Fix CallShot UI Duplicate Components` + entry `FixAllScenes()` สำหรับ batchmode
   - `RemoveEmptyDuplicates()`: group `FindObjectsByType<ChinesePoolCallShotUI>` ตาม GO → DestroyImmediate ตัวว่าง (ถ้ามีตัวครบซ้อน)
   - `RepointUIManager()`: `SerializedObject` อ่าน `_callShotUI` → ถ้า null (หลังลบ) → ชี้ตัวรอดตัวแรก
   - ตรวจ/บันทึก log จำนวนที่เหลือต่อฉาก
3. สร้าง worktree branch `fix/callshot-ui-scene-refs` จาก origin/main + คัดลอก plan/tool เข้า
4. รัน batchmode ใน worktree: `-executeMethod CueStrike.Editor.CallShotUISceneFixer.FixAllScenes -quit`
   (รอบเดียวได้ทั้งแก้ฉาก + compile check ตาม Rule 4; fresh Library → อาจใช้เวลาหลายนาที)
5. Verify: grep ฉาก → เหลือ `1479` ไม่มี, มี `1480`; UIManager ชี้ `1480`; compile 0 errors; ไม่มี .meta noise (ถ้ามี revert ตาม R4)
6. Docs (Rule 2): CUESTRIKE_MASTER.md + TASK_PROGRESS.md (Round 15) + checkboxes (ใน worktree)
7. Commit + push branch + ให้ URL เปิด PR
8. ลบ worktree + ไฟล์ชั่วคราวใน main checkout

## Verification (Rule 4)
- `grep "guid: 0d69029a"` ในฉาก = 1 จุด (component block) + 1 ใน m_Component list ต่อฉาก
- UIManager `_callShotUI` ชี้ fileID ของตัวครบ (ไม่ใช่ 0)
- Compile 0 errors (batchmode exit 0)
