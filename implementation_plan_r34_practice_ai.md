# Implementation Plan — R34: ลุงโน๊กคู่ซ้อม AI (Practice AI Opponent)

**Round:** R34 (2026-08-12) — *หมายเหตุ: พี่โม่งสั่งว่า "R31" แต่ R31 ถูกใช้ไปแล้ว (Referee Event Bridge, PR #28 merged) → งานนี้คือ R34 ตาม roadmap*
**กฎ:** กฎข้อ 1 (ตรวจของจริงก่อน) + กฎข้อ 5 (plan → implement → docs → verify ในรอบเดียว)

---

## 1. สถานะจริง (ตรวจโค้ดแล้ว — grounded)

| ระบบ | สถานะ | รายละเอียด |
|------|--------|-----------|
| `ChinesePoolAIModifier` (stub) | ✅ มีอยู่ (ใน AAA 2 ตัว) | `DecideCallShot()` → (ballId, pocketId), `DecideShotParameters()` → (aimPoint, power, spin), `SetDifficulty(Easy/Medium/Hard/Expert)` — **แต่ไม่มีใครเรียก (dead)** |
| `CueStrikeAIController` | ✅ มีอยู่ (ใน AAA 2 ตัว) | `SetSkillLevel()` + `BeginTurn()` + 4 ระดับ — แต่ยิงผ่าน reflection `shotManager.currentForce` → **ฉากไม่มี CueStrikeShotManager → ยิงไม่ทำงาน** |
| `ChinesePoolGameManager` | ✅ มีอยู่ | `NextPlayer()` ตั้ง `isAiTurn = (currentPlayerIndex == 1 && aiModifier != null)` — **แต่ไม่มีโค้ดที่เมื่อ isAiTurn แล้วให้ AI ตัดสินใจ/ยิง** |
| `ChinesePoolMatchSetupUI` (R25) | ✅ มีอยู่ | Panel เลือกเงื่อนไข (Single/Best of 3/5/7/Practice) — **ไม่มีปุ่มเลือก AI difficulty** |
| การยิงลูก | — | Pattern จริง: `Rigidbody.AddForce(direction * force, ForceMode.Impulse)` (CueStrikeCue.cs:220) |

**สรุปช่องว่าง:** AI มีครบแต่ disconnected — ต้อง (1) bridge ผูก isAiTurn → ตัดสินใจ → ยิงจริง (2) UI เลือกระดับความยาก (3) ผูก bridge เข้าฉากที่เล่นได้

## 2. สิ่งที่จะทำ

### 2.1 `CueStrikePracticeAIBridge.cs` (ใหม่, runtime, `Assets/CueStrike/Scripts/AI/`)
- subscribe `ChinesePoolGameManager.OnTurnChanged` → เมื่อ `isAiTurn`:
  1. เรียก `ChinesePoolAIModifier.DecideCallShot()` → `gm.SetCallShot(ballId, pocketId)` (ถ้าโหมด call-shot)
  2. เรียก `DecideShotParameters()` → ยิงจริง: หา cue ball Rigidbody → `AddForce(aimDirection * power, ForceMode.Impulse)` (pattern CueStrikeCue)
  3. รอลูกหยุด → ประเมินผล (ลูกไหนหลุม/ฟาวล์) → `gm.ProcessShotResult(ShotResult)` — fail-safe: ประเมินไม่ได้ → สลับเทิร์น (`gm.NextPlayer()`)
- `SetAIDifficulty(SkillLevel)` — map ไป `ChinesePoolAIModifier.SetDifficulty()` + `CueStrikeAIController.SetSkillLevel()` (ใช้ทั้ง 2 ระบบ)
- Fail-safe: หา modifier/controller/ballSetup ไม่เจอ → log + retry ทุก 2s, ไม่พัง

### 2.2 `ChinesePoolMatchSetupUI.cs` (แก้)
- เพิ่มแถว "ระดับ AI" (Easy/Medium/Hard/Expert) ใต้ปุ่มเงื่อนไข — สร้างปุ่มใน code เหมือนปุ่มเดิม
- เลือกแล้ว → เก็บ `PlayerPrefs` + เรียก bridge `SetAIDifficulty()` ก่อน `StartNewMatch()`

### 2.3 Editor tool `PracticeAISetup.cs` (ใหม่, `Assets/CueStrike/Editor/`)
- `Tools/CueStrike/AI/90. Setup Practice AI` — เพิ่ม `CueStrikePracticeAIBridge` ลง AAA_RoomDAY + Snooker_Demo + assign refs + idempotent + self-test + batchmode

## 3. Verify
- Compile gate batchmode: **0 errors** (ไฟล์ใหม่ 0 warnings)
- Tool รันจริง 2/2 ฉาก + idempotent (รันซ้ำ skip)
- Self-test: bridge + modifier + controller + difficulty mapping ครบ
- main checkout คืนสภาพสะอาด

## 4. Docs
- TASK_PROGRESS.md (section R34), CUESTRIKE_MASTER.md (status line), task.md (section R34)

## 5. PR
- feature branch `feat/r34-practice-ai` → PR → รอ CI เขียว → merge
