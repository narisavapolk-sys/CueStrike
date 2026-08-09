# 🚀 Implementation Plan — Git Setup (Version Control)

> **Project:** CueStrike VR Billiards | **Date:** 2026-08-09
> **อนุมัติโดย:** พี่โม่ง (Project Owner) | **ผู้ทำ:** Buffy (Freebuff Dev Agent)
> **อ้างอิง:** `AI_TOOLS_MANDATE.md` (กฎข้อ 2, 4, 5) | **สถานะ:** 📝 Plan — รอลงมือ

---

## 🎯 เป้าหมาย

1. ตั้งค่า **git** ให้โปรเจกต์ CueStrike (โปรเจกต์หลัก = `CueStrike_Project/` ปัจจุบัน ยังไม่มี version control เลย)
2. สร้าง **`.gitignore`** — ยกเว้น caches, logs, junk artifacts, secrets
3. ตั้งค่า **Git LFS** สำหรับ binary assets (FBX/OBJ/PNG/WEBM/DLL) — Assets รวม 328MB
4. **Commit ครั้งแรก (baseline)** — สภาพโค้ดปัจจุบันทั้งหมด 284 ไฟล์ .cs + เอกสาร
5. อัปเดต `CUESTRIKE_MASTER.md` + `TASK_PROGRESS.md` ในรอบเดียวกัน (กฎข้อ 2)

## 🔍 ข้อเท็จจริงที่ verify แล้ว (กฎข้อ 1 — ห้ามเดา)

| รายการ | ผลตรวจ | หลักฐาน |
|--------|--------|---------|
| Git repository | ❌ ไม่มี `.git` ในโปรเจกต์หรือ parent | `find` ทั่ว `UnityProjects/` — ไม่พบ |
| Unity Editor | ✅ ปิดอยู่ (ปลอดภัยแก้ไฟล์ภายนอก) | `tasklist` — ไม่มี process Unity |
| Git + config | ✅ git 2.55.0, user.name=Mong, email ตั้งแล้ว | `git --version`, `git config` |
| Git LFS | ✅ ติดตั้งแล้ว 3.7.1 | `git lfs version` |
| API Keys | ⚠️ `api  key  ai audio/ElevenLabs KeY tester  for sound.txt` + `stability.ai.txt` (plaintext) | ขนาด 59B / 51B, ชื่อไฟล์บ่งชี้ |
| ffmpeg downloads | ⚠️ `api  key  ai audio/ffmpeg-9.0.tar` (98MB) + `Assets/ffmpeg-9.0.tar.xz` (12MB) | `ls -la` |
| junk artifacts | ⚠️ 67 ไฟล์ `*.log`, `err*.txt`, `%f`, `gold_hex.txt`, `filelist*.txt`, `all_scripts.txt` — ไม่มี doc อ้างถึง | grep เอกสารทั้งหมดไม่พบ |
| nested `CueStrike_Project/` | 📦 โฟลเดอร์ว่าง 0 ไฟล์ (git ไม่ track โฟลเดอร์ว่าง) | `find -type f` = 0 |
| Unity cache dirs | `Library/`, `Temp/`, `Logs/`, `UserSettings/` — ต้อง exclude | `ls -la` |
| โปรเจกต์หลัก | ✅ cwd นี้มี MCP layer (TASK_PROGRESS.md ยืนยันว่า `CueStrike_Project` = หลัก, parent = สำเนาเก่า) | `TASK_PROGRESS.md` §compile blocker |

## 📋 ขั้นตอน

- [x] **ขั้น 0 — สำรวจสถานะ** (ทำแล้ว ข้างบน)
- [ ] **ขั้น 1 — เขียน plan นี้** (ไฟล์นี้) + update todos
- [ ] **ขั้น 2 — สร้าง `.gitignore`**: Unity standard + `*.log` + `err*.txt` + `%f` + `gold_hex.txt` + `filelist*.txt` + `all_scripts.txt` + `api  key  ai audio/` + `*.tar`/`*.tar.xz`/`ffmpeg-9.0/` + `__pycache__/` + `.freebuff/` + `[Ll]ibrary/` + `[Tt]emp/` + `[Ll]ogs/` + `[Uu]serSettings/`
- [ ] **ขั้น 3 — สร้าง `.gitattributes` + Git LFS**: `*.fbx *.obj *.png *.webm *.dll *.wav *.mp4` → LFS
- [ ] **ขั้น 4 — `git init -b main` + `git lfs install --local`**
- [ ] **ขั้น 5 — `git add` + ตรวจสอบก่อน commit**:
  - `git status` ดูว่าไม่มี `Library/`, `Temp/`, secrets, junk
  - `git ls-files | grep -i -E "key|secret|token"` ต้องว่าง
  - ตรวจ LFS: `git lfs ls-files` หลัง commit
- [ ] **ขั้น 6 — Commit ครั้งแรก** (baseline) message อธิบายชัด
- [ ] **ขั้น 7 — อัปเดตเอกสาร (กฎข้อ 2)**: `CUESTRIKE_MASTER.md` (header + COMPLETED WORK) + `TASK_PROGRESS.md` (section ใหม่ + Last Updated)

## ⚠️ ความเสี่ยง & มาตรการ

| ความเสี่ยง | มาตรการ |
|-----------|---------|
| API keys หลุดเข้า commit | `api  key  ai audio/` อยู่ใน .gitignore + ตรวจ `git ls-files` ก่อน commit + **แนะนำพี่โม่งย้าย keys ออกนอกโปรเจกต์และ rotate** |
| Binary ใหญ่ทำ repo พอง (74MB .obj, 27MB .fbx, 28MB .webm) | Git LFS ทุก binary type — พร้อม push ขึ้น GitHub/GitLab ได้เลย |
| commit junk เข้า baseline | .gitignore ครอบคลุม + ตรวจ staged list ก่อน commit |
| nested โฟลเดอร์ว่าง | git ไม่ track โฟลเดอร์ว่างอยู่แล้ว — ไม่มีผล |

## ✅ Definition of Done (กฎข้อ 4)

- [ ] `git status` สะอาด, มี commit แรก
- [ ] `git ls-files` ไม่มี secrets / caches / junk
- [ ] เอกสารอัปเดตครบในรอบเดียวกัน (CUESTRIKE_MASTER + TASK_PROGRESS)
- [ ] ไม่แตะโค้ด C# — compile ไม่กระทบ (ไม่ต้องรัน Unity compile เพราะไม่มีการแก้ code)

---
*Plan นี้เขียนก่อนลงมือตามกฎข้อ 5 — 2026-08-09*
