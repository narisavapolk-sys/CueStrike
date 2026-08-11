# Vision Audit Checklist — Manual PlayMode Verification (กฎข้อ 4)

**When:** After merge of R13/R17 (Boot + CallShot scene refs) and R14 (show trigger)
into main. Open Unity Editor → Play Mode from `Boot.unity`.

**Why:** Static audit ครอบคลุม code + scene + wiring — แต่ runtime visual check ต้องดู
ด้วยตา: เปิด scene → กด Play → verify behavior ตามรายการ

---

## R13: Boot scene + VRStartup transition

### A. Boot → Title transition (1 min)
1. Open Project → `Assets/CueStrike/Scenes/Boot.unity` (should be Scene 0).
2. Press Play ▶.
3. **Expect:** Loading screen appears briefly → Title scene loads.
4. **Console expect:** `[VRStartup] Quest optimizations applied: <Hz> (<device>)`
5. **Verify Hz:**
   - **Editor (PC):** `90Hz (Unknown)`
   - **Quest 2 build:** `72Hz (Meta Quest 2)` — **CRITICAL** ต้องไม่เป็น 90Hz
   - **Quest 3 build:** `90Hz (Meta Quest 3)` (or `120Hz` if `enable120HzOnQuest3` ticked)

### B. VR optimizations active
6. **VSync:** check Stats window — "Batches" rendered at vsync rate (~72/90 fps in Quest).
7. **CPU/GPU levels:** check Oculus app on device — performance overlay shows CPU Lv2 / GPU Lv2.
8. **FFR (Fixed Foveated Rendering):** check via `adb shell setprop debug.oculus.foveation.level 3` on device.

### C. Scene/components rendered
9. Title scene shows main menu (Play button, settings, etc.).
10. Avatar/room decorations render correctly (no missing meshes — pink/magenta check).

---

## R14: Call-shot UI show trigger

### A. Trigger on turn change
1. Select CueStrike ChinesePool mode → start match.
2. Break shot → AI doesn't call (it's their turn → no UI).
3. **Human player's first turn** → expect: Call-shot panel appears at center of screen.
4. **Console expect:** `[ChinesePoolGameManager] MaybeShowCallShotUI invoked`
5. UI elements present: pocket-select, ball-select grid, confirm/cancel buttons.

### B. Confirm → GameManager.SetCallShot
6. Pick (ball × pocket) → Confirm.
7. **Console expect:** `OnShotCalled` fired, `ChinesePoolGameManager.SetCallShot` called with (ball, pocket) → score table records expected outcome.
8. After shot completes (legal pot) → shot counted correct.

### C. Cancel → ClearCallShot
9. New human turn → Call-shot panel shows.
10. Press Cancel → panel hides.
11. **Console expect:** `OnCallShotCancelled` fired, `ChinesePoolGameManager.ClearCallShot` called.
12. Player shoots any ball (no penalty, just un-called shot).

### D. AI turn: UI NOT shown
13. Switch to AI's turn → **panel should NOT appear** (guard `!isAiTurn`).
14. **Console expect:** `MaybeShowCallShotUI` invoked but early-return on `isAiTurn=true`.

---

## R17: CallShot UI scene refs (panel actually appears)

### A. Panel actually visible (the bug fix)
1. With headset on (or scene-mode), trigger R14 → confirm panel **visually appears** (not just runtime-no-op).
2. **Expect:** panel takes up mid-screen area, semi-transparent background, ball grid + pocket grid visible.
3. Click each sub-element: confirm buttons highlight on hover.

### B. Both scenes
4. Repeat above for **Title_NoksGrandHall.unity** (load via main menu).
5. Repeat for **AAA_RoomDAY.unity** (via Play ChinesePool).

### C. Component count
6. Select `CallShot_Panel` GameObject in scene hierarchy.
7. **Expect:** exactly 1 ChinesePoolCallShotUI component attached (not 2).
8. Inspector: 9/9 fields populated (no `None (Missing)`).

---

## Negative tests

### What should NOT happen
- ❌ Play scene ติด Loading screen forever (Boot transition broken)
- ❌ Player turn but CallShot UI ไม่ปรากฏ (R14 trigger fail)
- ❌ UI panel ปรากฏตอน AI เทิร์น (R14 guard fail)
- ❌ Field อะไรใน `CallShot_Panel` = `None (Missing)` (R17 incomplete)
- ❌ VR fps 90Hz บน Quest 2 (R16 auto-detect fail)
- ❌ CI fail ที่ Node 20 deprecation (R21 still pending)

---

## How to report findings

When you Play test, please report:
- For each **Step** above: ✅ pass / ❌ fail + screenshot or console excerpt
- If fail, give exact error message / unexpected behavior
- Note any visual artifacts (z-fighting, missing textures, foveation edges visible)

Returns drives next round: if all ✅, merge PRs to main and enable Branch Protection.
If any ❌, fix in next round.
