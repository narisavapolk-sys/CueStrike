# Implementation Plan: Call-Shot Show Trigger (Round 14)

**Date:** 2026-08-10 | **Rule 5:** Plan before action | **Rule 1:** All facts verified with tools

## Objective
ปิดวงจร call-shot ให้ครบ (ต่อจาก R11 ที่ wiring `OnShotCalled`→`SetCallShot`,
`OnCallShotCancelled`→`ClearCallShot` เสร็จแล้ว): **เพิ่มฝั่ง "โชว์ UI"** — เมื่อถึงตาผู้เล่นจริง
ที่ต้องเรียก shot → GameManager สั่ง `ChinesePoolUIManager.Instance.ShowCallShot(...)` ให้ panel ปรากฏ

## Facts (verified 2026-08-10)
- Wiring R11 เสร็จแล้ว: `ChinesePoolGameManager.cs:121-122` (subscribe) + `:90-91` (unsubscribe) — commit `956e7cf`, push แล้ว
- **ไม่มีใครเรียก `ShowCallShot`** — `ChinesePoolUIManager.Instance` ประกาศ (line 26) แต่ 0 refs ภายนอก → panel ไม่เคยโชว์
- `ChinesePoolUIManager.ShowCallShot(bool isOpenTable, int playerGroup)` → forward ไป `_callShotUI` (มีในฉาก)
- `ChinesePoolCallShotUI.ShowCallShot` semantics: `isOpenTable` = title/instruction ต่างกัน, `playerGroup` **1=RED, 2=YELLOW** (0 = generic)
- `ChinesePoolGameManager.IsCallShotRequired()` (line 376) = `callShotRequired && phase != Break && phase != OpenTable` — พร้อมใช้
- `isAiTurn` = (currentPlayerIndex == 1 && aiModifier != null) — ตา AI ต้องไม่โชว์ panel
- `GetCurrentPlayerGroup()` (public) คืน `BallGroup` (Red/Yellow/None)
- `using CueStrike.UI.ChinesePool;` มีอยู่แล้วใน GameManager (R7 เพิ่ม) → เรียก `ChinesePoolUIManager.Instance` ได้ตรงๆ
- Flow: `NextPlayer()` ถูกเรียกทุก turn change; `HandleBreakOrOpenTable()` ปลายทางตอน break (กลุ่มถูก assign → Playing → ผู้เล่นคนเดิมเล่นต่อ)
- ไฟล์ GameManager ไม่โดนแตะจากงาน uncommitted/remote ของอีก session (ตรวจแล้ว)

## Design (v1 — minimal, conservative)
- Helper `MaybeShowCallShotUI()`:
  - `if (!IsCallShotRequired()) return;` (ไม่โชว์ตอน Break/OpenTable)
  - `if (isAiTurn) return;` (ไม่โชว์ตา AI)
  - `ChinesePoolUIManager.Instance?.ShowCallShot(false, BallGroupToPlayerGroup(GetCurrentPlayerGroup()));`
  - `BallGroupToPlayerGroup`: Red→1, Yellow→2, None→0 (UI แสดง generic text ถ้า 0)
- จุดเรียก:
  1. ท้าย `NextPlayer()` — ทุกการเปลี่ยนตา (foul/wrong-ball/ธรรมดา)
  2. ท้าย `HandleBreakOrOpenTable()` — หลัง assign กลุ่ม (ผู้เล่นเดิมเล่นต่อโดยไม่ผ่าน NextPlayer)
  - branch "none potted" → OpenTable + NextPlayer → `IsCallShotRequired()` false → ไม่โชว์ (ถูกต้อง)
- `isOpenTable: false` เสมอใน v1 (IsCallShotRequired() ไม่รวม OpenTable อยู่แล้ว)

## Steps
1. เขียน plan นี้ (Rule 5) ✅
2. แก้ `ChinesePoolGameManager.cs` (Private Helpers + NextPlayer + HandleBreakOrOpenTable) ✅ — helper `:388`, เรียก `:247` + `:438`
3. Compile verify: `tools/compile_check.sh` ✅ 0 errors (exit 0) — และ gate รันอัตโนมัติตอน commit
4. Docs (Rule 2): CUESTRIKE_MASTER.md (§5 + status R14) + TASK_PROGRESS.md (Round 14) + checkboxes ✅
5. Commit + พยายาม push (remote อาจยังติดจากงาน ExecuteCodeTool.cs ของอีก session — ถ้าติด รายงานตามจริง)

## Verification
- compile 0 errors (batchmode exit 0)
- grep ยืนยันจุดเรียก 2 จุด + helper ใหม่
- ไม่แตะไฟล์อื่น / ไม่มี .meta noise

## Known follow-ups (นอก scope v1)
- ตา AI ต่อจาก panel ที่โชว์ค้าง → panel อาจค้าง (ต้องการ hide logic — v2)
- UI ในฉากบาง instance field ว่าง (`_callShotPanel: {fileID: 0}` ฯลฯ) — ต้อง assign ใน Editor + Vision audit (กฎข้อ 4) ก่อนเห็นภาพจริง
