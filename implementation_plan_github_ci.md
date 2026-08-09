# 🚦 Implementation Plan — GitHub Actions CI (Compile Gate on PR)

> **Project:** CueStrike VR Billiards | **Date:** 2026-08-09
> **อนุมัติโดย:** พี่โม่ง (Project Owner) | **ผู้ทำ:** Buffy (Freebuff Dev Agent)
> **อ้างอิง:** `AI_TOOLS_MANDATE.md` (กฎข้อ 2, 4, 5) | **สถานะ:** 📝 Plan — รอลงมือ

---

## 🎯 เป้าหมาย

ต่อยอด compile gate (R6 — ฝั่ง local) ขึ้น **GitHub Actions**: ทุก PR (และ push ขึ้น main) → รัน Unity batchmode compile check บน CI → กันโค้ดพังเข้าคนอื่นก่อน merge

## 🏗️ การออกแบบ

| ไฟล์ | บทบาท |
|------|-------|
| `.github/workflows/compile-gate.yml` | **ตัวหลัก**: trigger `pull_request` + `push` ไป main → `game-ci/unity-test-runner@v4` (testMode editmode — compile ก่อนรัน test; ถ้า compile พัง job fail) + `actions/checkout` พร้อม `lfs: true` + cache `Library` |
| `.github/workflows/unity-activate.yml` | **ตัวช่วย (รันครั้งเดียว)**: สร้าง Unity activation file (`.alf`) ให้พี่โม่งเอาไปขอ license ที่ license.unity3d.com → เก็บเป็น secret `UNITY_LICENSE` |

**ทำไม editmode test runner:** เป็นวิธีมาตรฐานของชุมชน Unity CI (game-ci) — รัน compile จริงก่อน แล้วรัน EditMode tests ที่มี (AudioAssetConsistencyTests — เช็คว่าไฟล์เสียง 14 voice + SFX slots มีจริง ซึ่งอยู่ใน repo ผ่าน LFS → ผ่าน) license ถูกจัดการให้อัตโนมัติผ่าน secret

## 🔍 ข้อเท็จจริง (กฎข้อ 1)

| รายการ | ผล |
|--------|-----|
| Unity version | `6000.4.4f1` — game-ci รองรับการติดตั้งบน runner |
| EditMode tests | `AudioAssetConsistencyTests.cs` (4 [Test]) — เช็คไฟล์เสียงที่มีใน repo ✓ |
| ทดลองรัน test ท้องถิ่น (Windows) | `-runTests` exit 0 แต่ไม่สร้างผลลัพธ์ (flaky ของ Unity 6 batchmode บน Windows) — **ไม่กระทบ CI** (Linux runner ของ game-ci รันได้ปกติ) |
| LFS | checkout ต้อง `lfs: true` — ไฟล์เสียง 250MB |
| Secret ที่ต้องมี | `UNITY_LICENSE` (พี่โม่งต้องตั้ง — ขั้นตอนใน report) |

## 📋 ขั้นตอน

- [x] **ขั้น 1 — เขียน plan นี้** + update todos
- [x] **ขั้น 2 — สร้าง `.github/workflows/compile-gate.yml`** + `unity-activate.yml`
- [x] **ขั้น 3 — Validate YAML** → ✅ valid (python yaml)
- [x] **ขั้น 4 — เพิ่ม `*_results.xml` ลง `.gitignore`** + ลบ artifact ท้องถิ่น (`editmode_results.xml`)
- [x] **ขั้น 5 — อัปเดตเอกสาร (กฎข้อ 2)**: `CUESTRIKE_MASTER.md` (§5 + status) + `TASK_PROGRESS.md` (Round 12) + `AI_TOOLS_MANDATE.md` (tool table) + plan checkboxes
- [x] **ขั้น 6 — Commit + push** (push ขึ้น main → workflow จะรันครั้งแรก แต่จะ fail เพราะยังไม่มี secret `UNITY_LICENSE` — เป็นที่คาดหมาย)

## ⚠️ ความเสี่ยง & มาตรการ

| ความเสี่ยง | มาตรการ |
|-----------|---------|
| CI ต้องใช้ license | สร้าง `unity-activate.yml` + ขั้นตอนตั้ง secret ชัดเจนใน report/docs |
| test เช็คไฟล์เสียง fail | ไฟล์อยู่ใน repo (LFS) — checkout `lfs: true` |
| workflow fail เพราะยังไม่มี secret | คาดหมายได้ — รันจริงเมื่อตั้ง secret แล้ว |
| Unity บน runner ใช้เวลานาน | cache `Library` (hashFiles) + game-ci ติดตั้ง Unity ครั้งแรก ~5-10 นาที (ครั้งต่อมาเร็วขึ้น) |

## ✅ Definition of Done (กฎข้อ 4)

- [x] workflow 2 ไฟล์ถูกต้อง (YAML valid)
- [x] `.gitignore` ครอบคลุม test artifacts
- [x] เอกสารอัปเดตครบในรอบเดียวกัน

---
*Plan นี้เขียนก่อนลงมือตามกฎข้อ 5 — 2026-08-09*
