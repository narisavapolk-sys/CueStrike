# 🎮 Implementation Plan — VR Startup Duplicate Cleanup (R10)

> **Project:** CueStrike VR Billiards | **Date:** 2026-08-09
> **อนุมัติโดย:** พี่โม่ง (Project Owner) | **ผู้ทำ:** Buffy (Freebuff Dev Agent)
> **อ้างอิง:** `AI_TOOLS_MANDATE.md` (กฎข้อ 2, 4, 5) | **สถานะ:** 📝 Plan — รอลงมือ

---

## 🎯 เป้าหมาย

มี VR startup script 2 ตัวซ้ำกัน — หาว่าตัวไหนคือตัวจริง แล้วลบตัวที่ซ้ำ (ต่อจาก note R8)

## 🔍 ผลวิเคราะห์ (กฎข้อ 1 — verify ทุกจุด)

| ไฟล์ | class | เนื้อหา | สถานะ |
|------|-------|---------|-------|
| `VR/VRStartup.cs` (227L) | `VRStartup` | **ตัวจริง**: Quest optimization ครบเครื่อง — auto frame rate (72/90Hz), CPU/GPU levels (OVRManager), FFR (foveated rendering), OpenXR Meta Quest features (reflection), `DefaultExecutionOrder(-1000)`, persistAcrossScenes | ✅ **เก็บ** — สมบูรณ์กว่า, ออกแบบมาเป็น production |
| `Scripts/CueStrikeVRStartup.cs` (161L) | `CueStrikeVRStartup` | **ตัวเก่า/ซ้ำ**: XR init + scene loading — แต่ scene names พัง (`"Main"`/`"Boot"` — ไม่มีฉากนี้) ถ้าใช้จริงจะ `LoadSceneAsync("Main")` error ทันที; ไม่มี FFR/CPU-GPU/OpenXR config | 🗑️ **ลบ** |

**หลักฐานว่าทั้งคู่ไม่ถูกใช้ (ลบได้ปลอดภัย):**
- GUID ทั้งคู่ = **0 ref** ในทุก scene (`.unity`)
- code refs นอกตัวเอง = **0** (มีแต่ class declaration + Debug.Log ภายใน)
- เมนู `"NARI CUE STRIKE"` ที่ VRStartup.cs อ้างไว้ **ไม่เคยถูกสร้าง** (grep เจอแค่ comment)
- ไม่มี editor script ไหน `AddComponent` class ทั้งสอง
- เอกสารอ้างถึงแค่ note R8 ของเราเอง

## 📋 ขั้นตอน

- [x] **ขั้น 1 — เขียน plan นี้** + update todos
- [x] **ขั้น 2 — ลบ** `Scripts/CueStrikeVRStartup.cs` + `.meta`
- [x] **ขั้น 3 — Compile verify** → **0 errors** (`compile_check.sh` exit 0) + GUID 0 ref ค้าง
- [x] **ขั้น 4 — อัปเดตเอกสาร (กฎข้อ 2)**: `CUESTRIKE_MASTER.md` (§5 + status) + `TASK_PROGRESS.md` (Round 10 + ล้าง note R8 เดิม) + plan checkboxes
- [x] **ขั้น 5 — Commit + push origin**

## ⚠️ หมายเหตุ / งานถัดไป

- `VR/VRStartup.cs` ตัวที่เก็บไว้**ยังไม่ได้ถูกใส่ในฉากใด** — ตอนทำ Boot scene จริงต้องสร้าง editor tool ผูก component นี้ (ตาม design ที่ comment ไว้) + Vision audit (กฎข้อ 4)

## ✅ Definition of Done (กฎข้อ 4)

- [x] เหลือ VR startup ตัวเดียว (`VR/VRStartup.cs`)
- [x] Compile **0 errors**
- [x] เอกสารอัปเดตครบในรอบเดียวกัน

---
*Plan นี้เขียนก่อนลงมือตามกฎข้อ 5 — 2026-08-09*
