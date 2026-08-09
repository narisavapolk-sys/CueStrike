# 🎯 CueStrike — ความคืบหน้า Blender Assets

อัปเดตล่าสุด: 04/08/2026 22:30

## 🔴 MCP MANDATORY POLICY (บังคับใช้งาน)
> **ทุกการแก้ไขโค้ด/แอสเซต/ซีน ต้องทำผ่าน MCP เท่านั้น**
> - ห้าม Edit Code/Script/Scene/Asset ด้วยมือโดยตรง
> - ใช้ MCP Tools: `execute_code`, `read_file`, `write_file`, `list_files`, `search_files`
> - ผ่าน HTTP API: `POST http://localhost:8080/mcp` (JSON-RPC 2.0)
> - เปิดหน้าต่าง MCP: `CueStrike > MCP Server` (เริ่มเซิร์ฟเวอร์ที่พอร์ต 8080)

## ✅ เสร็จแล้ว
- [x] สร้างสคริปต์ Blender ครบ 5 ไฟล์ (`BlenderScripts/`)
- [x] รันสคริปต์จริงผ่าน Blender 3.6 Command Line สำเร็จ
- [x] แก้บั๊ก Emission (Blender 3.6 ใช้ `Emission` ไม่ใช่ `Emission Color`)
- [x] **MCP HTTP Server + 5 Core Tools สร้างเสร็จแล้ว** (`Assets/CueStrike/Editor/MCP/`)

## 📦 Assets ที่ถูกสร้างเข้า Unity แล้ว
| หมวด | ไฟล์ | สถานะ |
|------|------|--------|
| ลูกบอล 16 ลูก | `Models/AAA_Props/CueStrike_PoolBalls_AAA.fbx` | ✅ |
| ไม้คิว | `Models/AAA_Props/CueStrike_Cue_AAA.fbx` | ✅ |
| โคมระย้า | `Models/AAA_Props/LuxuryChandelier.fbx` | ✅ |
| โคมอุตสาหกรรม | `Models/AAA_Props/IndustrialLamp.fbx` | ✅ |
| โคมเซน | `Models/AAA_Props/ZenLantern.fbx` | ✅ |
| ป้ายนีออน | `Models/AAA_Props/NeonSign_Strike.fbx` | ✅ |
| ขวดบาร์ | `Models/AAA_Props/BarBottleSet.fbx` | ✅ |
| ประตูมิติ | `Models/AAA_Props/WarpPortalArch.fbx` | ✅ |
| คอนโซลอวกาศ | `Models/AAA_Props/SpaceConsole.fbx` | ✅ |
| จอ Holo | `Models/AAA_Props/HoloScreen.fbx` | ✅ |
| ตัวละครผู้ชม | `Models/AAA_Props/CrowdDummy.fbx` | ✅ |
| Texture โต๊ะ 9 แบบ | `Textures/Felt_*, Wood_*, Cushion_*, Pocket_*, Diamond_*` | ✅ |

## 🧍 ตัวละคร (สร้างครบ 9 + Somchay เดิม = 10 ตัว) ✅
สร้างโดย `BlenderScripts/create_all_characters_aaa.py` (Blender 3.6 headless) — FBX ถูก export เข้า `Models/` ให้แล้ว

| ตัวละคร | ธีม | FBX | สถานะ |
|---------|-----|-----|--------|
| Somchay | (เดิมมีอยู่) | `Models/Somchay_AAA.fbx` | ✅ |
| MeiLing | ดอกซากุระ/ดอกไม้ | `Models/MeiLing_AAA.fbx` | ✅ |
| Gentleman | ชุดสูท + หมวกทรงสูง + โมโนเคิล | `Models/Gentleman_AAA.fbx` | ✅ |
| PanPan | แพนด้า + ไผ่ | `Models/PanPan_AAA.fbx` | ✅ |
| Finn | นักดำน้ำ + ถังออกซิเจน/ครีบ | `Models/Finn_AAA.fbx` | ✅ |
| KingFlex | นักกล้าม + มงกุฎ + สร้อยทอง | `Models/KingFlex_AAA.fbx` | ✅ |
| Tusker | ช้าง + เฟซ + งา | `Models/Tusker_AAA.fbx` | ✅ |
| Phantom | ผี + พลังสเปกตรัมเรืองแสง | `Models/Phantom_AAA.fbx` | ✅ |
| Cassidy | คาวบอย + ปืน + เข็มขัดหนัง | `Models/Cassidy_AAA.fbx` | ✅ |
| Bones | โครงกระดูก + ซี่โครงเรืองแสง | `Models/Bones_AAA.fbx` | ✅ |

> หมายเหตุ: ทุกตัวมาพร้อม Rigify rig + พื้นผิว Albedo/Normal/Roughness + คัดลอก FBX เข้า `Assets/CueStrike/Models/` อัตโนมัติ

## 📸 ขอรูปตัวอย่างห้องจริงในเกมส์
- [ ] สร้าง Editor script ถ่าย Screenshot 8 ห้อง
- [ ] รัน Unity Batch Mode ถ่ายรูป
- [ ] ส่งรูปให้พี่ดู

## 🏠 ห้อง 8 แบบในเกมส์ (Scene มีอยู่แล้ว)
AAA DAY, Cyberpunk, GrandArena, Industrial, Luxury, SpaceNebula, WarpFantasy, ZenDojo

## ⏭️ ขั้นต่อไป
1. รัน `Tools → CueStrike → Apply → Apply All AAA` ใน Unity
2. ถ่ายรูปตัวอย่างห้องจากในเกมส์จริง
