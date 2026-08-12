# Implementation Plan — R33: วาง BoPanda ลงห้องแข่ง (AAA_RoomDAY + Snooker_Demo)

> **กฎข้อ 5:** เขียน plan ก่อน implement — อัปเดตเอกสาร + verify compile ในรอบเดียวกัน
> **Branch:** `feat/r33-bopanda-match-scenes` (base: `main` @ `05cdc67` รวม R29/R30 แล้ว; R32/PR#26 ยัง CI — ไม่ชนกัน: R32 แก้ BoPanda prefab, งานนี้วาง instance ในฉาก)
> **วันที่:** 2026-08-12

---

## 1. เป้าหมาย (ตามคำสั่งพี่โม่ง)

วาง BoPanda ลง AAA_RoomDAY + Snooker_Demo ด้วย (ตอนนี้มีแค่ UncleNok) — ให้ห้องแข่งมีคู่พิธีกรครบ (ลุงโน๊ก referee + โบกองเชียร์)

## 2. Findings จากการตรวจโค้ดจริง (กฎข้อ 1)

| # | พบ | สถานะ |
|---|-----|--------|
| 1 | BoPanda อยู่ใน **แค่ Title** (GUID c62e9006) | ❌ ต้องเพิ่มห้องแข่ง |
| 2 | AAA_RoomDAY + Snooker_Demo มี UncleNok อยู่แล้ว (R29 ที่ (0,0,-4.6)) | ✅ |
| 3 | Tool R29 `MascotScenePlacementSetup.cs` จัดการ **แค่ UncleNok** — ขยายให้วาง BoPanda ด้วย | ⚠️ |
| 4 | BoPanda prefab มี Animator + controller + BoPandaBanter + BoComedyDirector (R32 กำลัง CI) | ✅ |
| 5 | โต๊ะ AAA อยู่ (0, 0.4, 0) scale (4, 0.5, 8) / Snooker โต๊ะ origin | ℹ️ |

## 3. แผนงาน

### 3.1 ขยาย `MascotScenePlacementSetup.cs`
- เพิ่ม `BoPandaPrefabPath` + ตำแหน่งวาง BoPanda:
  - **AAA_RoomDAY**: (0, 0, 4.6) — ฝั่งตรงข้ามลุงโน๊ก (0, 0, -4.6) → คู่พิธีกรยืนคนละฝั่งโต๊ะ
  - **Snooker_Demo**: (0, 0, 4.6) — เดียวกัน
- Idempotent: เช็ค GameObject ชื่อมี "BoPanda" ในฉาก → ข้าม (กัน duplicate)
- ไม่แตะ Title (มี Bo อยู่แล้ว)
- Update self-test: ตรวจ BoPanda ในฉากเป้าหมาย

### 3.2 ทำไมวางที่ (0, 0, 4.6)?
- ลุงโน๊ก (referee) อยู่ (0, 0, -4.6) ริมโต๊ะฝั่งหนึ่ง
- โบ (กองเชียร์) ควรอยู่ฝั่งตรงข้าม (0, 0, 4.6) — มองเห็นลุง + ผู้เล่นชัดเจน
- ตำแหน่งหันหน้าเข้าหาโต๊ะ (rotation 0)

## 4. Verify

- [ ] Compile gate batchmode: 0 errors
- [ ] Tool รันจริง: วาง BoPanda 2 ฉาก + idempotent (รันซ้ำ skip)
- [ ] self-test ผ่าน
- [ ] main checkout คืนสภาพสะอาด
- [ ] docs: CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md

## 5. ขอบเขตงาน (ไม่ทำ)

- ไม่แก้ BoPanda prefab (R32 จัดการ)
- ไม่วาง Bo ลงห้อง 8 ตัว (เล่นผ่าน scene picker เท่านั้น)
- ไม่แตะ Title (มี Bo อยู่แล้ว)
