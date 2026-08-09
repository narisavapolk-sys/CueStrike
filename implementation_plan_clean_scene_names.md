# 🧼 Implementation Plan — Clean Stale Scene Name Defaults

> **Project:** CueStrike VR Billiards | **Date:** 2026-08-09
> **อนุมัติโดย:** พี่โม่ง (Project Owner) | **ผู้ทำ:** Buffy (Freebuff Dev Agent)
> **อ้างอิง:** `AI_TOOLS_MANDATE.md` (กฎข้อ 2, 4, 5) | **สถานะ:** 📝 Plan — รอลงมือ

---

## 🎯 เป้าหมาย

`TitleSceneManager.cs` มีค่า default ชื่อ scene เก่าที่ไม่มีอยู่จริง (จากยุคชื่อเก่า) — ถ้า class นี้ถูกใส่ใน scene แล้วกด Play จะพังทันที ล้างให้ชี้ไปฉากจริง / ว่างเปล่าอย่างปลอดภัย

## 🔍 ข้อเท็จจริง (กฎข้อ 1)

| รายการ | ผล |
|--------|-----|
| `TitleSceneManager` อยู่ใน scene ไหน? | ❌ ยังไม่มี (grep scenes = 0) — การแก้ตอนนี้เป็น preventive |
| refs นอก class | ไม่มี — field ถูกใช้แค่ใน class (btnPlay → `LoadScene(mainSceneName)`) |
| ฉากจริงที่มี (11, ใน build settings แล้ว) | `Title_NoksGrandHall`, `MainMenu`, `Snooker_Demo`, 8 ห้อง |
| `mainSceneName` ใช้จริง | ✅ btnPlay → `LoadScene(mainSceneName)` — ค่า `"MainScene"` พัง |
| `practice/multiplayer/settings/credits` | ยังไม่ถูกใช้โดยปุ่ม (Practice/Multiplayer = ComingSoon, Settings/Credits = panel) |
| พบเพิ่ม | `CueStrikeVRStartup.cs` มี default `"Main"`/`"Boot"` เก่า — **แต่ไม่ถูกใส่ในฉากไหน (dead)** → แค่บันทึก ไม่แก้ |

## 📋 ขั้นตอน

- [x] **ขั้น 1 — เขียน plan นี้** + update todos
- [x] **ขั้น 2 — แก้ `TitleSceneManager.cs`** (tooltips เป็น English ตามกฎ "Code English 100%"):
  - `mainSceneName`: `"MainScene"` → **`"MainMenu"`** (Title → MainMenu)
  - `practiceSceneName`: `"PracticeHub"` → **`"Snooker_Demo"`** (ตรงกับ MainMenuUIController)
  - `multiplayerSceneName`: `"MultiplayerLobby"` → **`""`** (ยังไม่มีฉาก — `LoadScene` guard ค่าว่างแล้ว ปลอดภัย)
  - `settingsSceneName` / `creditsSceneName`: → **`""`** (เป็น panel ไม่ใช่ฉาก)
- [x] **ขั้น 3 — Compile verify** → **0 errors** (`compile_check.sh` exit 0)
- [x] **ขั้น 4 — อัปเดตเอกสาร (กฎข้อ 2)**: `CUESTRIKE_MASTER.md` (§5) + `TASK_PROGRESS.md` (section) + plan checkboxes
- [x] **ขั้น 5 — Commit**

## ✅ Definition of Done (กฎข้อ 4)

- [x] ไม่มีค่า default เก่าที่ชี้ฉากไม่มีจริงใน `TitleSceneManager`
- [x] Compile **0 errors**
- [x] เอกสารอัปเดตครบในรอบเดียวกัน

---
*Plan นี้เขียนก่อนลงมือตามกฎข้อ 5 — 2026-08-09*
