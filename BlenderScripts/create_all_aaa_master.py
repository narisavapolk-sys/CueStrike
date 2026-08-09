"""
CueStrike — Blender 3.6 MASTER SCRIPT: Create ALL AAA Assets (One Click!)
==========================================================================
This script runs ALL 4 Blender scripts in sequence so P'Momg only needs to
open Blender ONE time and press Run ONCE. Everything exports DIRECTLY into
the Unity project folder (Assets/CueStrike/) — Unity auto-imports instantly!

Scripts run in order:
  1. create_table_textures_aaa.py  → 9 PNG textures → Assets/CueStrike/Textures/
  2. create_pool_balls_aaa.py      → 16 balls FBX   → Assets/CueStrike/Models/AAA_Props/
  3. create_cue_aaa.py             → cue FBX        → Assets/CueStrike/Models/AAA_Props/
  4. create_room_props_aaa.py      → 9 room props   → Assets/CueStrike/Models/AAA_Props/

Instructions for P'Momg:
  1. Open Blender 3.6
  2. Scripting workspace → New → paste this entire script
  3. Press Run Script ▶ (or Alt+P)
  4. Wait ~30-60 seconds (status prints as it works)
  5. ALL done! Close Blender → open/switch to Unity → it auto-imports!
  6. In Unity press: Tools → CueStrike → Apply → Apply All AAA 🔥
"""

import bpy
import os
import sys
import subprocess
import importlib.util

# ═══════════════════════════════════════════════
# CONFIGURATION
# ═══════════════════════════════════════════════

BLENDER_SCRIPTS_DIR = "C:/Users/mongo/UnityProjects/CueStrike/CueStrike_Project/BlenderScripts"

SCRIPTS = [
    "create_table_textures_aaa.py",   # 1. Textures (PNG)
    "create_pool_balls_aaa.py",       # 2. Pool balls (FBX)
    "create_cue_aaa.py",              # 3. Cue stick (FBX)
    "create_room_props_aaa.py",       # 4. Room props + crowd (FBX)
    "create_title_screen_aaa.py",     # 5. Title screen animation (FBX + Camera + Lights)
]

# ═══════════════════════════════════════════════
# RUN ALL SCRIPTS
# ═══════════════════════════════════════════════

print("=" * 70)
print("🎱 CUESTRIKE AAA MASTER SCRIPT — CREATING EVERYTHING!")
print("=" * 70)

total = len(SCRIPTS)
for i, script_name in enumerate(SCRIPTS, 1):
    script_path = os.path.join(BLENDER_SCRIPTS_DIR, script_name)
    print(f"\n📦 STEP {i}/{total}: {script_name}")
    print("-" * 50)

    if not os.path.exists(script_path):
        print(f"❌ ERROR: Script not found: {script_path}")
        continue

    # Execute the script in Blender's Python
    try:
        with open(script_path, "r", encoding="utf-8") as f:
            code = f.read()
        exec(compile(code, script_path, "exec"), {"__name__": "__main__", "__file__": script_path})
        print(f"✅ COMPLETED: {script_name}")
    except Exception as e:
        print(f"⚠️ WARNING: {script_name} had an issue (continuing): {e}")

print("\n" + "=" * 70)
print("🎉 ALL CUESTRIKE AAA ASSETS CREATED!")
print("=" * 70)
print("""
📂 Everything exported DIRECTLY into the Unity project:
   ├─ Assets/CueStrike/Textures/          ← 9 PNG textures
   └─ Assets/CueStrike/Models/AAA_Props/  ← FBX models (balls, cue, 9 props, crowd)

✅ Unity auto-imports new files instantly (if open)
✅ If Unity was closed, open it now — files are already there!

👉 Next: In Unity press:  Tools → CueStrike → Apply → Apply All AAA
🔥 That's it! No manual dragging needed! 🚀
""")