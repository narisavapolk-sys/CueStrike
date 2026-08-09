# 🎯 CueStrike — Blender Script Workflow

## 📂 โฟลเดอร์นี้มีอะไรบ้าง

```
BlenderScripts/
├── README_BLENDER_WORKFLOW.md    ← ไฟล์นี้ (คำแนะนำ + Context Brief สำหรับ AI)
├── create_all_aaa_master.py      ← สคริปต์หลัก: รันทุกอย่างในครั้งเดียว (One Click!)
├── create_pool_balls_aaa.py      ← Script 1: สร้างลูกบอล 16 ลูก
├── create_table_textures_aaa.py  ← Script 2: สร้าง Texture โต๊ะ (ผ้า/ไม้/ขอบ)
├── create_cue_aaa.py             ← Script 3: สร้างไม้คิว
├── create_room_props_aaa.py      ← Script 4: สร้าง Props ห้อง + CrowdDummy
└── Exports/                      ← ของที่ Export ออกมา (FBX, PNG)
    └── Textures/                 ← Texture แยก
```

---

## 🧠 Context Brief — โครงการ CueStrike (สำหรับ AI Agent ตัวถัดไป)

> **สำคัญมาก:** Copy ส่วนนี้ไปให้ AI ตัวใหม่อ่านเพื่อ "สวมวิญญาณ" เป็นผู้ช่วยได้ทันที

### 📝 โครงการ: CueStrike VR (AAA Game Development)
**เป้าหมาย:** สร้างเกม Billiard/Pool ระดับ AAA สำหรับ VR โดยใช้ AI ช่วยเขียน Script เพื่อ Automation ระหว่าง Blender และ Unity

### 1. Stack เทคนิคที่ใช้
| ส่วน | เทคโนโลยี |
|------|-----------|
| Game Engine | Unity (**URP** — Universal Render Pipeline) |
| Modeling/Texturing | Blender 3.6 (รันผ่าน Python Script) |
| Automation | Python ใน Blender (สร้าง Model/UV/Export) + C# Editor Script ใน Unity (Auto-apply Textures/Materials) |
| Assets | โต๊ะพูล, ลูกบอล 16 ลูก, ไม้คิว, ฉากห้อง (9 แบบ), ระบบ CrowdDummy (ผู้ชม) |

### 2. กระบวนการทำงาน (Workflow Pipeline)
```
Input: User ส่งรูปอ้างอิงให้ AI
        ↓
Blender Side (Python): AI เขียนสคริปต์ควบคุม Blender 3.6
  → สร้างโมเดล, กาง UV, สร้าง Texture
  → Export เป็น .fbx และ .png เข้า Assets/CueStrike/ ใน Unity โดยตรง
        ↓
Unity Side (C#): AI เขียน Editor Script (Tools → CueStrike → Apply)
  → ดึงไฟล์ที่ Export มาเข้าหา Prefab อัตโนมัติ
  → ใส่ Texture ให้ลูกบอล, เปลี่ยนผ้าโต๊ะ, จัดวาง Props ในห้อง
```

### 3. รายชื่อสคริปต์หลักในระบบ
| สคริปต์ | ภาษา | หน้าที่ |
|---------|------|--------|
| `create_all_aaa_master.py` | Python (Blender) | สคริปต์หลัก รันทุกอย่าง 4 สคริปต์ย่อย |
| `CueStrikeAAAApplyAll.cs` | C# (Unity Editor) | `Tools → CueStrike → Apply → Apply All AAA` ประกอบร่าง Asset |

---

## 🚨 Critical Issue — Pink Material (สีชมพู) & วิธีแก้

### อาการ
หลังจากรัน Pipeline แล้ว ภาพที่ออกมาใน Unity เป็น **สีชมพู (Magenta)** ทั้งหมด

### สาเหตุ (วิเคราะห์แล้ว)
| # | สาเหตุ | รายละเอียด |
|---|--------|-----------|
| 1 | **Shader ไม่เข้ากับ Render Pipeline** | โปรเจกต์ใช้ **URP** แต่ Blender Export material มาพร้อม **Standard Shader** (Built-in) → URP แสดงผลเป็นสีชมพู |
| 2 | **Material Name ไม่ตรง** | บางครั้ง AI ใน Blender ไม่ได้ใส่ Material Name ให้ตรงกับที่ C# Script เรียกหา → Unity หา Material ไม่เจอ |
| 3 | **FBX-embedded Materials ไม่ถูกแปลง** | Material ที่ฝังมากับ FBX (ลูกบอล/ไม้คิว/Props) ถูกใช้ as-is โดยไม่ได้แปลง Shader เป็น URP |

### ✅ แนวทางแก้ (ทำแล้วใน C# Script)
ไฟล์: `Assets/CueStrike/Editor/CueStrikeAAAApplyAll.cs`

| ฟีเจอร์ | คำอธิบาย |
|---------|---------|
| **เมนูใหม่** | `Tools → CueStrike → Fix → Fix Pink Materials (URP Conversion)` |
| **อัตโนมัติ** | `Apply All AAA` จะเรียก `ConvertAllFBXMaterialsToURP()` เป็นขั้นตอน 1.5 ทุกครั้ง |
| **แปลง Material** | Extract Texture + Material ออกจาก FBX → แปลง Shader เป็น `Universal Render Pipeline/Lit` |
| **Self-Test** | เพิ่ม Test 3: ตรวจจับ Material ที่ไม่ใช้ URP shader → แจ้งเตือน "PINK MATERIAL DETECTED" |

### 📌 ข้อควรจำสำหรับ AI
1. **ห้ามใช้ Standard Shader** ในโปรเจกต์นี้ — ใช้ `Universal Render Pipeline/Lit` เท่านั้น
2. ตรวจ Render Pipeline ได้ที่ `ProjectSettings/GraphicsSettings.asset` (ดู `m_RenderPipelineGlobalSettingsMap`)
3. ตรวจ Material ที่ฝังใน .fbx: คลิก .fbx ใน Unity → Materials tab → ถ้า shader เป็น Standard ให้กด Fix
4. ถ้าหลังกด Fix แล้วยังชมพู → เช็คว่า Material เป็น .mat asset จริง (ไม่ใช่ embedded) และ Shader เป็น URP/Lit

---

## 🎱 Script 1: ลูกบิลเลียด 16 ลูก (Pool Balls)

### วิธีใช้
1. เปิด **Blender 3.6** (จะขึ้น Cube มาให้ ไม่ต้องสน)
2. เปลี่ยนไปที่ **Scripting** workspace (แถบด้านบน)
3. กด **New** (สร้าง Text ใหม่) → **แล้วแทนที่ด้วยเนื้อหา** ในไฟล์ `create_pool_balls_aaa.py`
   - หรือ: **Text → Open Text Block** → เลือก `BlenderScripts/create_pool_balls_aaa.py`
4. กด **Run Script** ▶ (หรือ Alt+P)
5. Blender จะสร้างลูกบอล 16 ลูกเรียงเป็นแถว แล้ว **Export FBX อัตโนมัติ**
6. เปิด `BlenderScripts/Exports/` → จะมี `CueStrike_PoolBalls_AAA.fbx`

### ผลลัพธ์
| ไฟล์ | ที่อยู่ |
|------|--------|
| `CueStrike_PoolBalls_AAA.fbx` | `BlenderScripts/Exports/` (แล้วย้ายเข้า `Assets/CueStrike/Models/AAA_Props/`) |

---

## 🖼️ Script 2: Texture โต๊ะ

### วิธีใช้
1. เปิด Blender → Scripting → วาง `create_table_textures_aaa.py`
2. กด **Run Script** ▶
3. Texture PNG จะถูกสร้างใน `BlenderScripts/Exports/Textures/`

### ผลลัพธ์ (9 ไฟล์)
| ไฟล์ | ใช้กับ |
|------|--------|
| `Felt_Snooker_Green.png` | ผ้าโต๊ะ Snooker (สีเขียว) |
| `Felt_Snooker_Green_Normal.png` | Normal map ขนผ้า |
| `Felt_Pool_Blue.png` | ผ้าโต๊ะ Pool/8-Ball (สีน้ำเงิน) |
| `Felt_Pool_Blue_Normal.png` | Normal map ขนผ้า |
| `Cushion_Rubber.png` | ขอบยางโต๊ะ |
| `Wood_Dark_Walnut.png` | โครงโต๊ะสีเข้ม |
| `Wood_Light_Oak.png` | โครงโต๊ะสีอ่อน |
| `Pocket_Leather.png` | หนังหลุม |
| `Diamond_Marker_Ivory.png` | แต้มบอกตำแหน่ง |

---

## 🎯 Script 3: ไม้คิว

### วิธีใช้
1. เปิด Blender → Scripting → วาง `create_cue_aaa.py`
2. กด **Run Script** ▶
3. FBX จะถูกสร้างที่ `BlenderScripts/Exports/CueStrike_Cue_AAA.fbx` (แล้วย้ายเข้า `Assets/CueStrike/Models/AAA_Props/`)

### ชิ้นส่วนของไม้คิว
| ส่วน | วัสดุ | สี |
|------|-------|-----|
| Shaft (ลำไม้หน้า) | Ash Wood | สีอ่อน |
| Tip (ปลาย) | Leather | เขียว-น้ำเงิน |
| Joint (ข้อต่อ) | Metal (Silver) | เงิน |
| Butt (ลำไม้หลัง) | Walnut Wood | สีเข้ม |
| Ring (แหวน) | Brass | ทองเหลือง |
| Bumper (ท้าย) | Rubber | ดำ |

---

## 🏠 Script 4: Props ห้อง + CrowdDummy

### วิธีใช้
1. เปิด Blender → Scripting → วาง `create_room_props_aaa.py`
2. กด **Run Script** ▶
3. FBX ทั้งหมดจะถูกสร้างใน `BlenderScripts/Exports/` (แล้วย้ายเข้า `Assets/CueStrike/Models/AAA_Props/`)

### Props ที่สร้าง (9 อย่าง)
| ไฟล์ | ใช้กับห้อง |
|------|-----------|
| `LuxuryChandelier.fbx` | AAA_RoomDAY, AAA_RoomLuxury |
| `IndustrialLamp.fbx` | AAA_RoomIndustrial |
| `ZenLantern.fbx` | AAA_RoomZenDojo |
| `NeonSign_Strike.fbx` | AAA_RoomCyberpunk |
| `BarBottleSet.fbx` | Luxury, Cyberpunk, Industrial |
| `WarpPortalArch.fbx` | AAA_RoomWarpFantasy |
| `SpaceConsole.fbx` | AAA_RoomSpaceNebula |
| `HoloScreen.fbx` | Luxury, Cyberpunk, SpaceNebula, WarpFantasy |
| `CrowdDummy.fbx` | GrandArena (ผู้ชม) |

---

## 🎮 ขั้นตอนหลังจากได้ FBX/PNG แล้ว

### 1️⃣ ลากเข้า Unity
นำไฟล์ทั้งหมดจาก `BlenderScripts/Exports/` ไปวางใน Unity:
- `.fbx` → วางใน `Assets/CueStrike/Models/AAA_Props/`
- `.png` → วางใน `Assets/CueStrike/Textures/` (จะอยู่ข้าง ball_0.png - ball_15.png)

### 2️⃣ ใช้ MenuItem ใน Unity
ไปที่เมนู `Tools → CueStrike → Apply` แล้วเลือก:
- `Apply All AAA (Blender → Game)` — รันทุกอย่างรวม (แนะนำ)
  - Import FBX + PNG
  - **แปลง FBX materials → URP/Lit (แก้สีชมพูอัตโนมัติ)**
  - สร้าง Material จาก Texture
  - ประกอบ Table/Ball/Cue + Props ลงห้อง
- `Setup → Apply Ball Materials` — เอา FBX ลูกบอลไปใส่ Prefab
- `Apply Table Textures` — เอา Texture ไปใส่ Material โต๊ะ
- `Apply Cue Materials` — เอา FBX ไม้คิวไปใส่ Prefab

### 2.5️⃣ ถ้าเจอสีชมพู
ไปที่เมนู `Tools → CueStrike → Fix → Fix Pink Materials (URP Conversion)`
- แปลง Material ทั้งหมด (รวมที่ฝังใน FBX) ให้เป็น `Universal Render Pipeline/Lit`

### 3️⃣ ทดสอบ
- กด Play → เช็คว่าลูกบอล, โต๊ะ, ไม้คิวมี texture ใหม่
- รัน Self-Test: `Tools → CueStrike → Debug → Test AAA Setup` (ตรวจหาสีชมพูอัตโนมัติ)
- ถ้าไม่ถูกต้อง → กด Undo หรือ Import ใหม่

---

## 🤖 คำแนะนำสำหรับ AI Agent ตัวถัดไป (ทำตามลำดับ)

1. **ตรวจ Render Pipeline ก่อน** — โปรเจกต์นี้ใช้ **URP** (ดู `ProjectSettings/GraphicsSettings.asset`)
2. **ห้ามใช้ Standard Shader** ในโปรเจกต์ URP — มันจะแสดงเป็นสีชมพู
3. **ถ้าต้องสร้าง Material** → กำหนด Shader เป็น `Universal Render Pipeline/Lit` เสมอ
4. **ตรวจ Material Name ใน .fbx** ว่าตรงกับที่ C# Script เรียกใช้หรือไม่
5. **Workflow มาตรฐาน:**
   - ปิด Unity Editor
   - แก้โค้ด C# / Python
   - รัน batchmode compile → 0 errors
   - เปิด Unity → กด `Apply All AAA` → ตรวจสีชมพู → Self-Test

---

## 💡 Tips
- **Script ใช้เวลารันประมาณ 5-10 วินาที** — รอจนเห็นข้อความ "DONE!"
- ถ้า export ไม่ได้ (path error) → สร้างโฟลเดอรี่ `BlenderScripts/Exports/` และ `BlenderScripts/Exports/Textures/` เอง
- ถ้าอยากปรับ texture ความละเอียดสูงขึ้น → เปลี่ยน `TEX_RESOLUTION = 2048` → `4096` ใน script
- Blender 3.6 นี้เหมาะมาก เพราะ Python API เสถียร
- **สีชมพู = Shader ไม่เข้ากับ URP** → กด `Fix Pink Materials` ทันที
- ถ้า `Apply All AAA` ทำใหม่ซ้ำได้เรื่อย ๆ (มี check duplicate แล้ว ไม่วาง Props ซ้ำ)

---

**มีปัญหาตรงไหนบอกนาริได้เลยครับ! 🙏**

*อัปเดตล่าสุด: 2026-08-01 | เพิ่ม Context Brief, Pink Material Fix Guide, Script 4*