# R27 — Character Animation Clips (UncleNok + BoPanda) via Blender Pipeline

**Status:** In Progress
**Branch:** `feat/r27-character-animation`
**PR:** (to open)
**Date:** 2026-08-11

## 🎯 เป้าหมาย (ตามคำสั่งพี่ + โค้ช)

สร้าง animation clips 4 ตัว (Idle / Celebrate / Disappointed / Speak) สำหรับลุงโน๊กและโบ
ผ่าน Blender pipeline เดิม แล้ว assign ลง `UncleNok.controller` ให้เกม "มีชีวิต" (P9 ของโค้ช)

## 🔍 Findings จากการตรวจของจริง (กฎข้อ 1)

| รายการ | สถานะจริง |
|--------|-----------|
| Rig | **Rigify 706 bones** — bone names ของ `UncleNok_AAA.fbx` และ `Somchay_AAA.fbx` **เหมือนกัน 100%** (diff แค่ชื่อ armature) → clip หนึ่งชุดใช้กับทุกตัวละครได้ |
| Prefab | `UncleNok_Prefab` + `BoPanda_Prefab` เป็น variant ของ `Somchay_AAA.fbx` + มี Animator แต่ **`m_Controller: {fileID: 0}` — ยังไม่ผูก controller!** |
| Controller | `UncleNok.controller` มี 5 parameters (Speak/Celebrate/Disappointed/Neutral triggers + IsIdle bool) แต่ **มี state เดียว (Idle) โดยไม่มี clip** (`m_Motion: {fileID: 0}`) และไม่มี transitions |
| Trigger mismatch | `UncleNokReferee` ใช้ `_announceTrigger="Announce"`, `_disapproveTrigger="Disapprove"`, `_thinkingTrigger="Thinking"` — **ไม่มีใน controller** (controller มี "Disappointed"/"Neutral"/"Speak") → ต้อง sync ให้ตรง |
| Blender | **Blender 3.6.21 พร้อมใช้** — เคยรัน headless สำเร็จ (มี log เดิม) |
| FBX animation | ตอนนี้ทุก FBX เป็น **static (ไม่มี animation)** — ต้องสร้าง actions ใน Blender + export ใหม่ |

## 📋 แผนงาน

### ขั้นที่ 1 — Blender script: `BlenderScripts/create_character_animations_aaa.py`
- Import `UncleNok_AAA.fbx` (rig 706 bones เหมือนทุกตัว) 
- สร้าง 4 Actions บน FK control bones (rigify FK chain):
  - **Idle** (~3s loop): หายใจ (chest/spine โยกเล็ก), แขน/ศีรษะ sway นิดๆ
  - **Celebrate** (~2s): ยกแขนทั้งสองขึ้น + ตัวกระดก + ศีรษะเงย (ชัยชนะ)
  - **Disappointed** (~2s): ศีรษะก้ม + ไหล่ตก + แขนห้อย
  - **Speak** (~2s): ขยับหัว/jaw (ปากพูด) + แขน gesture เล็กน้อย
- Export 4 FBX (มี animation) ไป `BlenderScripts/Exports/Animations/`:
  - `UncleNok_Idle.fbx`, `UncleNok_Celebrate.fbx`, `UncleNok_Disappointed.fbx`, `UncleNok_Speak.fbx`
- **สำคัญ:** bone names ต้องคงเดิม (ใช้ FK chain เดียวกัน) → clip ใช้กับ rig ของ Somchay variant ได้

### ขั้นที่ 2 — Unity Editor tool: `Assets/CueStrike/Editor/CharacterAnimationSetup.cs`
- Import animation FBX จาก `BlenderScripts/Exports/Animations/` → extract clips
- อัปเดต `UncleNok.controller`:
  - เพิ่ม states: Idle (default, loop) + Celebrate/Disappointed/Speak (จาก clips)
  - เพิ่ม transitions: AnyState → Celebrate/Disappointed/Speak (trigger) → กลับ Idle
  - Sync parameters ให้ตรงกับ `UncleNokReferee`: เพิ่ม `Announce`/`Thinking` triggers หรือ map
- Assign controller ให้ `UncleNok_Prefab` + `BoPanda_Prefab` (Animator.m_Controller)
- Self-test + batchmode capability (ตาม convention R24/R25/R26)

### ขั้นที่ 3 — Verify
- Compile gate batchmode: 0 errors
- Scene load + Editor tool idempotent + self-test
- Blender: 4 clips สร้างจริง (มี keyframes บน FK bones)

### ขั้นที่ 4 — Docs + PR
- อัปเดต `CUESTRIKE_MASTER.md` (P9 → มี animation), `TASK_PROGRESS.md` (R27), `task.md`
- Commit + push + เปิด PR

## ⏱️ ประมาณการ
- Blender script + รัน: ~30 นาที
- Unity tool + verify: ~30 นาที
- Docs + PR: ~15 นาที
