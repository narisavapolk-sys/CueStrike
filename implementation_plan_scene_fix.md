# 🎬 Implementation Plan — Scene Loading Fix

> **Project:** CueStrike VR Billiards | **Date:** 2026-08-09
> **อนุมัติโดย:** พี่โม่ง (Project Owner) | **ผู้ทำ:** Buffy (Freebuff Dev Agent)
> **อ้างอิง:** `AI_TOOLS_MANDATE.md` (กฎข้อ 2, 4, 5) | **สถานะ:** 📝 Plan — รอลงมือ

---

## 🎯 เป้าหมาย

แก้บั๊ก scene loading ที่ตรวจพบจริง 2 จุด:

1. **`EditorBuildSettings.asset` มี scene แค่ 1 จาก 11** — มีแค่ `Title_NoksGrandHall.unity` → การโหลด `MainMenu` / ห้อง 8 ห้อง / demo ในบิลด์จริงจะล้มเหลว
2. **`MainMenuUIController.cs:125` โหลด scene `"hub"` ที่ไม่มีอยู่จริง** (ROADMAP เคยให้ "Remove or repurpose hub.unity") → กดปุ่ม **Practice** ใน Main Menu จะ error ทันที

## 🔍 ข้อเท็จจริงที่ verify แล้ว (กฎข้อ 1 — ห้ามเดา)

| รายการ | ผลตรวจ | หลักฐาน |
|--------|--------|---------|
| Scenes ในโปรเจกต์ | 11 scenes (Title, MainMenu, Snooker_Demo, 8 ห้อง) — ชื่อไม่ซ้ำกัน | `find Assets -name "*.unity"` |
| Build settings | มีแค่ `Title_NoksGrandHall` (guid `9b4ce1f2...`) | `ProjectSettings/EditorBuildSettings.asset` |
| Runtime loading | `CueStrikeRoomManager.GetSceneName()` โหลด 8 ห้องด้วยชื่อ (e.g. `AAA_RoomDAY`, `ZenDojo_Room`) — ต้องอยู่ใน build | `CueStrikeRoomManager.cs:25-46` |
| MainMenu practice | `CueStrikeLoadingScreen.LoadScene("hub")` — scene ไม่มีจริง | `MainMenuUIController.cs:125` |
| Title scene | `TitleSceneManager` **ไม่ได้อยู่ใน** Title scene (grep = 0) → ไม่ต้องแก้ mainSceneName | `Title_NoksGrandHall.unity` |
| `Snooker_Demo.unity` | มีลูกสนุกเกอร์ (Red_2..Red_15, Blue) + `CueStrikeWBPSRuleset` → เป็นฉากเล่นได้จริง เหมาะเป็นเป้า Practice | YAML scene |
| Scene GUIDs | ครบ 11 — ดูตารางด้านล่าง | `*.unity.meta` |

### Scene ทั้ง 11 + GUID
| Scene | GUID |
|-------|------|
| `Title_NoksGrandHall.unity` (อยู่ใน build แล้ว) | `9b4ce1f23fdc46241b4628271c83627d` |
| `MainMenu.unity` | `da0e097f4e05efa4f8b918be2954aa0a` |
| `Snooker_Demo.unity` | `4c8d2da74ada2604190ca821730e5c33` |
| `AAA DAY/AAA_RoomDAY.unity` | `7e71f528c8e120345ae8764536972e5a` |
| `Cyberpunk/Cyberpunk_Room.unity` | `a8216b7443f657344881924487ef08dd` |
| `GrandArena/GrandArena_Room.unity` | `69e59e0fae95db34a8969f6a2cea9c47` |
| `Industrial/Industrial_Room.unity` | `13a2fec92b1f16a498158506947485c2` |
| `Luxury/Luxury_Room.unity` | `ab1c98f2b831d47488bc2930f9996e91` |
| `SpaceNebula/SpaceNebula_Room.unity` | `3ad04cd6d2da3b540986417b7243327d` |
| `WarpFantasy/WarpFantasy_Room.unity` | `1a2b3c4d5e6f708192a3b4c5d6e7f801` |
| `ZenDojo/ZenDojo_Room.unity` | `cfc1a194e95edee4092d45f18869abc8` |

## 📋 ขั้นตอน

- [x] **ขั้น 1 — เขียน plan นี้** (ไฟล์นี้) + update todos
- [x] **ขั้น 2 — สร้าง `Assets/CueStrike/Editor/SceneBuildSettingsFixer.cs`** — Editor tool ตามกฎข้อ 4 (Batchmode Automation):
  - `[MenuItem("Tools/CueStrike/Fix/Add All Scenes to Build Settings")]`
  - สแกน `Assets/CueStrike/Scenes/**/*.unity` → ตั้ง `EditorBuildSettings.scenes` ครบ 11 (enabled)
  - Log รายการ scenes + 3-layer guard ตาม convention
- [x] **ขั้น 3 — แก้ `MainMenuUIController.cs:125`**: `LoadScene("hub")` → `LoadScene("Snooker_Demo")` + แก้ log message
- [x] **ขั้น 4 — รัน batchmode**: `Unity.exe -batchmode -quit -projectPath … -executeMethod CueStrike.Editor.SceneBuildSettingsFixer.FixBuildScenes` — ✅ 0 errors + "Added 11 scenes"
- [x] **ขั้น 5 — Verify**: `EditorBuildSettings.asset` มี **11 scenes** + `grep "error CS"` = 0 + exit code 0 ✓
- [x] **ขั้น 6 — อัปเดตเอกสาร (กฎข้อ 2)**: `CUESTRIKE_MASTER.md` (§5) + `TASK_PROGRESS.md` (section ใหม่) ✓
- [x] **ขั้น 7 — Commit**

## ⚠️ ความเสี่ยง & มาตรการ

| ความเสี่ยง | มาตรการ |
|-----------|---------|
| แก้ .asset ด้วยมือผิดรูปแบบ YAML | ✅ เลือกใช้ `EditorBuildSettings.scenes` API ผ่าน batchmode (Unity เขียนให้เอง) — ไม่แตะ YAML มือ |
| compile error จากสคริปต์ใหม่ | batchmode run ตัวเดียวตรวจทั้ง compile + execute; log ชัด (`error CS`) |
| เปลี่ยนเป้า practice ผิดฉาก | ใช้ `Snooker_Demo` (ฉากเดียวที่เล่นได้จริงนอกจากห้อง) — บันทึกใน docs เผื่อพี่โม่งอยากเปลี่ยน |
| สคริปต์ fixer กลายเป็นขยะ | เก็บเป็น Editor Tool มี MenuItem + guard ตาม convention (ข้อ "ทุกงานต้องมีปุ่ม Apply") |

## ✅ Definition of Done (กฎข้อ 4)

- [x] `EditorBuildSettings.asset` มี **11 scenes** ครบ
- [x] `MainMenuUIController` ไม่อ้าง scene ที่ไม่มีจริง
- [x] Compile **0 errors** (batchmode exit 0)
- [x] เอกสารอัปเดตครบในรอบเดียวกัน

---
*Plan นี้เขียนก่อนลงมือตามกฎข้อ 5 — 2026-08-09*
