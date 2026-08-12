# Implementation Plan — R40: Bo เป็นกรรมการ (14 voice clips) + ลุงเป็นกองเชียร์

**วันที่:** 2026-08-12
**Branch:** `feat/r40-bo-referee` → `main`
**กฎ:** กฎข้อ 1 (ตรวจของจริง) + กฎข้อ 5 (plan ก่อน, docs + compile ในรอบเดียวกัน)

## หมายเหตุชื่อ R
พี่สั่งว่า "R37" แต่ **R37 ถูกใช้ไปแล้ว** (ChinesePool AI fix = PR #32) → งานนี้คือ **R40** ตาม roadmap (เหมือน R34/R35 case)

## เป้าหมาย
1. **ผูกเสียงน้องโบ 14 คลิปเข้า BoPanda_Prefab** (ลอกแบบ R30 ที่ทำลุง):
   - สร้าง `BoReferee.cs` (ลอก UncleNokReferee pattern — Random voice + cooldown + animation triggers)
   - สร้าง `BoRefereeEventBridge.cs` (ลอก UncleNokRefereeEventBridge — subscribe GameManager/WBPS events)
   - Editor tool: เพิ่ม AudioSource + assign _animator/_audioSource/_homePosition + assign 14 clips + bridge
2. **สลับบทบาท:** Bo เป็นกรรมการ (Random voice) / ลุงโน๊ะเป็นกองเชียร์ (disable `UncleNokRefereeEventBridge` ใน UncleNok_Prefab → ลุงไม่ประกาศคะแนน/ฟาวล์อีกต่อไป)

## สถานะจริงที่ตรวจพบ (กฎข้อ 1)

| รายการ | สถานะ | หลักฐาน |
|--------|--------|---------|
| เสียงโบ 14 ไฟล์บนดิสก์ | ✅ มีครบ | `Audio/Clips/Voice/NongBo/bo_*.wav` ×14 |
| เสียงโบผูกใน prefab | ❌ ไม่มี GUID ไหนอ้าง | grep GUID = 0 |
| `BoPandaBanter.cs` มีระบบเสียง | ❌ ไม่มี (แค่ UnityEvent + Debug.Log) | ตรวจทั้งไฟล์ |
| BoPanda prefab: AudioSource | ✅ มีแต่ `m_audioClip` ว่าง | prefab line 326 |
| BoPanda prefab: referee component | ❌ ไม่มี | มีแค่ BoPandaBanter + BoComedyDirector |
| UncleNok prefab: UncleNokReferee + bridge | ✅ มี (กรรมการคนปัจจุบัน) | line 313 + 469 |
| UncleNok 14 clips mapping | ✅ ชัดเจน (ลอก assign ได้) | prefab: matchStart 2/turnStart 2/potSuccess 3/century 1/highBreak 1/clearance 1/break 1/foulCalled 2/foulCueBall 1 |

## งานจริง

1. **`BoReferee.cs`** (ใหม่, runtime): ลอก UncleNokReferee — PlayRandomClip + CanAnnounce (cooldown) + OnFrameStart/OnMatchStart/OnPlayerTurnStart/OnBallPotted (potSuccess/century/highBreak/clearance)/OnFoulCommitted/OnBreakShot + TriggerAnimation (Speak/Celebrate/Disappointed)
2. **`BoRefereeEventBridge.cs`** (ใหม่, runtime): ลอก UncleNokRefereeEventBridge — subscribe ChinesePoolGameManager + WBPS → เรียก BoReferee methods (fail-safe retry)
3. **`BoVoicePinSetup.cs`** (ใหม่, Editor): ผูก BoReferee + AudioSource (3D spatial) + assign refs + assign 14 clips (ตาม mapping) + เพิ่ม bridge ลง BoPanda_Prefab — idempotent + self-test + batchmode
4. **สลับบทบาท:** disable `UncleNokRefereeEventBridge` ใน UncleNok_Prefab (ผ่าน tool) → ลุงเป็นกองเชียร์ (ยืนข้างสนาม, ยังมี animation idle/speak)
5. **Compile verify:** batchmode 0 errors (Library อุ่นบน main)
6. **รัน tool จริง** → BoPanda prefab ได้ referee + clips + bridge + ลุง bridge disabled
7. **Verify:** prefab YAML มี BoReferee + 14 clips + AudioSource + bridge; UncleNok prefab bridge disabled
8. **Docs:** CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md (R40 section)
9. **Commit + push + เปิด PR** ต่อ `main`

## ผลลัพธ์ที่คาดหวัง
- Bo เป็นกรรมการจริง: ประกาศคะแนน/ฟาวล์/เริ่มเฟรมด้วยเสียงโบ 14 คลิป (Random) + animation Speak/Celebrate/Disappointed
- ลุงเป็นกองเชียร์: ไม่ประกาศคะแนนอีก (bridge disabled) — ยืนดูข้างสนาม
- 3 ฉาก (Title/AAA_RoomDAY/Snooker_Demo) เป็น prefab instance → ได้ผลอัตโนมัติ

## ความเสี่ยง / หมายเหตุ
- งานนี้แตะ `BoPanda_Prefab.prefab` + `UncleNok_Prefab.prefab` + ไฟล์ใหม่ 4 ตัว (2 runtime + 1 editor + meta)
- ต้อง merge PR #34 (R39) ก่อน — แตะ docs เดียวกัน
- ไม่แตะโค้ดลุงเดิม (UncleNokReferee ยังอยู่ — แค่ปิด bridge)
