# Implementation Plan — เก็บ AIVisionAuditTests.cs (Automation Audit ถาวร)

**วันที่:** 2026-08-12
**Branch:** `feat/ai-vision-audit-tests` → `main`
**กฎ:** กฎข้อ 1 (ตรวจของจริง) + กฎข้อ 5 (plan ก่อน, docs + compile ในรอบเดียวกัน)

## เป้าหมาย
เปิด PR เก็บ `AIVisionAuditTests.cs` เป็น automation audit ถาวร (ต่อยอด R14R16R17PlayModeTests ที่มีอยู่) — ตรวจอัตโนมัติ:
1. **Prerequisites:** AAA_RoomDAY มี GameManager + BallSetup + AIModifier + bridge wired (R37/R38)
2. **AI ยิงจริง:** ตั้ง Expert → StartNewFrame → NextPlayer (AI เทิร์น) → รอ 25s → ลูก cue ขยับ > 0.05

## สถานะจริงที่ตรวจพบ (กฎข้อ 1)

| รายการ | สถานะ | หลักฐาน |
|--------|--------|---------|
| `AIVisionAuditTests.cs` | ✅ เขียน + **รันผ่าน 3 รอบ** (cueMoved=True ~5.59 หน่วย) | รอบ 1-3 audit log |
| meta GUID | ✅ Unity สร้าง (`050590c9...`) — ตรงกับที่ใช้รัน | meta file |
| Test framework เดิม | ✅ `CueStrike.PlayModeTests.asmdef` + `R14R16R17PlayModeTests.cs` (reflection pattern) | Tests/PlayMode/ |
| PR #38 (R43) | ⏳ IN_PROGRESS — ต้อง merge ก่อน (แตะ docs เดียวกัน) | CI |

## งานจริง

1. **merge PR #38 (R43)** — ถ้า CI เขียว (แตะ docs เดียวกัน)
2. **สร้าง worktree** `feat/ai-vision-audit-tests` จาก main ใหม่
3. **copy `AIVisionAuditTests.cs` + meta** ไป worktree (test ผ่านแล้ว — ไม่แก้โค้ด)
4. **Compile verify:** batchmode PlayMode test รันซ้ำ — 0 errors + test ผ่าน (บน worktree หรือ main Library อุ่น)
5. **Docs:** TASK_PROGRESS.md + CUESTRIKE_MASTER.md + task.md (เพิ่ม section: AI Vision Audit Tests)
6. **Commit + push + เปิด PR** ต่อ `main`

## ผลลัพธ์ที่คาดหวัง
- ทุก PR ต่อไปมี automation ตรวจ AI ยิงจริง (PlayMode) — ลดการพึ่ง manual Vision audit
- ครอบคลุม R34-R38 (Practice AI + modifier + BallSetup) — จับ regression ได้

## ความเสี่ยง / หมายเหตุ
- test นี้รันผ่านแล้ว 3 รอบบน main (รวม R42) — พร้อม commit
- ต้อง merge PR #38 ก่อน (แตะ TASK_PROGRESS เดียวกัน)
- ไม่แตะโค้ด runtime — แค่เพิ่ม test file + docs
