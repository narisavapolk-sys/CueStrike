# Implementation Plan — R37: Fix ChinesePool AI — เพิ่ม ChinesePoolAIModifier ลง AAA_RoomDAY

**วันที่:** 2026-08-12
**Branch:** `feat/r37-cnpool-ai-fix` → `main` (base `be5cc83` = R36 Snooker AI merged)
**กฎ:** กฎข้อ 1 (ตรวจของจริง) + กฎข้อ 5 (plan ก่อน, docs + compile ในรอบเดียวกัน)

*หมายเหตุ: พี่โม่งสั่งว่า "R36" แต่ R36 ถูกใช้ไปแล้ว (Snooker AI — PR #31 merged) → งานนี้คือ R37 ตาม roadmap*

## เป้าหมาย
แก้ Vision audit blocker (R34): AI ในโหมด Practice (Chinese Pool) ยิงไม่ได้
เพราะ AAA_RoomDAY ไม่มี `ChinesePoolAIModifier` component + refs ว่าง

## สถานะจริงที่ตรวจพบ (กฎข้อ 1)

| รายการ | สถานะ | หลักฐาน |
|--------|--------|---------|
| `ChinesePoolAIModifier` class (DecideCallShot/DecideShotParameters/SetDifficulty) | ✅ มีครบ | `ChinesePoolAIModifier.cs:70,109,142` (namespace `CueStrike.Gameplay.ChinesePool`) |
| **`ChinesePoolAIModifier` component ใน AAA** | ❌ **ไม่มีเลย** (grep=0) | `AAA_RoomDAY.unity` |
| `ChinesePoolGameManager.aiModifier` | ❌ ว่าง (`fileID: 0`) | AAA block |
| `CueStrikePracticeAIBridge.aiModifier` | ❌ ว่าง (`fileID: 0`) | AAA block (aiController มีแล้ว) |
| `CueStrikePracticeAIBridge.aiController` | ✅ มี (`1408312383`) | AAA block |
| Guard ใน bridge `OnTurnChanged`: `if (_gm == null \|\| _gm.aiModifier == null) return;` | ⚠️ return ทันทีเมื่อ modifier ว่าง → AI ไม่ยิง | `CueStrikePracticeAIBridge.cs:115` |

**ปัญหา:** `ChinesePoolAIModifier` หายจากฉาก → `FindFirstObjectByType` ใน Awake/Start หาไม่เจอ
→ `GameManager.aiModifier` และ `bridge.aiModifier` ว่าง → guard return → AI ไม่ยิง

## งานจริง

1. **เขียน Editor tool `ChinesePoolAIModifierSetup.cs`** (`Assets/CueStrike/Editor/`):
   - `[MenuItem("Tools/CueStrike/AI/110. Setup ChinesePool AI Modifier (AAA_RoomDAY)")]`
   - เปิด `AAA_RoomDAY.unity` (batchmode-safe)
   - ถ้ายังไม่มี `ChinesePoolAIModifier` → สร้าง GameObject `ChinesePoolAIModifier` + component
   - assign refs (ผ่าน SerializedObject — ไม่ใช้ reflection กับ private):
     - `ChinesePoolGameManager.aiModifier` → modifier
     - `CueStrikePracticeAIBridge.aiModifier` → modifier
   - Idempotent: รันซ้ำไม่สร้างซ้ำ / skip ถ้ามีครบ
   - Self-test: modifier มี + GameManager.aiModifier ≠ 0 + bridge.aiModifier ≠ 0
   - Batchmode: `-executeMethod ChinesePoolAIModifierSetup.RunFromBatch`

2. **Compile verify:** batchmode 0 errors (Library อุ่นบน main)
3. **รัน tool จริง** → เพิ่ม modifier + assign refs + บันทึก scene
4. **Verify:** scene YAML มี ChinesePoolAIModifier + refs ≠ 0
5. **Docs:** CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md (R37 section)
6. **Commit + push + เปิด PR** ต่อ `main`

## ผลลัพธ์ที่คาดหวัง
- `GameManager.aiModifier` ≠ null → `isAiTurn` ทำงาน → `OnTurnChanged` เรียก AI turn
- `bridge.aiModifier` ≠ null → `DecideCallShot()` + `DecideShotParameters()` ทำงาน → AI ยิงจริง (AddForce)
- AI ระดับ Easy/Medium/Hard/Expert (จาก UI R34) ทำงานเต็มรูปแบบ

## ความเสี่ยง / หมายเหตุ
- งานนี้แตะ `AAA_RoomDAY.unity` เท่านั้น — ไม่ชน PR เปิดอื่น (ไม่มี PR เปิดตอนนี้)
- ไม่แตะโค้ด runtime (GameManager/bridge มี logic auto-find อยู่แล้ว — แค่ฉากขาด component)
- `CueStrikeAIController` (มีอยู่แล้วใน AAA) เป็นคนคุม difficulty — modifier เป็นคนตัดสินใจ shot
