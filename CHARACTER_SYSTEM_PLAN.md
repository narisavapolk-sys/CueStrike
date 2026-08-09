# 🎱 CueStrike — Social Club & Character System Master Plan
> **Project:** CueStrike VR Billiards (AAA Unity, Meta Quest 2/3)
> **Date:** 2026-08-02 (อัปเดตรอบดึก — บันทึกไว้มาต่อตอนเช้า)
> **Status:** ⏳ ยังไม่เริ่ม implement — รออนุมัติจากโม่ง
> **Blender Path:** `C:\Program Files\Blender Foundation\Blender 3.6\blender.exe`

---

## 🎯 เป้าหมายรวม

ยกระดับ CueStrike จาก "เกมบิลเลียด" เป็น **"Virtual Sports Club ระดับ AAA"** ประกอบด้วย 6 ระบบ:

| # | ระบบ | สถานะ |
|---|------|-------|
| 1 | **Character System** — ตัวละครผู้เล่น + Billboard โปร่งแสง | 📝 วางแผนแล้ว |
| 2 | **Stance Reference** — ท่าทางนักบิลเลียดมือโปร (จากคลิป YouTube) | 🆕 เพิ่มใหม่ |
| 3 | **Cinema WebView** — จอโรงหนัง 3D ใน Nok's Grand Hall | 🆕 เพิ่มใหม่ |
| 4 | **Sports Club Activities** — 4 กิจกรรมป่วนคลายเครียด | 🆕 เพิ่มใหม่ |
| 5 | **Anti-Tilt Machines** — 3 เครื่องเล่นคลายหัวร้อน | 🆕 เพิ่มใหม่ |
| 6 | **Dev Cheats** — 4 ปุ่มโกงหลังบ้าน + Key Combo ลับ | 🆕 เพิ่มใหม่ |

---

## 1️⃣ CHARACTER SYSTEM (ตัวละคร)

### 👥 รายชื่อตัวละคร (12 ตัว — แก้ไขแล้ว)

| # | ตัวละคร | บทบาท | โฟลเดอร์ | รูปภาพ | สถานะ |
|---|---------|-------|----------|--------|-------|
| 1 | Somchay | ผู้เล่น | `Characters/Somchay/` | `SomChay.png` | ✅ มี Prefab |
| 2 | MeiLing | ผู้เล่น | `Characters/MeiLing/` | `image_1776936011352.jpeg` | ✅ มี Prefab |
| 3 | Gentleman | ผู้เล่น | `Characters/Gentleman/` | `Gentleman Player .png` | ✅ มี Prefab |
| 4 | **PanPan** | **ผู้เล่น** | `Characters/PanPan/` | `PanPan.jpeg` | ⚠️ มีแค่รูป |
| 5 | Finn | ผู้เล่น | `Characters/Finn/` | `Finn.jpeg` | ⚠️ มีแค่รูป |
| 6 | KingFlex | ผู้เล่น | `Characters/KingFlex/` | `KingFlex.jpeg` | ⚠️ มีแค่รูป |
| 7 | Tusker | ผู้เล่น | `Characters/Tusker/` | `Tusker.jpeg` | ⚠️ มีแค่รูป |
| 8 | Phantom | ผู้เล่น | `Characters/Phantom/` | `Phantom.png` | ⚠️ มีแค่รูป |
| 9 | Cassidy | ผู้เล่น | `Characters/Cassidy/` | `Cassidy.jpeg` | ⚠️ มีแค่รูป |
| 10 | Bones | ผู้เล่น | `Characters/Bones/` | `Bones.jpeg` | ⚠️ มีแค่รูป |
| 11 | **Bo (โบ)** | **มาสคอต** 🐼 | `Characters/BoPanda/` | `BoPanda.png` | ⚠️ มีแค่รูป + Banter |
| 12 | Uncle Nok (ลุงโน๊ก) | ผู้ตัดสิน/มุมห้อง | `Characters/` root | `Uncle Nok.jpeg` | ⚠️ มีแค่รูป + Mascot |

> ✅ **แก้แล้ว:** PanPan = ผู้เล่น (ไม่ใช่ Pandy) | Bo = มาสคอต (ไม่ใช่ผู้เล่น) | ลบ Pose Coach ตามสั่ง

### บิลด์ตัวละครผู้เล่น (AAA Humanoid Rig)
ใช้สคริปต์เดิม `BlenderScripts/create_character_aaa.py` ที่มีอยู่แล้ว:
- สร้าง **low-poly humanoid body** + **Rigify rig (T-pose)** → นำเข้า Unity เป็น Humanoid Avatar
- รองรับ **IK** ให้ตัวละครขยับตามหัว/มือ/เอวของผู้เล่นจริง
- รันทีละตัว: `blender --background --python create_character_aaa.py -- <ชื่อตัวละคร>`

### Billboard โปร่งแสง (ตัวละครประกอบ/Crowd — 11 ตัว + ลุงโน๊ก)
สร้างสคริปต์ใหม่ `BlenderScripts/create_characters_aaa.py`:
- **Billboard Quad** (1.0 × 1.8 ม.) จำนวน 12 อัน ใส่รูปจริงจาก `Assets/CueStrike/Characters/`
- **Material โปร่งแสง Hologram** (เห็นทะลุ ไม่บังวิว + ชื่อใต้ตัว)
- **เบามาก** — 1 quad = 2 สามเหลี่ยม เหมาะกับ VR Quest
- Export FBX → `Assets/CueStrike/Models/AAA_Characters/`
- ใช้เป็นผู้ชมใน Crowd System + ผู้เล่นคนอื่นใน Multiplayer (ประหยัด performance)

---

## 2️⃣ STANCE REFERENCE — ท่าทางนักบิลเลียดมือโปร 🎯

> จากคลิป YouTube ที่พี่โม่งส่ง (https://youtu.be/kB26rlNc9u4) — หลัก 4 ข้อ

| # | ท่าทาง | รายละเอียดทางเทคนิค | ค่าอ้างอิงในโค้ด |
|---|--------|---------------------|----------------|
| 1 | **Open Bridge** | ไม้คิววางระหว่างนิ้วโป้งกับนิ้วชี้ (มือข้างไม่ถนัด) | Bridge điểm = จุดวางคิว |
| 2 | **Cue on Chin** | ไม้คิวแตะคางตรงกลาง → แนวคิวอยู่ระนาบเดียวกับสายตา | Head-Cue alignment |
| 3 | **Elbow 90°** | ข้อศอกตั้งฉากกับพื้นตอนเล็ง → ดึง/แทงสม่ำเสมอ | Elbow angle = 90° |
| 4 | **Staggered Stance** | ขาหนึ่งตรง อีกข้างย่อ → ศูนย์ถ่วงมั่นคง | Hip/leg offset |

### ไฟล์ใหม่: `Assets/CueStrike/Characters/StanceReferenceData.cs`
- ScriptableObject เก็บค่ามาตรฐาน (มุมศอก, ระยะคาง-คิว, มุมสะโพก, ตำแหน่งเท้า)
- `CharacterIKAssist.cs` ใช้ค่านี้ทำให้ avatar ก้ม/วางมือเป็นท่ามือโปรอัตโนมัติ
- อนาคต: ระบบ "Pose Feedback" (ไม่ใช่ Coach — แค่แสดงองศาเทียบมาตรฐานใน Debug) — **ไม่ทำ Coach ตามที่พี่สั่งลบ**

---

## 3️⃣ CINEMA WEBVIEW — จอโรงหนัง 3D 🎬📺

### แนวคิด
จอโปรเจกเตอร์ยักษ์บนกำแพง Nok's Grand Hall → สตรีม YouTube จริง / เพลงแจ๊ส / วิดีโอสอนสปิน ขณะเล่นสนุกเกอร์
Bo 🐼 นั่งโซฟาถือป๊อปคอร์นดูจอไปด้วย + เชียร์ตอนช็อตเด็ด

### เทคโนโลยี (ตามพี่แก้ไข = **WebView**)
| ตัวเลือก | ข้อดี | ข้อเสีย |
|---------|------|--------|
| ✅ **Vuplex 3D WebView for Android** | เรนเดอร์ลง Quad 3D ได้ตรงๆ, รองรับ VR + Laser Pointer, คลิก/พิมพ์/สตรีมได้ | เสียค่า license (~$50) |
| 🔄 WebView แบบ Open Source (WebViewObject) | ฟรี | เรนเดอร์เป็น 2D overlay — ต้องทำ Quad จำลอง |

> ⚠️ พี่ตอบว่า "เป็น WEBVIEW" → **ใช้ Vuplex 3D WebView for Android** เป็นหลัก (สำหรับ Quad 3D ใน VR)

### ฟีเจอร์
- เปิด/ปิดจอได้ (จอยักษ์ ~5 ม. บนกำแพง)
- Laser Pointer ชี้ + คลิกเลือกวิดีโอ/เพลง
- ระบบเสียงสเตอริโอในห้อง
- Bo เชียร์/หัวเราะตามจังหวะช็อตเด็ด

### ไฟล์ใหม่
| ไฟล์ | หน้าที่ |
|------|--------|
| `Assets/CueStrike/Environment/CinemaScreenManager.cs` | ควบคุมจอ WebView, เปิด/ปิด, โหลด URL |
| `Assets/CueStrike/Environment/BoCinemaWatcher.cs` | Bo นั่งดูจอ + โซฟา + ป๊อปคอร์น + เชียร์ |

---

## 4️⃣ SPORTS CLUB ACTIVITIES — 4 กิจกรรมป่วนคลายเครียด 🎯🍻🔫🎶

| # | กิจกรรม | รายละเอียด |
|---|---------|-----------|
| 1 | **Physics Dartboard** 🎯 | เป้าลูกดอกบนกำแพง, ปาแบบฟิสิกส์ 1:1, คะแนนขึ้นที่เป้า, เสียงปึก ๆ |
| 2 | **Virtual Bar & Feeding** 🍻 | หยิบแก้ว/จิบ/ชนแก้ว (เสียงคริสตัล) + โยนถั่วป้อน **ลุงโน๊ก🐘** / โยนไผ่ป้อน **โบ🐼** → แอนิเมชันเคี้ยว |
| 3 | **Toy Foam Blaster** 🔫 | ปืนโฟม ยิงแปะหน้าเพื่อน/ลูก/แว่นลุงโน๊ก + ปุ่ม "Mute Foam Gun" |
| 4 | **Victorian Gramophone** 🎶 | เครื่องเล่นแผ่นเสียงทองเหลือง เลือกแผ่นแจ๊ส/คลาสสิก หมุนแกนเปลี่ยนเพลง |

### ไฟล์ใหม่ (หมวดนี้ทั้งหมด)
| ไฟล์ | หน้าที่ |
|------|--------|
| `Props/DartboardGame.cs` | ฟิสิกส์ลูกดอก + คะแนน + เสียงปัก |
| `Props/BarFeedingSystem.cs` | การหยิบจับแก้ว + ชนแก้ว + ระบบป้อนอาหารมาสคอต |
| `Props/FoamBlaster.cs` | ปืนโฟม + กระสุน + ปุ่ม Mute |
| `Props/GramophoneSystem.cs` | แผ่นเสียง + การหมุน + เปลี่ยนเพลงเบื้องหลัง |
| `Props/BarProps.cs` | สร้างแก้ว/เหยือก/ขวด/ถังป๊อปคอร์น (ฟิสิกส์จับได้) |

---

## 5️⃣ ANTI-TILT MACHINES — เครื่องเล่นคลายหัวร้อน 🥊🔨

| # | เครื่องเล่น | รายละเอียด |
|---|-----------|-----------|
| 1 | **Physics Punching Bag** 🥊 | กระสอบทรายแขวนเสา สามารถต่อย/ฟาดด้วยไม้คิว, ฟิสิกส์เหวี่ยง + Haptics + เสียงปึ้ก |
| 2 | **Arcade Punching Machine** 📟 | ตู้ชกมวยวัดแรงหมัด ดิจิทัลนับคะแนน → ขิงเพื่อนในมัลติเพลเยอร์ |
| 3 | **Bo's Whack-A-Mole** 🔨🐼 | ตู้ค้อนทุบหัวตัวตุ่น **หน้าตาโบ** โผล่สลับ → เก็บแต้มทุบสถิติ |

### ไฟล์ใหม่ (หมวดนี้ทั้งหมด)
| ไฟล์ | หน้าที่ |
|------|--------|
| `Props/PunchingBag.cs` | ฟิสิกส์กระสอบทราย + แรงเหวี่ยง + Haptics |
| `Props/ArcadePunchMachine.cs` | วัดแรงหมัด + จอคะแนนดิจิทัล |
| `Props/WhackAMoleGame.cs` | ตู้มินิเกมตัวตุ่นโบ + ค้อน VR + สถิติ |

---

## 6️⃣ DEV CHEATS — ปุ่มโกงหลังบ้าน 🤫

| # | Cheat | รายละเอียด |
|---|-------|-----------|
| 1 | **Infinite Raycast Aim** 📏🔮 | เส้นนำทองสะท้อนทะลุกำแพง มองเห็นวิถี 3–4 ชิ่งก่อนยิง |
| 2 | **Bo's Distraction Prank** 🐼🍌 | ส่งโบวิ่งบังกล้องเพื่อน / ป๊อปคอร์นลอยผ่านหน้า 10 วิ (แล้วหยุดจนกว่าเลือกใหม่) |

### 🔐 วิธีเปิด (Key Combo ลับ — ผู้เล่นปกติไม่รู้แน่นอน)
```
กด Left Grip ค้าง + คลิก Analog ขวา 3 ครั้ง → เปิด Dev Cheats Panel
```

### ไฟล์ใหม่
| ไฟล์ | หน้าที่ |
|------|--------|
| `Scripts/Core/CueStrikeDevCheats.cs` | Key Combo + Cheat 1 (Raycast Aim) + Cheat 2 (Bo Prank) |
| `Scripts/Core/CueStrikeDevCheatPanelUI.cs` | Panel UI สำหรับเปิด/ปิด Cheats (เฉพาะ Dev) |

---

## 📁 โครงสร้างไฟล์ทั้งหมด (สรุป)

```
BlenderScripts/
├── create_character_aaa.py        ← มีอยู่แล้ว (Rigify humanoid rig)
└── create_characters_aaa.py       ← NEW: Billboard โปร่งแสง 12 ตัว

Assets/CueStrike/
├── Characters/
│   ├── CharacterIKAssist.cs       ← NEW: IK ก้มท่ามือโปร
│   ├── StanceReferenceData.cs     ← NEW: ค่ามาตรฐานท่ามือโปร (ScriptableObject)
│   └── CharacterTransparencyShader.shader ← NEW: Hologram โปร่งแสง URP
├── Environment/
│   ├── CinemaScreenManager.cs     ← NEW: จอ WebView 3D
│   └── BoCinemaWatcher.cs         ← NEW: โบดูจอ + เชียร์
├── Props/
│   ├── DartboardGame.cs           ← NEW
│   ├── BarFeedingSystem.cs        ← NEW
│   ├── FoamBlaster.cs             ← NEW
│   ├── GramophoneSystem.cs        ← NEW
│   ├── BarProps.cs                ← NEW
│   ├── PunchingBag.cs             ← NEW
│   ├── ArcadePunchMachine.cs      ← NEW
│   ├── WhackAMoleGame.cs          ← NEW
├── Scripts/Core/
│   ├── CueStrikeDevCheats.cs      ← NEW
│   └── CueStrikeDevCheatPanelUI.cs ← NEW
├── Editor/
│   ├── CharacterAAASetup.cs       ← NEW: Apply button
│   └── CharacterAAASelfTest.cs    ← NEW: Test button
└── Models/AAA_Characters/         ← FBX Billboard 12 ตัว
```

---

===== REPLACE
## ✅ Checklist การทำงาน

- [ ] **1. Blender:** เขียน `create_characters_aaa.py` → รัน Blender 3.6 → ได้ Billboard FBX 12 ตัว
- [ ] **2. Blender:** รัน `create_character_aaa.py` สร้าง humanoid rig (ถ้าต้องการตัวหลัก 3 ตัวใหม่)
- [ ] **3. Unity:** ย้าย FBX เข้า `Assets/CueStrike/Models/AAA_Characters/`
- [ ] **4. Unity:** เขียน `CharacterTransparencyShader.shader` (URP Hologram)
- [x] **5. Unity:** เขียน `StanceReferenceData.cs` + `CharacterIKAssist.cs` (ท่ามือโปร)
- [ ] **6. Unity:** เขียน `CinemaScreenManager.cs` + `BoCinemaWatcher.cs` (WebView)
- [ ] **7. Unity:** เขียน 4 Sports Club Activities (Dartboard, Bar, Foam, Gramophone)
- [ ] **8. Unity:** เขียน 3 Anti-Tilt Machines (Punching Bag, Punch Machine, Whack-A-Mole)
- [ ] **9. Unity:** เขียน Dev Cheats (Key Combo + 2 Cheats + UI)
- [x] **10. Editor:** เขียน `CharacterAAASetup.cs` + `CharacterAAASelfTest.cs`
- [ ] **11. Compile:** รัน batchmode compile → 0 errors
- [ ] **12. อัปเดต:** `CUESTRIKE_MASTER.md` (เพิ่ม Section ใหม่ + สถานะ)
- [ ] **13. ทดสอบใน Unity:** Apply → Self-Test → Play Mode

---

## ⚠️ ข้อควรจำ (จาก Master Doc)

- ใช้ **URP เท่านั้น** — ห้าม Standard Shader (จะขึ้นสีชมพู)
- ปิด Unity Editor ก่อนแก้โค้ดเสมอ → รัน batchmode compile → 0 errors
- Manager ใช้ Singleton, Communication เป็น event-driven
- ชื่อตัวละคร: **PanPan** (ไม่ใช่ Pandy), **Bo** = มาสคอต
- พูดกับโม่งเป็นภาษาไทย สุภาพ อ่อนหวาน

---

*Document Version: 2026-08-02 v2 | ครอบคลุม 6 ระบบ | มาต่อได้จาก Checklist ด้านบนเลยครับ*