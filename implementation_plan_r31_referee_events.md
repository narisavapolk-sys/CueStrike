# Implementation Plan — R31: กรรมการจริง — ผูก UncleNokReferee กับ game events

> **กฎข้อ 5:** เขียน plan ก่อน implement — อัปเดตเอกสาร + verify compile ในรอบเดียวกัน
> **Branch:** `feat/r31-referee-events` (base: `main` @ `d5aa9cd` รวม R30+R32 แล้ว)
> **วันที่:** 2026-08-12

---

## 1. เป้าหมาย (ตามคำสั่งพี่โม่ง)

ผูก UncleNokReferee กับ game events (OnFrameStart/OnBallPotted/OnFoulCommitted) ในโหมดแข่ง — กรรมการประกาศคะแนน + ฟาวล์จริง

## 2. Findings จากการตรวจโค้ดจริง (กฎข้อ 1)

| # | พบ | สถานะ |
|---|-----|--------|
| 1 | `ChinesePoolGameManager.Instance` (static) มี events: `OnFrameWon(int)`, `OnFrameLost(int)`, `OnFoulCommitted(int,string)`, `OnMatchOver`, `OnTurnChanged(int)`, `OnScoreChanged(int,int)` | ✅ แหล่ง event |
| 2 | `CueStrikeWBPSRuleset.Instance` (static) มี events: `OnBallPotted(int)`, `OnFoulCommitted(int,string)`, `OnFrameWon` | ✅ แหล่ง event (Snooker) |
| 3 | `UncleNokReferee` มี methods: `OnFrameStart/OnFrameEnd/OnMatchStart/OnMatchEnd/OnPlayerTurnStart/OnPlayerTurnEnd/OnBallPotted/OnFoulCommitted(FoulType,int,int)/OnBreakShot/...` | ✅ ครบ |
| 4 | **ไม่มีใคร subscribe events → referee ตัวตั้ง** (ยังไม่ประกาศคะแนน/ฟาวล์) | ❌ งานนี้ |
| 5 | R30 ผูก AudioSource + refs แล้ว — พร้อมเล่นเสียง | ✅ base |

## 3. แผนงาน

### 3.1 เขียน `UncleNokRefereeEventBridge.cs` (runtime — ใหม่)
- `Start()`: หา referee (FindAnyObjectByType) + subscribe events:
  - **ChinesePoolGameManager**: `OnFrameWon` → `OnFrameEnd(winner)`, `OnFoulCommitted` → `OnFoulCommitted(mapFoulType)`, `OnMatchOver` → `OnMatchEnd(winner)`, `OnTurnChanged` → `OnPlayerTurnStart/End`, `OnFrameStarted` (ถ้ามี) → `OnFrameStart`
  - **CueStrikeWBPSRuleset**: `OnBallPotted` → `OnBallPotted(0, pts, 1)`, `OnFoulCommitted` → `OnFoulCommitted`, `OnFrameWon` → `OnFrameEnd`
- Fail-safe: หา manager/referee ไม่เจอ → log + retry (คล้าย BoComedy)
- un-subscribe ใน OnDestroy

### 3.2 map FoulType (string → enum)
- "CueBallPotted" → FoulType.CueBallPotted, "NoBallContacted" → NoBallContacted, "WrongBallFirst" → WrongBallFirst, "NoCushionAfterContact" → NoCushionAfterContact, "BallOffTable" → BallOffTable, อื่น → Generic

### 3.3 Editor tool `RefereeEventBridgeSetup.cs`
- MenuItem: `Tools/CueStrike/Mascots/80. Setup Referee Events`
- ผูก bridge เข้า UncleNok_Prefab (PrefabUtility.LoadPrefabContents) — ฉากไหนมีลุงโน๊กได้ผลอัตโนมัติ
- Idempotent + self-test + batchmode

## 4. Verify

- [ ] Compile gate batchmode: 0 errors
- [ ] Tool รันจริง + self-test ผ่าน (bridge ใน prefab + events มีใน manager)
- [ ] main checkout คืนสภาพสะอาด
- [ ] docs: CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md

## 5. ขอบเขตงาน (ไม่ทำ)

- ไม่แตะ AI opponent (R34 แยก)
- ไม่แตะ BoPanda / Bo Comedy (R32 merged แล้ว)
- ไม่เพิ่มเสียงใหม่ (ใช้ clips ที่มี — R30)
