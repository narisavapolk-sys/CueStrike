# 🔊 CueStrike — AAA Audio System Plan
> **Project:** CueStrike VR Billiards (AAA Unity, Meta Quest 2/3)
> **Date:** 2026-08-02 (อัปเดตรอบดึก — บันทึกไว้มาต่อตอนเช้า)
> **Status:** ⏳ วางแผนเรียบร้อย — รอปฏิบัติตาม Checklist
> **อนุมัติโดย:** พี่โม่ง ✅ (เพิ่มเสียง Near-Miss Gasp)

---

## 🎯 เป้าหมาย

ยกระดับเสียงจาก "การสังเคราะห์ (Synth)" เป็น **ไฟล์เสียงจริงคุณภาพ AAA (.wav)** โดยรักษาระบบ Dynamic Volume/Pitch + Spatial Audio ที่มีอยู่เดิมไว้ แล้วเพิ่มเสียงที่ยังขาด

---

## 🔍 ระบบเสียงที่มีอยู่แล้ว (ห้ามทำซ้ำ — มีครบแล้ว)

| ไฟล์ | ฟีเจอร์ที่มีอยู่ |
|------|----------------|
| `Audio/CueStrikeAudioManager.cs` | AudioManager Singleton + PlayOneShot + ambient music |
| `Audio/CueStrikeDynamicPhysicsSFX.cs` | ✅ Dynamic Volume ตามแรงชน + Random Pitch + Play3DHit(position, velocity) + เลือก clip ตามแรง (hard/medium/soft) |
| `Physics/CueStrikeBallPhysics.cs` | OnCollisionEnter → เรียก AudioManager (relativeVelocity) |
| `Physics/CueStrikeSnookerPhysics.cs` | impactSpeed → เล่นเสียงตามแรง |
| `Audio/CueStrikeChampionshipCrowd.cs` | เสียงฝูงชน + ambient + applause + cheer + **gasp** + random pitch |
| `Audio/CueStrikeRealisticAudioSynth.cs` | สังเคราะห์เสียงฟิสิกส์ (ชน/เด้ง/ถูผ้า) |
| `MascotSystem/UncleNokReferee.cs` | เสียงผู้ตัดสิน |

> ✅ Dynamic Volume / Pitch / Random Pitch / Spatial Audio / Audio Manager **มีครบแล้ว** — งานคือใส่ไฟล์เสียงจริง + เพิ่มเสียงใหม่

---

## 📋 ขั้นตอนที่ 1: เสียงที่ต้องได้มา (.wav จริง)

| # | เสียง | สถานะเดิม | เป้าหมาย | วิธีได้มา |
|---|-------|----------|----------|----------|
| 1 | 🎱 **Ball Collision** (ชนกัน) | มี synth | .wav จริงหลายแบบ (กันซ้ำ) | AI Gen / คลิป / Sonniss |
| 2 | 🕳️ **Pocket Drop** (ลูกลงหลุม) | ❌ ไม่ชัด | thud หนัก + ลูกกลิ้งในรางไม้ | AI Gen: *"Heavy pool ball dropping into leather-lined pocket, muffled thud, rolling wooden track"* |
| 3 | 🎯 **Near-Miss Gasp** (ลูกเกือบลงแต่พลาด) | มี gasp clips | 🔊 **เสียงผู้ชมอึกอักตกใจ** | 🆕 ตามพี่โม่งสั่งเพิ่ม — ใช้ _gaspClips ที่มี + NearMissDetector ใหม่ |
| 4 | 👏 **Crowd Applause** | มีแล้ว | เพิ่มคุณภาพ/หลากหลาย | คลิป https://youtu.be/kB26rlNc9u4 → ตัดเสียงพากย์ (Vocalremover/Gaudio) |
| 5 | 🎶 **Ambient** (แอร์/ซุบซิบ) | มีแล้ว | อัปเกรดคุณภาพ | AI Gen / Sound Library |

**Prompt สำหรับ AI Gen (ที่พี่ให้มา):**
- Collision: *"Extreme close-up, high-quality sound of two professional phenolic resin pool balls colliding, sharp 'clack', indoor pool hall acoustics, no background noise"*
- Pocket: *"Heavy pool ball dropping into a leather-lined pocket, muffled thud, followed by rolling in wooden track, realistic"*
- Crowd: *"Atmospheric crowd applause in a large luxury billiard hall, polite clapping, occasional hushed 'ooh' and 'aah', echoing space, VR 360 soundscape"*
- **Near-Miss Gasp (ใหม่):** *"A crowd of spectators sharply inhaling in collective shock and disappointment, brief 'oohh' gasp after a near-miss shot, hushed murmuring, indoor arena"*

---

## 📋 ขั้นตอนที่ 2: ไฟล์ C# ที่ต้องสร้าง/ปรับปรุง

| ไฟล์ | การกระทำ | รายละเอียด |
|------|----------|-----------|
| `Audio/BallSoundController.cs` | 🆕 NEW | ตาม code ที่พี่ให้มา: ตรวจชน (Ball/Cushion) → เลือก clip สุ่ม → volume ตาม impactVelocity → pitch สุ่ม 0.9–1.1 → PlayOneShot |
| `Audio/PocketSoundDetector.cs` | 🆕 NEW | ตรวจจับลูกเข้าหลุม → เล่นเสียง thud + rolling (Spatial ที่ตำแหน่งหลุม) |
| `Audio/NearMissDetector.cs` | 🆕 NEW | **ตรวจจับลูกผ่านใกล้หลุมมาก (< ระยะกำหนด) แต่ไม่ลง → เล่นเสียงผู้ชมอึกอักตกใจ (_gaspClips)** |
| `Audio/CueStrikeAudioManager.cs` | 🔄 ปรับปรุง | เพิ่มตัวแปร clip: pocketHit, pocketRoll, ambient, nearMiss |
| `Audio/CueStrikeDynamicPhysicsSFX.cs` | ✅ คงเดิม | รับ clip ใหม่จาก AudioManager (ไม่ต้องแก้ logic) |

### 🔑 ระบบ Near-Miss Gasp (ตามที่พี่สั่งเพิ่ม) 🎯
- **ตรวจจับ:** เมื่อลูกวิ่งผ่านเขตหลุม (รัศมี ~0.15 ม. รอบปากหลุม) แล้ว **ไม่อยู่ในหลุม** (ไม่ถูก PocketDetector จับ)
- **เงื่อนไข:** ลูกต้องมีความเร็วสูงพอ (> threshold) + เป็นลูกที่ "เกือบลงจริงๆ" (ไม่ใช่ลูกวิ่งผ่านห่างๆ)
- **ผล:** Crowd System เล่น `_gaspClips` (อึกอักตกใจ) ที่ตำแหน่งหลุมนั้น → Spatial Audio ให้ผู้เล่นได้ยินทิศทางถูกต้อง
- **กันซ้ำ:** Cooldown ~1.5 วิ กันเสียง gasp ซ้ำถี่เกิน

---

## 📋 ขั้นตอนที่ 3: Pipeline ไฟล์เสียง (ส่วนที่พี่โม่งช่วย)

1. พี่โหลด/สร้างไฟล์ .wav:
   - **คลิปที่พี่ส่ง** → ใช้ Vocalremover.org / Gaudio Studio ตัดเสียงพากย์ → Ambient + SFX + Crowd
   - **Sonniss GDC Bundle** (ฟรีทุกปี) / MyEdit / Stable Audio / ElevenLabs ตาม prompt ด้านบน
2. พี่วางไฟล์ไว้ที่: `Assets/CueStrike/Audio/Clips/`
3. ผม (นาริ) ผูกไฟล์เข้ากับ AudioManager + สร้าง ScriptableObject/Inspector ลาก clip เข้า

---

## 🧠 หลัก AAA ที่ใช้ (จากที่พี่ส่งมา)
- เสียง "ดัง-เบา" ตามความแรงชน (Impact Velocity) ✅ มีแล้ว
- Random Pitch กันเสียงซ้ำปลอม ✅ มีแล้ว
- Spatial Audio 3D — ได้ยินตำแหน่งถูกต้องใน VR ✅ มีแล้ว
- เสียงลูกไกลหลุมเบากว่าลูกชนใกล้ตัว (Unity 3D Sound อัตโนมัติ) ✅ มีแล้ว
- **เสียง = 50% ของความสมจริงใน VR** 🎯

---

## 📁 โครงสร้างไฟล์

```
AUDIO_SYSTEM_PLAN.md                        ← ไฟล์นี้ (แผนเสียง)
Assets/CueStrike/Audio/
├── CueStrikeAudioManager.cs                ← 🔄 ปรับปรุง (เพิ่ม clip)
├── CueStrikeDynamicPhysicsSFX.cs           ← ✅ คงเดิม
├── BallSoundController.cs                  ← 🆕 NEW (ตามที่พี่ให้มา)
├── PocketSoundDetector.cs                  ← 🆕 NEW (ลูกลงหลุม)
├── NearMissDetector.cs                     ← 🆕 NEW (เสียงอึกอักตกใจ ตามพี่เพิ่ม)
└── Clips/                                  ← 📥 โฟลเดอร์ใส่ .wav (พี่โหลดมา)
```

---

## ✅ Checklist

- [ ] **1.** พี่โม่งโหลด/สร้างไฟล์ .wav → วางใน `Assets/CueStrike/Audio/Clips/`
- [ ] **2.** เขียน `BallSoundController.cs` (Dynamic Volume + Random Pitch)
- [ ] **3.** เขียน `PocketSoundDetector.cs` (ลูกลงหลุม)
- [ ] **4.** เขียน `NearMissDetector.cs` 🔊 **เสียงผู้ชมอึกอักตกใจ (พี่สั่งเพิ่ม)**
- [ ] **5.** ปรับปรุง `CueStrikeAudioManager.cs` (เพิ่ม clip ใหม่)
- [ ] **6.** ผูก clip ลง Inspector / ScriptableObject
- [ ] **7.** Compile batchmode → 0 errors
- [ ] **8.** ทดสอบใน Play Mode: ชนแรง/เบา, ลูกลงหลุม, เกือบลงแต่พลาด (Gasp)

---

## ⚠️ ข้อควรจำ
- URP เท่านั้น / ปิด Unity ก่อนแก้โค้ด / compile 0 errors
- เสียง .wav แนะนำ 44.1kHz: SFX = Mono 16-bit, Ambient = Stereo
- ไม่ลบระบบ synth เดิม (เป็น backup ถ้าไม่มีไฟล์ .wav)

---

*Document Version: 2026-08-02 v1 | อนุมัติโดยพี่โม่ง + เพิ่ม Near-Miss Gasp | มาต่อจาก Checklist ครับ*