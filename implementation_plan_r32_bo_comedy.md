# Implementation Plan — R32: Bo Comedy Director — โมเมนต์ตลกง่ายๆ 2 ตัว

> **กฎข้อ 5:** เขียน plan ก่อน implement — อัปเดตเอกสาร + verify compile ในรอบเดียวกัน
> **Branch:** `feat/r32-bo-comedy` (base: `main` @ `05cdc67` รวม R29 + R30 แล้ว)
> **วันที่:** 2026-08-12

---

## 1. เป้าหมาย (ตามคำสั่งพี่โม่ง)

ระบบ Bo Comedy Director — โมเมนต์ตลกง่ายๆ 2 ตัวก่อน ใช้ animation ที่มีอยู่แล้ว:
1. **Bo หลับ** — เมื่อผู้เล่นคิดนานเกิน 30 วินาที
2. **Bo มึนสกอร์เสมอ** — เมื่อสกอร์เสมอกัน

## 2. Findings จากการตรวจโค้ดจริง (กฎข้อ 1)

| # | พบ | สถานะ |
|---|-----|--------|
| 1 | BoPanda prefab มี **Animator + controller** (261a1aca) — triggers: `IsIdle` (bool), `Celebrate`, `Disappointed`, `Speak` (trigger) | ✅ ใช้ได้ |
| 2 | BoPanda มี `BoPandaBanter.cs` (reaction system) อยู่แล้ว | ✅ |
| 3 | `ChinesePoolScoreboard` มี **`OnScoreChanged` event** + `SetScore`/`AddScore` | ✅ แหล่ง score |
| 4 | Title scene มี BoPanda อยู่ (1.8, 0.4, -1.6) — ฉากทดสอบ | ✅ |
| 5 | R30 (PR #25) merged — `UncleNokReferee` มี AudioSource + refs ครบ | ✅ base |

## 3. แผนงาน

### 3.1 `BoComedyDirector.cs` (runtime — ใหม่)
- **ทริกเกอร์ 1: Bo หลับ (คิดนานเกิน 30s)** — ใช้ `BallActivityDetector` (หา Rigidbody ที่ขยับ) → ไม่ขยับเกิน 30s → `SetTrigger("Disappointed")` (ก้มหน้า = หลับ) + "zzz..."; มีการขยับ → `SetBool("IsIdle", true)` + ตื่น
- **ทริกเกอร์ 2: Bo มึนสกอร์เสมอ** — subscribe `ChinesePoolScoreboard.OnScoreChanged` → p1 == p2 > 0 → `SetTrigger("Speak")` + cooldown 20s
- Fail-safe: หา Animator/Scoreboard ไม่เจอ → log + ข้าม ไม่พัง

### 3.2 `BoComedySetup.cs` (Editor tool — convention R24-R31)
- MenuItem: `Tools/CueStrike/Mascots/70. Setup Bo Comedy Director`
- `PrefabUtility.LoadPrefabContents` → เพิ่ม `BoComedyDirector` เข้า **BoPanda_Prefab** (ฉากไหนมี Bo ได้ผลอัตโนมัติ)
- Idempotent + self-test (ตรวจ component + trigger 3 ตัวใน controller) + batchmode

## 4. Verify

- [ ] Compile gate batchmode: 0 errors
- [ ] Tool รันจริง + self-test ผ่าน
- [ ] prefab มี BoComedyDirector จริง
- [ ] main checkout คืนสภาพสะอาด
- [ ] docs: CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md

## 5. ขอบเขต (ไม่ทำ)

- ไม่สร้าง animation ใหม่ (ใช้ Disappointed = หลับ, Speak = มึน)
- ไม่ทำ AI opponent (R31 แยก) / ไม่ผูก UncleNok กับ events (R31 แยก)
