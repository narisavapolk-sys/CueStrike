# Implementation Plan — R42: Referee Mode Switcher (Bo กรรมการ คู่ลุง / แทนลุง)

**วันที่:** 2026-08-12
**Branch:** `feat/r42-referee-mode` → `main`
**กฎ:** กฎข้อ 1 (ตรวจของจริง) + กฎข้อ 5 (plan ก่อน, docs + compile ในรอบเดียวกัน)

## หมายเหตุชื่อ R
พี่สั่งว่า "R38" แต่ **R38/R41 ถูกใช้ไปแล้ว** → งานนี้คือ **R42** ตาม roadmap

## เป้าหมาย
"Bo เป็นกรรมการได้ด้วย (ไม่ใช่ผู้เล่น) — ผูก Bo clips กับ game events คู่กับลุงหรือแทนลุงได้"
→ เพิ่ม **Referee Mode** ให้เลือกได้:
- **ReplaceUncle (default — R40 ที่ทำแล้ว):** Bo กรรมการคนเดียว — ลุง bridge เงียบ (disabled)
- **DuoWithUncle:** Bo + ลุง กรรมการคู่ — ลุง bridge กลับมาประกาศด้วย (enable ตอน runtime)

## สถานะจริงที่ตรวจพบ (กฎข้อ 1)

| รายการ | สถานะ | หลักฐาน |
|--------|--------|---------|
| Bo เป็นกรรมการ (R40 merged) | ✅ BoReferee + BoRefereeEventBridge + 14 clips ใน BoPanda prefab | main `5fb82e1` |
| ลุง bridge | ❌ **disabled** (`m_Enabled: 0`) — "แทนลุง" ทำแล้ว | UncleNok_Prefab.prefab:465 |
| BoRefereeEventBridge | ✅ มี (subscribe GameManager + WBPS) | BoRefereeEventBridge.cs |
| UncleNokRefereeEventBridge | ✅ มี (subscribe เดียวกัน) | UncleNokRefereeEventBridge.cs |
| ตัวเลือกโหมด "คู่กับลุง" | ❌ **ไม่มี** — ต้องเพิ่ม RefereeMode | — |

**ปัญหา:** R40 ทำ "แทนลุง" อย่างเดียว — ผู้ใช้เลือก "คู่กับลุง" ไม่ได้ (ลุงเงียบถาวร)

## งานจริง

1. **`BoRefereeEventBridge.cs`** (แก้): เพิ่ม
   - `public enum RefereeMode { ReplaceUncle, DuoWithUncle }`
   - `public RefereeMode refereeMode = RefereeMode.ReplaceUncle;`
   - `ApplyRefereeMode()` — ใน Start/Update: DuoWithUncle → enable UncleNokRefereeEventBridge ใน scene (ถ้าเจอ) / ReplaceUncle → disable (idempotent, retry เหมือน pattern เดิม)

2. **`RefereeModeSetup.cs`** (ใหม่, Editor): tool `Tools/CueStrike/Mascots/145. Set Referee Mode (Bo only / Bo+Uncle)`
   - ตั้งค่า refereeMode บน BoPanda prefab (ผ่าน PrefabUtility.LoadPrefabContents)
   - batchmode entry: `RunFromBatch` + self-test (verify field + enum)

3. **Compile verify:** batchmode 0 errors (Library อุ่นบน main)
4. **รัน tool จริง** → prefab ได้ refereeMode field (default ReplaceUncle — ไม่เปลี่ยนพฤติกรรมเดิม)
5. **Verify:** prefab YAML มี refereeMode + self-test + idempotency
6. **Docs:** CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md (R42 section)
7. **Commit + push + เปิด PR** ต่อ `main` (base = main ปัจจุบัน — ต้อง merge PR #36 (R41) ก่อน แตะ docs เดียวกัน)

## ผลลัพธ์ที่คาดหวัง
- **โหมด ReplaceUncle (default):** Bo กรรมการคนเดียว (เหมือน R40 — ลุงเงียบ)
- **โหมด DuoWithUncle:** Bo + ลุงประกาศคู่กัน (ลุง bridge re-enable ตอน runtime — ผูก same events)
- เลือกได้จาก Inspector หรือ Editor tool — ต่อยอดเป็น UI ได้ภายหลัง

## ความเสี่ยง / หมายเหตุ
- งานนี้แตะ `BoRefereeEventBridge.cs` + `BoPanda_Prefab.prefab` + ไฟล์ Editor ใหม่
- **ต้อง merge PR #36 (R41) ก่อน** — แตะ docs เดียวกัน
- ไม่แตะโค้ดลุง (แค่ enable/disable bridge ตอน runtime)
- การ enable ลุง bridge ตอน runtime: bridge มี fail-safe retry อยู่แล้ว → เมื่อ enabled จะ subscribe เอง
