# Implementation Plan — R35: Bo Comedy Director ทำงานเต็มรูปแบบในห้องแข่ง (AAA_RoomDAY)

**วันที่:** 2026-08-12
**Branch:** `feat/r35-bo-scoreboard` → `main` (base `7e19b21` = R34 merged)
**กฎ:** กฎข้อ 1 (ตรวจของจริง) + กฎข้อ 5 (plan ก่อน, docs + compile ในรอบเดียวกัน)

## เป้าหมาย
Bo Comedy Director โมเมนต์ "มึนสกอร์เสมอ" (R32) ทำงานจริงในห้องแข่ง AAA_RoomDAY
— ต้องการ `ChinesePoolScoreboard` component ในฉาก + `ChinesePoolUIManager._scoreboard` assigned.

## สถานะจริงที่ตรวจพบ (กฎข้อ 1)

| รายการ | สถานะ | หลักฐาน |
|--------|--------|---------|
| `BoComedyDirector` ใน BoPanda prefab | ✅ (R32 merged) | `BoPanda_Prefab.prefab:433` |
| BoPanda instance ใน AAA_RoomDAY | ✅ (R33 merged) | `AAA_RoomDAY.unity:1153` |
| `BoComedyDirector.TrySubscribeScoreboard()` | ✅ logic ครบ (retry ทุก 2s) | `BoComedyDirector.cs:97-110` |
| `ChinesePoolScoreboard` component ใน AAA | ❌ **ไม่มีเลย** | grep = 0 ใน scene |
| "Digital Scoreboard" ใน AAA | ⚠️ เป็นแค่ mesh ตกแต่ง | MeshRenderer+MeshFilter+BoxCollider, ไม่มี MonoBehaviour |
| `ChinesePoolUIManager` ใน AAA | ✅ มี (`fileID 1104105757`) | `AAA_RoomDAY.unity:27079` |
| `ChinesePoolUIManager._scoreboard` | ❌ **ว่าง (`fileID: 0`)** | `AAA_RoomDAY.unity:27089` |
| `ChinesePoolScoreboard.OnScoreChanged` | ✅ event มี (line 17) | `ChinesePoolScoreboard.cs` |
| ใครเพิ่มสกอร์จริง | `BallPottedTracker` + `UIManager` (RegisterPottedBall/OnBallPotted) | `BallPottedTracker.cs:43,134-136` |

**ปัญหา:** AAA ไม่มี `ChinesePoolScoreboard` component → UIManager._scoreboard ว่าง →
Bo `FindAnyObjectByType<ChinesePoolScoreboard>()` หาไม่เจอ → retry ตลอด → "มึนสกอร์" ไม่เกิด.

## งานจริง (ตามลำดับ)

1. **เขียน Editor tool `BoScoreboardSetup.cs`** (`Assets/CueStrike/Editor/`):
   - `[MenuItem("Tools/CueStrike/Mascots/95. Setup Bo Comedy Scoreboard (AAA_RoomDAY)")]`
   - เปิด `AAA_RoomDAY.unity` (แบบ batchmode-safe)
   - ถ้ายังไม่มี `ChinesePoolScoreboard` → สร้าง GameObject `CueStrike_ChinesePoolScoreboard`
     + component + UI structure พื้นฐาน (Text scores P1/P2 + turn indicators + ball containers)
     — ลอก pattern จาก `CueStrikeGamePolishSetup.CreateScoreboard` (มีอยู่แล้วใน repo)
   - หา `ChinesePoolUIManager` ในฉาก → assign `_scoreboard` (ผ่าน SerializedObject — ไม่ใช้ reflection กับ private field)
   - Idempotent: รันซ้ำไม่สร้างซ้ำ, skip ถ้ามีครบ
   - Self-test: ตรวจ scoreboard มี + UIManager._scoreboard assigned + BoPanda มี BoComedyDirector
   - Batchmode: `-executeMethod BoScoreboardSetup.Run` → บันทึก scene

2. **Compile verify:** batchmode 0 errors (บน main checkout — Library อุ่น)
3. **รัน tool จริง** → วาง scoreboard ลง AAA + assign refs + บันทึก scene
4. **Verify:** scene YAML มี ChinesePoolScoreboard + UIManager._scoreboard ≠ 0
5. **Docs:** CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md (R35 section)
6. **Commit + push + เปิด PR** ต่อ `main`

## ผลลัพธ์ที่คาดหวัง
- Bo ใน AAA_RoomDAY จะ subscribe `ChinesePoolScoreboard.OnScoreChanged` ได้จริง
- เมื่อสกอร์ P1 == P2 > 0 (จาก BallPottedTracker/GameManager) → Bo `SetTrigger("Speak")` = มึน "ใครชนะนะ??"
- ระบบ scoreboard ใช้ได้จริง (แสดงสกอร์/เทิร์น/ฟาวล์) — ประโยชน์ต่อ R25 flow

## ความเสี่ยง / หมายเหตุ
- งานนี้แตะ `AAA_RoomDAY.unity` (scene) — ไม่ชน PR เปิดอื่น (ไม่มี PR เปิดตอนนี้)
- ไม่แตะ BoPanda prefab / BoComedyDirector.cs — logic มีอยู่แล้ว
- UI structure สร้างใน code — ใช้ LegacyRuntime.ttf เหมือน tool อื่นใน repo
- ไม่เปลี่ยน behavior ของเกม — แค่เพิ่ม component ที่ขาด + ผูก ref
