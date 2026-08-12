# Implementation Plan — R36: Snooker AI — AI opponent เข้ากับ WBPS ruleset (Snooker_Demo)

**วันที่:** 2026-08-12
**Branch:** `feat/r36-snooker-ai` → `main` (base `d498a0d` = R35 merged)
**กฎ:** กฎข้อ 1 (ตรวจของจริง) + กฎข้อ 5 (plan ก่อน, docs + compile ในรอบเดียวกัน)

## เป้าหมาย
ต่อ AI opponent เข้ากับ WBPS ruleset ใน Snooker_Demo — AI เล่นสนุกเกอร์ได้จริง
(เลือก ball ตามกฎ WBPS: red→color→red→...→color phase, ยิงจริง, ฟาวล์/สกอร์ ถูกต้อง)

## สถานะจริงที่ตรวจพบ (กฎข้อ 1)

| รายการ | สถานะ | หลักฐาน |
|--------|--------|---------|
| `CueStrikeWBPSRuleset` instance ใน Snooker_Demo | ✅ | `Snooker_Demo.unity:769` — Awake → ResetFrame → SetupRack |
| ลูก 22 ตัว (Red 1-15 + สี 16-21 + Cue 0) | ✅ มี | BallIdentity ×23 ใน scene |
| **ลูกมี Rigidbody** | ❌ **ไม่มีเลย** (grep=0) | physics ไม่ทำงาน — ยิงไม่ได้ |
| **ลูกมี Collider** | ❌ **ไม่มี** (SphereCollider=0) | WBPS.SpawnSnookerBall ใช้ CreatePrimitive (มี collider) แต่ลูกเดิมใน scene ไม่มี |
| **พื้นโต๊ะ/rail/cushion** | ❌ ไม่มี | ลูกจะตกทะลุ — ไม่มีที่ให้กลิ้ง |
| **Pocket positions** | ❌ ไม่มี | AI ไม่มีเป้าหมายหลุม + ตรวจผลไม่ได้ |
| WBPS events | ✅ | OnBallPotted / OnFoulCommitted / OnFrameWon |
| WBPS turn system | ❌ ไม่มี | ต้องสร้างใน bridge |
| `CueStrikeShotManager.ExecuteShot` | ✅ มี API | แต่ Snooker_Demo ไม่มี instance → bridge ใช้ AddForce ตรง |
| `CueStrikeAIController` (difficulty) | ✅ มี | SkillLevel Easy/Medium/Hard/Expert — ใช้เป็น base difficulty |

**สรุปปัญหา:** Snooker_Demo เป็นแค่ "ลูก + ruleset" ไม่มีฟิสิกส์/โต๊ะ/หลุม → ต้องสร้าง environment + bridge ให้ครบ

## งานจริง (ตามลำดับ)

### 1. แก้ `CueStrikeWBPSRuleset.cs`
- `SpawnSnookerBall()`: เพิ่ม **Rigidbody** (mass, drag, constraints — ห้ามหมุน/ตก) + **SphereCollider** ให้ลูกที่ spawn ใหม่ — ลูกที่สร้างจาก CreatePrimitive มี collider อยู่แล้ว → เน้นเพิ่ม Rigidbody + ensure collider
- เพิ่ม public accessor สำหรับ state ที่ AI ต้องอ่าน:
  - `public int ColorSequenceIndex => _colorSequenceIndex;`
  - `public bool IsColorPhase => isColorPhase;`
  - `public bool AwaitingRespotColor => awaitingRespotColor;`
  - `public int RedsRemaining => redsRemaining;`
- เพิ่ม `public void ValidateShotFull(...)` ใช้ได้อยู่แล้ว

### 2. เขียน `CueStrikeSnookerAIBridge.cs` (ใหม่, runtime — `Assets/CueStrike/Scripts/AI/`)
- **Turn system:** P1 (human) ↔ P2 (AI) สลับ — `StartTurn(int player)` / สังเกต `WBPS.OnBallPotted`/`OnFoulCommitted` → สลับเทิร์น
- **AI ตัดสินใจ (ตามกฎ WBPS):**
  - อ่าน state: `RedsRemaining`, `AwaitingRespotColor`, `IsColorPhase`, `ColorSequenceIndex`
  - เลือก target ball:
    - Red phase: เลือกลูกแดงที่เหลือ (1-15) ที่มีเส้นทางไปหลุม (raycast ง่ายๆ หรือ nearest-to-pocket heuristic)
    - หลังแดง (awaiting color): เลือกสี (16-21) ค่ามากสุดที่เหลือ (Black=7 ก่อน)
    - Color phase: ต้องยิงสีตาม `ColorSequenceIndex` (Yellow→Green→...→Black)
  - เลือก pocket: หลุมที่ target ball เข้าถึงได้ (nearest pocket — ไม่มี occlusion check เต็ม ใช้ distance heuristic)
  - คำนวณ aim: ghost-ball method — aim point = target.position + dir(target→pocket) * (radius*2)
  - **AddForce** จริง (pattern CueStrikeCue.cs:220 / ShotManager.ExecuteShot) + error ตาม difficulty
- **ประเมินผล:** รอลูกหยุด → ตรวจผ่าน pocket positions (อยู่ใกล้หลุม + ต่ำกว่าโต๊ะ) → เรียก `WBPS.RegisterPot(ballId)` / `ValidateShotFull(...)` → สลับเทิร์น
- **Difficulty:** Easy/Medium/Hard/Expert — ใช้ error margin จาก difficulty (เริ่ม Medium) + public `SetDifficulty(SkillLevel)`

### 3. เขียน `SnookerAISetup.cs` (ใหม่, Editor — `Assets/CueStrike/Editor/`)
- `[MenuItem("Tools/CueStrike/Snooker/100. Setup Snooker AI (Snooker_Demo)")]`
- เปิด Snooker_Demo →:
  1. **สร้างโต๊ะ:** Plane/BoxCollider ใต้ลูก (พื้น y≈0.42) + 4 rail/cushion BoxCollider (กันลูกตก) — ขนาดจากลูกตำแหน่ง (x ±1.3, z ±1.6)
  2. **สร้าง 6 pockets:** Empty GameObject + SphereCollider (trigger) + ตำแหน่ง (มุม 4 + กลาง 2)
  3. **เพิ่ม Rigidbody + SphereCollider** ให้ลูกทุกตัวที่ยังไม่มี
  4. **เพิ่ม `CueStrikeSnookerAIBridge`** + assign pocket positions + WBPS ref
- Idempotent + self-test + batchmode

### 4. Compile verify + รัน tool + self-test
- Batchmode 0 errors (Library อุ่นบน main)
- รัน tool → verify scene YAML (bridge + pockets + rigidbodies)
- Self-test: bridge มี + WBPS ref + pockets ≥ 6 + ลูกมี Rigidbody

### 5. Docs: CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md (R36 section)

### 6. Commit + push + เปิด PR → `main`

## ผลลัพธ์ที่คาดหวัง
- AI เล่นสนุกเกอร์ได้: เลือกลูกถูกตามกฎ (red→color→color phase), ยิงจริงด้วย physics, ฟาวล์/สกอร์ผ่าน WBPS
- เลือก difficulty ได้ (เริ่ม Medium ผ่าน bridge field)
- Snooker_Demo มีโต๊ะ/หลุม/ฟิสิกส์ครบ — คนเล่นเองก็เล่นได้

## ความเสี่ยง / หมายเหตุ
- งานนี้แตะ `CueStrikeWBPSRuleset.cs` + `Snooker_Demo.unity` — ไม่ชน PR เปิดอื่น
- AI aim เป็น heuristic (nearest pocket) — ไม่ perfect แต่เล่นได้สมเหตุสมผล (ทำ error ตาม difficulty)
- ไม่สร้าง UI เลือก difficulty ใหม่ — ใช้ field บน bridge (ต่อยอด R26 mode selector ทีหลัง)
- ไม่แตะ Chinese Pool / AAA_RoomDAY
