"""
CueStrike — Blender 3.6 Script: Create AAA Table Textures
===========================================================
Creates realistic procedural textures for the pool/snooker table:
1. Baize (felt cloth) — green/navy with micro-fur normal
2. Cushion rubber — dark green matte
3. Wood frame — wood grain with varnish gloss
4. Pocket leather — dark brown
5. Diamond markers — ivory/pearl

These are exported as tiled textures (PNG) that can be applied
to the existing Table Prefab in Unity.

Instructions for P'Momg:
1. Open Blender 3.6
2. Switch to Scripting workspace
3. Click "New" text button → paste this entire script
4. Click "Run Script" ▶ (or press Alt+P)
5. Textures will be saved to: BlenderScripts/Exports/Textures/
6. Drag PNG files into Unity → They're ready for Materials
"""

import bpy
import os
import math

# ═══════════════════════════════════════════════
# CONFIGURATION
# ═══════════════════════════════════════════════

# Output path — export DIRECTLY into Unity Assets so it auto-imports!
# No manual dragging needed — Unity re-imports automatically when open.
UNITY_TEXTURE_DIR = "C:/Users/mongo/UnityProjects/CueStrike/CueStrike_Project/Assets/CueStrike/Textures"
EXPORT_DIR = UNITY_TEXTURE_DIR
if not os.path.exists(EXPORT_DIR):
    os.makedirs(EXPORT_DIR)

TEX_RESOLUTION = 2048  # 2K textures (good balance quality/performance)

print("=" * 60)
print("CREATING AAA TABLE TEXTURES...")
print("=" * 60)

# ═══════════════════════════════════════════════
# 1. BAIZE (FELT CLOTH) — Two variants
# ═══════════════════════════════════════════════

def create_felt_texture(name, base_color, roughness, variant="snooker"):
    """Create a felt cloth texture with micro-fuzz normal map."""
    
    # Create new image
    img = bpy.data.images.new(name, TEX_RESOLUTION, TEX_RESOLUTION, alpha=True, float_buffer=False)
    
    # Generate pixels with procedural felt pattern
    pixels = []
    import random
    random.seed(42)
    
    for y in range(TEX_RESOLUTION):
        for x in range(TEX_RESOLUTION):
            # Base color
            r, g, b = base_color
            
            # Micro-fuzz noise (subtle variation)
            noise = (random.random() - 0.5) * 0.03
            
            # Diagonal weave pattern (very subtle)
            weave = math.sin(x * 0.05) * 0.005 + math.cos(y * 0.05) * 0.005
            
            r_final = max(0, min(1, r + noise + weave))
            g_final = max(0, min(1, g + noise + weave))
            b_final = max(0, min(1, b + noise + weave))
            
            pixels.extend([r_final, g_final, b_final, 1.0])
    
    img.pixels = pixels
    img.pack()
    img.filepath_raw = os.path.join(EXPORT_DIR, f"{name}.png")
    img.file_format = 'PNG'
    img.save()
    print(f"   ✓ Created {name}.png ({TEX_RESOLUTION}x{TEX_RESOLUTION})")
    return img


def create_felt_normal_map(name):
    """Create a normal map for felt cloth fuzz."""
    
    img = bpy.data.images.new(f"{name}_Normal", TEX_RESOLUTION, TEX_RESOLUTION, alpha=True)
    
    pixels = []
    import random
    random.seed(99)
    
    for y in range(TEX_RESOLUTION):
        for x in range(TEX_RESOLUTION):
            # Normal map: mostly flat (128,128,255) with tiny perturbations
            nx = (random.random() - 0.5) * 0.08 + 0.5
            ny = (random.random() - 0.5) * 0.08 + 0.5
            nz = 0.5 + (1.0 - abs(nx - 0.5) * 2) * 0.5
            
            # Ensure it's a valid normal
            length = math.sqrt((nx-0.5)**2 + (ny-0.5)**2 + (nz-0.5)**2)
            if length > 0:
                nx = (nx - 0.5) / length * 0.5 + 0.5
                ny = (ny - 0.5) / length * 0.5 + 0.5
                nz = (nz - 0.5) / length * 0.5 + 0.5
            
            pixels.extend([nx, ny, nz, 1.0])
    
    img.pixels = pixels
    img.pack()
    img.filepath_raw = os.path.join(EXPORT_DIR, f"{name}_Normal.png")
    img.file_format = 'PNG'
    img.save()
    print(f"   ✓ Created {name}_Normal.png (normal map)")
    return img


# Create Snooker felt (green)
create_felt_texture(
    "Felt_Snooker_Green",
    base_color=(0.18, 0.42, 0.15),  # Classic snooker green
    roughness=0.9,
    variant="snooker"
)

# Create Pool felt (blue) — for 8-ball/9-ball
create_felt_texture(
    "Felt_Pool_Blue",
    base_color=(0.12, 0.25, 0.55),  # Tournament blue
    roughness=0.9,
    variant="pool"
)

# Create normal maps for both
create_felt_normal_map("Felt_Snooker_Green")
create_felt_normal_map("Felt_Pool_Blue")

# ═══════════════════════════════════════════════
# 2. CUSHION RUBBER
# ═══════════════════════════════════════════════

def create_cushion_texture(name, base_color):
    """Create rubber cushion texture with subtle dimple pattern."""
    
    img = bpy.data.images.new(name, TEX_RESOLUTION, TEX_RESOLUTION, alpha=True)
    
    pixels = []
    import random
    random.seed(777)
    
    for y in range(TEX_RESOLUTION):
        for x in range(TEX_RESOLUTION):
            r, g, b = base_color
            
            # Rubber dimple pattern
            dimple = math.sin(x * 0.1) * math.cos(y * 0.1) * 0.02
            
            # Noise for rubber texture
            noise = (random.random() - 0.5) * 0.04
            
            r_final = max(0, min(1, r + noise + dimple))
            g_final = max(0, min(1, g + noise + dimple))
            b_final = max(0, min(1, b + noise + dimple))
            
            pixels.extend([r_final, g_final, b_final, 1.0])
    
    img.pixels = pixels
    img.pack()
    img.filepath_raw = os.path.join(EXPORT_DIR, f"{name}.png")
    img.file_format = 'PNG'
    img.save()
    print(f"   ✓ Created {name}.png")
    return img

create_cushion_texture("Cushion_Rubber", (0.08, 0.20, 0.10))  # Dark green rubber

# ═══════════════════════════════════════════════
# 3. WOOD GRAIN (Table Frame)
# ═══════════════════════════════════════════════

def create_wood_texture(name, base_color, grain_scale=1.0):
    """Create realistic wood grain texture."""
    
    img = bpy.data.images.new(name, TEX_RESOLUTION, TEX_RESOLUTION, alpha=True)
    
    pixels = []
    import random
    random.seed(12345)
    
    for y in range(TEX_RESOLUTION):
        for x in range(TEX_RESOLUTION):
            r, g, b = base_color
            
            # Wood grain: concentric rings with variation
            nx = x / TEX_RESOLUTION * grain_scale
            ny = y / TEX_RESOLUTION * grain_scale
            
            # Multiple sine waves for complex grain
            grain1 = math.sin(nx * 50 + math.sin(ny * 30) * 2) * 0.08
            grain2 = math.sin(nx * 80 + math.sin(ny * 20) * 1.5) * 0.04
            grain3 = math.cos(ny * 40 + math.sin(nx * 25) * 1.8) * 0.03
            
            # Knot (dark spot)
            dist_to_knot = math.sqrt((nx - 0.3)**2 + (ny - 0.5)**2)
            knot = max(0, 0.06 * math.exp(-dist_to_knot * 20))
            
            # Noise for pore structure
            pore_noise = (random.random() - 0.5) * 0.02
            
            # Vignette (darker edges)
            edge = 1.0 - abs(nx - 0.5) * 0.3 - abs(ny - 0.5) * 0.3
            
            grain_total = grain1 + grain2 + grain3 + knot + pore_noise
            
            r_final = max(0, min(1, r * edge + grain_total * 0.5))
            g_final = max(0, min(1, g * edge + grain_total * 0.3))
            b_final = max(0, min(1, b * edge + grain_total * 0.2))
            
            pixels.extend([r_final, g_final, b_final, 1.0])
    
    img.pixels = pixels
    img.pack()
    img.filepath_raw = os.path.join(EXPORT_DIR, f"{name}.png")
    img.file_format = 'PNG'
    img.save()
    print(f"   ✓ Created {name}.png")
    return img

create_wood_texture("Wood_Dark_Walnut", (0.28, 0.15, 0.08), grain_scale=2.0)
create_wood_texture("Wood_Light_Oak", (0.50, 0.32, 0.18), grain_scale=1.5)

# ═══════════════════════════════════════════════
# 4. POCKET LEATHER
# ═══════════════════════════════════════════════

def create_leather_texture(name, base_color):
    """Create leather texture for pockets."""
    
    img = bpy.data.images.new(name, TEX_RESOLUTION, TEX_RESOLUTION, alpha=True)
    
    pixels = []
    import random
    random.seed(5555)
    
    for y in range(TEX_RESOLUTION):
        for x in range(TEX_RESOLUTION):
            r, g, b = base_color
            
            # Leather grain
            grain_x = math.sin(x * 0.2 + math.sin(y * 0.15) * 5) * 0.015
            grain_y = math.cos(y * 0.2 + math.cos(x * 0.15) * 5) * 0.015
            
            # Random pores
            pore = 0
            if random.random() < 0.01:
                pore = -0.05
            
            noise = (random.random() - 0.5) * 0.03
            
            r_final = max(0, min(1, r + grain_x + grain_y + pore + noise))
            g_final = max(0, min(1, g + grain_x + grain_y + pore + noise))
            b_final = max(0, min(1, b + grain_x + grain_y + pore + noise))
            
            pixels.extend([r_final, g_final, b_final, 1.0])
    
    img.pixels = pixels
    img.pack()
    img.filepath_raw = os.path.join(EXPORT_DIR, f"{name}.png")
    img.file_format = 'PNG'
    img.save()
    print(f"   ✓ Created {name}.png")
    return img

create_leather_texture("Pocket_Leather", (0.15, 0.10, 0.08))  # Dark brown

# ═══════════════════════════════════════════════
# 5. DIAMOND MARKERS (Ivory/White)
# ═══════════════════════════════════════════════

def create_diamond_texture(name, base_color):
    """Create ivory/pearl diamond marker texture."""
    
    img = bpy.data.images.new(name, TEX_RESOLUTION, TEX_RESOLUTION, alpha=True)
    
    pixels = []
    import random
    random.seed(8888)
    
    for y in range(TEX_RESOLUTION):
        for x in range(TEX_RESOLUTION):
            r, g, b = base_color
            
            # Mother of pearl shimmer
            shimmer = math.sin(x * 0.05 + y * 0.05) * 0.02 + math.cos(x * 0.03 - y * 0.07) * 0.01
            
            # Subtle noise
            noise = (random.random() - 0.5) * 0.01
            
            r_final = max(0, min(1, r + shimmer + noise))
            g_final = max(0, min(1, g + shimmer * 0.5 + noise))
            b_final = max(0, min(1, b + shimmer * 0.3 + noise))
            
            pixels.extend([r_final, g_final, b_final, 1.0])
    
    img.pixels = pixels
    img.pack()
    img.filepath_raw = os.path.join(EXPORT_DIR, f"{name}.png")
    img.file_format = 'PNG'
    img.save()
    print(f"   ✓ Created {name}.png")
    return img

create_diamond_texture("Diamond_Marker_Ivory", (0.92, 0.90, 0.85))

# ═══════════════════════════════════════════════
# SUMMARY
# ═══════════════════════════════════════════════

print(f"\n{'=' * 60}")
print(f"✅ ALL TEXTURES EXPORTED TO: {EXPORT_DIR}")
print(f"{'=' * 60}")
print("""
📂 Files created:
   ┌─ Felt_Snooker_Green.png        ← Snooker table cloth
   ├─ Felt_Snooker_Green_Normal.png  ← Felt bump map
   ├─ Felt_Pool_Blue.png             ← Pool/8-ball cloth
   ├─ Felt_Pool_Blue_Normal.png      ← Felt bump map
   ├─ Cushion_Rubber.png             ← Rail cushion
   ├─ Wood_Dark_Walnut.png           ← Dark wood frame
   ├─ Wood_Light_Oak.png             ← Light wood frame
   ├─ Pocket_Leather.png             ← Pocket nets/leather
   └─ Diamond_Marker_Ivory.png       ← Table markers

👉 Next: Drag these PNGs into Unity → assign to Materials
👉 Then Tools → CueStrike → Setup → Apply Table Textures
""")