# Implementation Plan — R30: Voice Pinning — UncleNokReferee + AudioSource + refs

> **กฎข้อ 5:** เขียน plan ก่อน implement — อัปเดตเอกสาร + verify compile ในรอบเดียวกัน
> **Branch:** `feat/r30-voice-pinning` (base: `main` @ `a65cb93` รวม R29 แล้ว)
> **วันที่:** 2026-08-12

---

## 1. เป้าหมาย (ตามคำสั่งพี่โม่ง)

ผูก UncleNokReferee 14 voice clips กับ prefab จริง — เพิ่ม AudioSource, assign `_animator`/`_audioSource`/`_homePosition` ใน prefab (และ 3 ฉากที่วางไว้แล้ว: Title, AAA_RoomDAY, Snooker_Demo)

## 2. Findings จากการตรวจโค้ดจริง (กฎข้อ 1)

| # | พบ | สถานะ |
|---|-----|--------|
| 1 | **clips 14 ตัว assign ครบแล้วใน prefab** (GUID ตรงไฟล์ Voice/UncleNok/*.wav ทุกตัว: match_start_01/02, turn_start_01/02, pot_success_01/02/03, century_break, high_break, break_shot, clearance, foul_called_01/02, foul_cueball) | ✅ ครบ |
| 2 | **prefab ไม่มี AudioSource component เลย** (grep = 0) → `_audioSource: {fileID: 0}` → เสียงไม่ออก | ❌ ต้องเพิ่ม |
| 3 | `_animator: {fileID: 0}` — มี Animator ใน prefab (fileID 1307204390460968239) แต่ ref ว่าง | ❌ ต้อง assign |
| 4 | `_homePosition: {fileID: 0}` — ควรชี้ root Transform (แต่ระวัง: script `Start()` ย้าย transform ไป homePosition → ถ้าชี้ self จะล็อกตำแหน่งเดิม ไม่ขยับ) | ⚠️ ต้องระวัง |
| 5 | 3 ฉากที่ R29 วาง UncleNok ไว้ (Title/AAA_RoomDAY/Snooker_Demo) เป็น **prefab instance** → แก้ prefab แล้วทุกฉากได้ผลอัตโนมัติ | ℹ️ |
| 6 | `Awake()` มี fallback `GetComponent<Animator>()` / `GetComponent<AudioSource>()` — assign ชัดเจนดีกว่า (ลด race) | ℹ️ |

## 3. แผนงาน

### 3.1 เขียน Editor tool `UncleNokVoicePinSetup.cs` (convention R24-R29)
- MenuItem: `Tools/CueStrike/Mascots/60. Pin UncleNok Voice & Refs`
- ใช้ `PrefabUtility.LoadPrefabContents(UncleNok_Prefab)` → Unity จัดการ fileID เอง:
  1. **เพิ่ม AudioSource** (ถ้ายังไม่มี): `spatialBlend = 1f`, `playOnAwake = false`, `rolloffMode = Logarithmic`, `maxDistance = 20f`
  2. **assign `_animator`** = Animator component ใน prefab (ตัวแรก)
  3. **assign `_audioSource`** = AudioSource ที่เพิ่งเพิ่ม
  4. **assign `_homePosition`** = root Transform ของ prefab
  5. `SavePrefabAsset` → prefab ใหม่
- Idempotent: ถ้า AudioSource + refs ครบแล้ว → ข้าม
- self-test: prefab มี AudioSource + Animator + refs ครบ + clips 14 ตัวไม่ว่าง

### 3.2 3 ฉาก (Title/AAA_RoomDAY/Snooker_Demo)
- เป็น prefab instance → **ไม่ต้องแก้ฉาก** (ได้ผลอัตโนมัติจาก prefab)
- tool ตรวจด้วยว่า instance มี `_animator` ถูกต้องหรือไม่ (optional)

### 3.3 homePosition — การตัดสินใจ
- `_homePosition` ชี้ root Transform → `Start()` จะย้ายลุงโน๊กไปที่ homePosition (ตำแหน่งเดิมของมันเอง) + `Update()` หมุนหันเข้าหา homePosition
- ผล: ลุงโน๊กจะ **หันเข้าหาจุดยืนเดิมตลอด** (ไม่หมุนตามอารมณ์) — ตรงกับ design ที่ต้องการให้ referee หันหน้าเข้าหาโต๊ะ/ผู้เล่น
- **ข้อควรระวัง:** ถ้า homePosition = self transform → `transform.position = _homePosition.position` = ไม่ขยับ (safe) + `LookRotation(lookDirection)` เมื่อ lookDirection = 0 → กัน crash ได้ (guard `if (lookDirection != Vector3.zero)` มีอยู่แล้วใน script)

## 4. Verify

- [ ] Compile gate batchmode: 0 errors
- [ ] Tool รันจริง + self-test ผ่าน (refs ครบ + clips ครบ)
- [ ] prefab มี AudioSource + refs จริง (ตรวจ YAML)
- [ ] main checkout คืนสภาพสะอาด
- [ ] docs: CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md

## 5. ขอบเขตงาน (ไม่ทำ)

- ไม่ผูก referee กับ game events (งาน R31 กรรมการจริง — แยก PR)
- ไม่แก้ clips (มีครบแล้ว)
- ไม่แตะ BoPanda (มี binder แยก — `CueStrikeVoiceBinderEditor`)
