# Implementation Plan — R28: SFX 9 ช่อง (ผูก AudioSource + volume ตามแรงกระแทก)

> **กฎข้อ 5:** เขียน plan ก่อน implement — อัปเดตเอกสาร + verify compile ในรอบเดียวกัน
> **Branch:** `feat/r28-sfx-channels` (base: `main` @ `012ef25`)
> **วันที่:** 2026-08-12

---

## 1. เป้าหมาย (ตามคำสั่งพี่โม่ง)

ผูกช่อง SFX จริง 9 ตัวเข้ากับ AudioSource:
`ball hit, cushion, pocket, cue, chalk, crowd, ambient, ui_click, ui_hover`
+ volume ตามแรงกระแทก + เขียนตารางไฟล์ที่พี่ต้องหาไว้ใน TASK_PROGRESS

## 2. Findings จากการตรวจโค้ดจริง (กฎข้อ 1)

| # | พบ | สถานะ |
|---|-----|--------|
| 1 | ไฟล์ SFX 9 ตัว **มีอยู่แล้ว** ใน `Assets/CueStrike/Audio/Clips/` (placeholder สังเคราะห์จาก `CueStrikeAudioGenerator`) | ✅ มีไฟล์ |
| 2 | `CueStrikeAudioManager` มี field ครบ 9+ ช่อง + `PlayBallHit(intensity, cushionImpact)` + `PlayPocketAt` + `PlayChalk` + `PlayMenuClick/Hover` | ✅ โค้ดพร้อม |
| 3 | **AudioManager อยู่ในแค่ Title scene** — MainMenu, Boot, AAA_RoomDAY, Snooker_Demo + ห้อง 8 ตัวไม่มีเลย | ❌ ปัญหาใหญ่ |
| 4 | `CueStrikeBallPhysics` เรียก `PlayBallHit(impactMagnitude, cushionImpact)` อยู่แล้ว → volume ตามแรงมี logic แต่ต้องมี AudioManager ในฉาก | ⚠️ ขึ้นกับ #3 |
| 5 | Title assign เกือบครบ แต่ **hitMedium = cue_ball_hit (ผิดช่อง)** + nearMissGasp/ambientLoungeMusic ว่าง | ⚠️ ต้องแก้ |
| 6 | `CueStrikeCrowdSystem.ambientMurmur` ว่าง — ควรได้ `crowd_murmur.wav` | ⚠️ ต้อง assign |
| 7 | `CueStrikeDynamicPhysicsSFX` (3D spatial + velocity volume) ไม่อยู่ในฉากไหนเลย | ⚠️ เพิ่มได้ |

## 3. แผนงาน

### 3.1 ปรับ `CueStrikeAudioManager.cs`
- เพิ่ม field `public AudioClip cueStrike;` + method `PlayCueStrike(float intensity)` (volume ตามแรงยิง 0..1)
- เพิ่ม field `public AudioClip crowdAmbient;` (loop background crowd) + `PlayCrowdAmbient()`
- แก้ mapping ให้ถูกช่อง: hitSoft/hitMedium/hitHard = ball_ball_hit / cue_ball_hit(medium) / ball_ball_hit(hard) ตาม Title เดิม — จริงๆ ปรับให้ hitMedium เป็น ball_ball_hit (ไม่ใช่ cue_ball_hit ซึ่งเป็นเสียงคิวตีลูก)
- ไม่เปลี่ยน behavior เดิมของเมธอดที่มีอยู่ (รักษา API)

### 3.2 เขียน Editor tool `CueStrikeSfxSceneSetup.cs` (convention R24-R26)
- MenuItem: `Tools/CueStrike/Audio/40. Setup SFX Channels`
- สำหรับทุกฉากที่เล่นได้ (MainMenu, Boot, Title, AAA_RoomDAY, Snooker_Demo + ห้อง 8 ตัว):
  1. หา/สร้าง GameObject `AudioManager` + `CueStrikeAudioManager`
  2. assign 9 clips จาก `Assets/CueStrike/Audio/Clips/` (โหลดด้วย AssetDatabase — ไม่ hardcode GUID)
  3. เพิ่ม `CueStrikeDynamicPhysicsSFX` (3D impact + volume ตามแรง) ถ้ายังไม่มี
  4. assign `CrowdSystem.ambientMurmur = crowd_murmur.wav` ถ้ามี CrowdSystem ในฉาก
- แก้ Title: hitMedium → ball_ball_hit, nearMissGasp → crowd_murmur, ambientLoungeMusic → ambient_room_tone, cueStrike → cue_ball_hit
- Idempotent + self-test + batchmode (`-executeMethod`)

### 3.3 ตารางไฟล์ที่พี่ต้องหา (ใน TASK_PROGRESS.md)
| ช่อง | ไฟล์ปัจจุบัน | สถานะ | ที่พี่ต้องหา |
|------|-------------|--------|-------------|
| ball hit | `ball_ball_hit.wav` | ✅ placeholder | เสียงจริง (บิลเลียดลูกชน) |
| cushion | `ball_cushion_hit.wav` | ✅ placeholder | เสียงจริง (ลูกชนขอบ) |
| pocket | `ball_pocket_drop.wav` | ✅ placeholder | เสียงจริง (ลูกลงหลุม) |
| cue | `cue_ball_hit.wav` | ✅ placeholder | เสียงจริง (คิวตีลูก) |
| chalk | `chalk_scrape.wav` | ✅ placeholder | เสียงจริง (ถูชอล์ก) |
| crowd | `crowd_murmur.wav` | ✅ placeholder | เสียงจริง (เสียงผู้ชม) |
| ambient | `ambient_room_tone.wav` | ✅ placeholder | เสียงจริง (บรรยากาศห้อง) |
| ui_click | `ui_click.wav` | ✅ placeholder | เสียงจริง (กดปุ่ม) |
| ui_hover | `ui_hover.wav` | ✅ placeholder | เสียงจริง (ชี้ปุ่ม) |

## 4. Verify

- [ ] Compile gate batchmode: 0 errors
- [ ] Scene load + Editor tool idempotent + self-test ผ่าน
- [ ] main checkout คืนสภาพสะอาด
- [ ] docs: CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md

## 5. ขอบเขตงาน (ไม่ทำ)

- ไม่สร้างไฟล์เสียงใหม่ (รอพี่หาไฟล์จริง)
- ไม่แตะ voice clips (UncleNok/BoPanda — งาน R28 เดิม แยก PR)
- ไม่แก้ physics behavior ของลูก (มีอยู่แล้ว)
