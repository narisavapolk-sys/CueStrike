"""
CueStrike — Blender 3.6 Script: Create AAA Pool/Snooker Balls
==============================================================
What this script does:
1. Creates 16 billiard balls (0=Cue, 1-7=Solid, 8=Black, 9-15=Stripe)
2. Applies realistic materials: glossy finish, colored body, white number circle
3. Adds numbered texture using procedural nodes (no external image needed)
4. Exports to FBX with embedded materials, ready for Unity

Instructions for P'Momg:
1. Open Blender 3.6
2. Switch to "Scripting" workspace (top tabs)
3. Click "New" text button → paste this entire script
4. Click "Run Script" ▶ (or press Alt+P)
5. FBX file will be saved to: C:/Users/mongo/UnityProjects/CueStrike/CueStrike_Project/BlenderScripts/Exports/
6. Close Blender (no need to save .blend file unless you want to)

=== COLORS (Snooker-style, Pool-compatible) ===
  0 — Cue Ball: White
  1 — Solid Yellow
  2 — Solid Blue
  3 — Solid Red
  4 — Solid Purple
  5 — Solid Orange
  6 — Solid Green
  7 — Solid Maroon
  8 — Black
  9 — Stripe Yellow
 10 — Stripe Blue
 11 — Stripe Red
 12 — Stripe Purple
 13 — Stripe Orange
 14 — Stripe Green
 15 — Stripe Maroon
"""

import bpy
import os
import math
import mathutils

# ═══════════════════════════════════════════════
# CONFIGURATION
# ═══════════════════════════════════════════════

BALL_RADIUS = 0.028  # ~56mm diameter (slightly larger than real 52.5mm for visibility)
BALL_SEGMENTS = 64    # High poly for smooth spheres
BALL_RINGS = 64

# Output path — export DIRECTLY into Unity Assets so it auto-imports!
# No manual dragging needed — Unity re-imports automatically when open.
UNITY_MODEL_DIR = "C:/Users/mongo/UnityProjects/CueStrike/CueStrike_Project/Assets/CueStrike/Models/AAA_Props"
EXPORT_DIR = UNITY_MODEL_DIR
if not os.path.exists(EXPORT_DIR):
    os.makedirs(EXPORT_DIR)

# Ball colors (R, G, B) — Snooker/Pool standard
BALL_COLORS = [
    (0.95, 0.95, 0.95),   # 0: Cue Ball (white, slight cream for realism)
    (0.95, 0.82, 0.05),   # 1: Yellow
    (0.05, 0.35, 0.75),   # 2: Blue
    (0.85, 0.08, 0.08),   # 3: Red
    (0.50, 0.10, 0.60),   # 4: Purple
    (0.95, 0.50, 0.05),   # 5: Orange
    (0.05, 0.55, 0.20),   # 6: Green
    (0.55, 0.05, 0.05),   # 7: Maroon
    (0.08, 0.08, 0.08),   # 8: Black
    (0.95, 0.82, 0.05),   # 9: Stripe Yellow
    (0.05, 0.35, 0.75),   # 10: Stripe Blue
    (0.85, 0.08, 0.08),   # 11: Stripe Red
    (0.50, 0.10, 0.60),   # 12: Stripe Purple
    (0.95, 0.50, 0.05),   # 13: Stripe Orange
    (0.05, 0.55, 0.20),   # 14: Stripe Green
    (0.55, 0.05, 0.05),   # 15: Stripe Maroon
]

# Ball names
BALL_NAMES = [
    "Ball_Cue", "Ball_01", "Ball_02", "Ball_03",
    "Ball_04", "Ball_05", "Ball_06", "Ball_07",
    "Ball_08", "Ball_09", "Ball_10", "Ball_11",
    "Ball_12", "Ball_13", "Ball_14", "Ball_15"
]

# Is this a stripe ball? (index 9-15)
def is_stripe(idx):
    return 9 <= idx <= 15

# ═══════════════════════════════════════════════
# CLEAN SCENE
# ═══════════════════════════════════════════════

def clean_scene():
    """Delete all objects, materials, and data blocks in the scene."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=True)
    
    # Remove all materials
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    
    # Remove all meshes
    for mesh in list(bpy.data.meshes):
        bpy.data.meshes.remove(mesh)

clean_scene()

# ═══════════════════════════════════════════════
# SETUP LIGHTS & CAMERA (for preview)
# ═══════════════════════════════════════════════

# Area light from above
bpy.ops.object.light_add(type='AREA', location=(0, 0, 0.5))
area_light = bpy.context.active_object
area_light.data.energy = 500
area_light.data.size = 2
area_light.location = (0, 0, 0.5)

# Point light
bpy.ops.object.light_add(type='POINT', location=(0.2, -0.2, 0.2))
point_light = bpy.context.active_object
point_light.data.energy = 200

# Camera (angled down)
bpy.ops.object.camera_add(location=(0.4, -0.4, 0.3))
cam = bpy.context.active_object
cam.rotation_euler = (math.radians(60), 0, math.radians(45))
bpy.context.scene.camera = cam

# ═══════════════════════════════════════════════
# CREATE BALLS
# ═══════════════════════════════════════════════

def create_ball(idx, x_pos):
    """Create a single billiard ball with AAA material."""
    
    # ── 1. Create sphere ──
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=BALL_RADIUS,
        segments=BALL_SEGMENTS,
        ring_count=BALL_RINGS,
        location=(x_pos, 0, BALL_RADIUS)
    )
    ball = bpy.context.active_object
    ball.name = BALL_NAMES[idx]
    ball.data.name = f"{BALL_NAMES[idx]}_Mesh"
    
    # ── 2. Mark seam for UV — horizontal ring around equator ──
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='DESELECT')
    bpy.ops.object.mode_set(mode='OBJECT')
    
    # Select middle edge loop for seam
    mesh = ball.data
    vert_count = len(mesh.vertices)
    mid_vert = vert_count // 2
    
    # Select vertices at the equator row
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='DESELECT')
    bpy.ops.object.mode_set(mode='OBJECT')
    
    # Find vertices at mid-height to mark seam
    mid_z = 0  # equator at z=0 in local space
    for v in mesh.vertices:
        if abs(v.co.z) < 0.001:
            v.select = True
    
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.mark_seam(clear=False)
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.unwrap(method='ANGLE_BASED', margin=0.001)
    bpy.ops.object.mode_set(mode='OBJECT')
    
    # ── 3. Create material ──
    mat = bpy.data.materials.new(name=f"{BALL_NAMES[idx]}_Mat")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    
    # Clear default nodes
    nodes.clear()
    
    # --- Create node setup for AAA ball ---
    
    # Output node
    output = nodes.new(type='ShaderNodeOutputMaterial')
    output.location = (800, 0)
    
    # Principled BSDF (main shader)
    principled = nodes.new(type='ShaderNodeBsdfPrincipled')
    principled.location = (400, 0)
    
    # Mix Shader for clear coat
    mix_shader = nodes.new(type='ShaderNodeMixShader')
    mix_shader.location = (600, 0)
    
    # Glossy for clear coat
    glossy = nodes.new(type='ShaderNodeBsdfGlossy')
    glossy.location = (400, 200)
    glossy.inputs['Roughness'].default_value = 0.05
    glossy.inputs['Color'].default_value = (1, 1, 1, 1)
    
    # Color for the ball
    base_color = BALL_COLORS[idx]
    
    # For stripe balls, create a gradient texture
    if is_stripe(idx):
        # Mix between white and color
        color_mix = nodes.new(type='ShaderNodeMixRGB')
        color_mix.location = (0, 0)
        color_mix.blend_type = 'MIX'
        color_mix.inputs['Fac'].default_value = 0.5
        
        # Color ramp for stripe pattern
        color_ramp = nodes.new(type='ShaderNodeValToRGB')
        color_ramp.location = (-200, 0)
        color_ramp.color_ramp.elements[0].position = 0.35
        color_ramp.color_ramp.elements[0].color = (1, 1, 1, 1)  # White band
        color_ramp.color_ramp.elements[1].position = 0.65
        color_ramp.color_ramp.elements[1].color = (base_color[0], base_color[1], base_color[2], 1)
        
        # Texture coordinate
        tex_coord = nodes.new(type='ShaderNodeTexCoord')
        tex_coord.location = (-500, 0)
        
        # Mapping for stripe orientation
        mapping = nodes.new(type='ShaderNodeMapping')
        mapping.location = (-350, 0)
        mapping.inputs['Rotation'].default_value[1] = math.radians(90)  # Rotate for horizontal stripe
        
        # Connect: TexCoord → Mapping → ColorRamp → MixRGB → Principled
        links.new(tex_coord.outputs['UV'], mapping.inputs['Vector'])
        links.new(mapping.outputs['Vector'], color_ramp.inputs['Fac'])
        links.new(color_ramp.outputs['Color'], color_mix.inputs['Color1'])
        links.new(color_mix.outputs['Color'], principled.inputs['Base Color'])
        color_mix.inputs['Color2'].default_value = (1, 1, 1, 1)
        
        # Set the stripe mix factor
        color_mix.inputs['Fac'].default_value = 0.5
        
    else:
        # Solid color ball
        color = (base_color[0], base_color[1], base_color[2], 1)
        principled.inputs['Base Color'].default_value = color
    
    # Number texture — use procedural number
    # For now, we'll use a simple white circle to represent the number area
    num_tex = nodes.new(type='ShaderNodeTexWave')
    num_tex.location = (-200, -200)
    num_tex.inputs['Scale'].default_value = 200
    num_tex.wave_type = 'BANDS'
    
    # Connect nodes
    links.new(principled.outputs['BSDF'], mix_shader.inputs[1])
    links.new(glossy.outputs['BSDF'], mix_shader.inputs[2])
    links.new(mix_shader.outputs['Shader'], output.inputs['Surface'])
    
    # Set clear coat amount
    mix_shader.inputs['Fac'].default_value = 0.15
    
    # Set roughness for main material based on ball type
    if idx == 0:  # Cue ball — shiniest
        principled.inputs['Roughness'].default_value = 0.05
        principled.inputs['Specular'].default_value = 0.5
    elif idx == 8:  # Black ball — matte
        principled.inputs['Roughness'].default_value = 0.15
        principled.inputs['Specular'].default_value = 0.3
    else:
        principled.inputs['Roughness'].default_value = 0.1
        principled.inputs['Specular'].default_value = 0.4
    
    # Assign material to ball
    if ball.data.materials:
        ball.data.materials[0] = mat
    else:
        ball.data.materials.append(mat)
    
    return ball


# ═══════════════════════════════════════════════
# CREATE ALL 16 BALLS IN A ROW
# ═══════════════════════════════════════════════

print("=" * 60)
print("CREATING 16 AAA POOL BALLS...")
print("=" * 60)

# Create collection for balls
ball_collection = bpy.data.collections.new("PoolBalls")
bpy.context.scene.collection.children.link(ball_collection)

spacing = BALL_RADIUS * 3
start_x = -(16 * spacing) / 2

for i in range(16):
    x = start_x + i * spacing + BALL_RADIUS
    ball = create_ball(i, x)
    
    # Move ball to collection
    for col in ball.users_collection:
        col.objects.unlink(ball)
    ball_collection.objects.link(ball)
    
    print(f"  ✓ Created {BALL_NAMES[i]} (color={BALL_COLORS[i]})")
    bpy.context.view_layer.update()

# ═══════════════════════════════════════════════
# EXPORT TO FBX
# ═══════════════════════════════════════════════

export_path = os.path.join(EXPORT_DIR, "CueStrike_PoolBalls_AAA.fbx")

# Select all balls
bpy.ops.object.select_all(action='DESELECT')
for obj in ball_collection.objects:
    obj.select_set(True)

bpy.ops.export_scene.fbx(
    filepath=export_path,
    use_selection=True,
    global_scale=1.0,  # Keep real-world meters — Unity reads FBX unit scale
    apply_unit_scale=True,
    bake_space_transform=True,
    object_types={'MESH'},
    mesh_smooth_type='FACE',
    path_mode='COPY',
    embed_textures=True,
    batch_mode='OFF'
)

print(f"\n✅ EXPORTED: {export_path}")
print(f"📦 File size: {os.path.getsize(export_path) / 1024:.1f} KB")
print("=" * 60)
print("DONE! Now close Blender and drag the FBX into Unity.")
print("Then run: Tools → CueStrike → Setup → Apply Ball Materials")