# 🚦 Implementation Plan — Automated Compile Gate

> **Project:** CueStrike VR Billiards | **Date:** 2026-08-09
> **อนุมัติโดย:** พี่โม่ง (Project Owner) | **ผู้ทำ:** Buffy (Freebuff Dev Agent)
> **อ้างอิง:** `AI_TOOLS_MANDATE.md` (กฎข้อ 2, 4, 5) | **สถานะ:** 📝 Plan — รอลงมือ

---

## 🎯 เป้าหมาย

ตัดวงจร "เขียนโค้ด → compile พัง → แก้ → รันใหม่" ซ้ำๆ (หลักฐาน: `compile_fix_errors1-18.log` เกือบ 40 รอบ) ด้วย **compile gate อัตโนมัติ**:

1. **`tools/compile_check.sh`** — สคริปต์รัน Unity batchmode compile check (exit 0 = 0 errors)
2. **`.githooks/pre-commit`** — git hook บล็อก commit ที่ stage ไฟล์ `.cs` ถ้า compile ไม่ผ่าน (ข้ามได้ด้วย `git commit --no-verify`)

## 🔍 ข้อเท็จจริงที่ verify แล้ว (กฎข้อ 1)

| รายการ | ผล | หลักฐาน |
|--------|-----|---------|
| Unity Editor | `6000.4.4f1` ที่ `/c/Program Files/Unity/Hub/Editor/` | `ls` ✓ |
| batchmode compile | ใช้ได้จริง — 0 errors ทุกครั้งที่รัน (งาน R4/R5) | `compile_check_buffy.log` |
| LFS hooks | `.git/hooks/` มี `post-checkout/post-commit/post-merge` (จาก `git lfs install --local`) — **ต้องรักษาไว้** ถ้าเปลี่ยน `core.hooksPath` | `cat .git/hooks/*` |
| bash/sh | มีทั้งคู่ (Git Bash) | `which bash sh` |
| ไฟล์ log | `*.log` อยู่ใน .gitignore อยู่แล้ว → log ของ gate ไม่หลุด commit | `.gitignore` |

## 🏗️ การออกแบบ

```
tools/compile_check.sh      ← สคริปต์หลัก (รันด้วยมือได้ + hook เรียก)
.githooks/pre-commit        ← compile gate (versioned — แชร์ผ่าน repo ได้)
.githooks/post-checkout     ← copy จาก LFS (กัน hooksPath ทำ LFS พัง)
.githooks/post-commit       ← copy จาก LFS
.githooks/post-merge        ← copy จาก LFS
```

**Logic ของ compile_check.sh:**
- `UNITY_PATH` env ตั้งเองได้; ถ้าไม่ตั้ง → auto-detect (`6000.4.4f1` ก่อน, ไม่งั้นเวอร์ชันล่าสุดใน Hub)
- รัน `Unity.exe -batchmode -quit -nographics -projectPath … -logFile compile_gate.log`
- นับ `error CS` ใน log → `>0` = exit 1; ไม่พบ Unity = exit 2

**Logic ของ pre-commit (สมาร์ท — ไม่ช้าทุก commit):**
1. ไม่มีไฟล์ `.cs` ใน staged → **ข้าม** (exit 0) — commit เอกสาร/asset ไม่ต้องรอ compile
2. Unity Editor กำลังเปิด → **เตือน + ข้าม** (batchmode ชนกับ Editor ที่เปิดอยู่; ตามกฎข้อ 4 ต้องปิด Unity ก่อนทำงานโค้ดอยู่แล้ว)
3. มี `.cs` staged + Unity ปิด → รัน compile gate → fail = **บล็อก commit** (พร้อม hint `--no-verify`)

## 📋 ขั้นตอน

- [x] **ขั้น 1 — เขียน plan นี้** + update todos
- [x] **ขั้น 2 — สร้าง `tools/compile_check.sh`**
- [x] **ขั้น 3 — สร้าง `.githooks/pre-commit` + คัดลอก LFS hooks 3 ตัวเข้า `.githooks/`**
- [x] **ขั้น 4 — `git config core.hooksPath .githooks` + chmod +x**
- [x] **ขั้น 5 — ทดสอบ 3 ทาง**:
  - (a) รัน `tools/compile_check.sh` ตรงๆ → 0 errors ✅
  - (b) **Failure path**: `.cs` มี error (CS0029) → hook บล็อก exit 1 + โชว์ error ✅
  - (c) **Success path**: `.cs` ถูกต้อง → hook ผ่าน exit 0 ✅ (ลบ probe ทั้งสองแล้ว)
- [x] **ขั้น 6 — อัปเดตเอกสาร (กฎข้อ 2)**: `CUESTRIKE_MASTER.md` (§5) + `TASK_PROGRESS.md` (section) + `AI_TOOLS_MANDATE.md` (tool table row 3)
- [x] **ขั้น 7 — Commit** (commit นี้ไม่มี .cs → gate ข้ามตาม design)

## ⚠️ ความเสี่ยง & มาตรการ

| ความเสี่ยง | มาตรการ |
|-----------|---------|
| hook ช้าทุก commit | กรองเฉพาะ `.cs` staged + ข้ามเมื่อ Unity เปิด — commit เอกสาร/asset ไวเหมือนเดิม |
| LFS พังจาก hooksPath | คัดลอก LFS hooks 3 ตัวเข้า `.githooks/` |
| hook บล็อกงานด่วน | escape hatch `git commit --no-verify` + บันทึกใน docs |
| clone ใหม่ไม่รู้จัก hook | เอกสารบอกคำสั่ง `git config core.hooksPath .githooks` |

## ✅ Definition of Done (กฎข้อ 4)

- [x] `tools/compile_check.sh` exit 0 เมื่อ compile ผ่าน
- [x] pre-commit บล็อก commit ที่มี `.cs` พัง (ทดสอบจริงทั้ง fail + pass path)
- [x] LFS hooks ยังทำงาน (อยู่ใน .githooks)
- [x] เอกสารอัปเดตครบในรอบเดียวกัน

---
*Plan นี้เขียนก่อนลงมือตามกฎข้อ 5 — 2026-08-09*
