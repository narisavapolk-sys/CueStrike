"""
CueStrike — Blender 3.6 Script: Create AAA Cue Stick
======================================================
Creates a realistic pool/snooker cue stick with:
- Ash/Walnut wood grain shaft
- Leather tip (blue/green)
- Butt with decorative ring
- Joint (metal)
- Correct proportions for VR

Instructions for P'Momg:
1. Open Blender 3.6 → Scripting → New → paste → Run
2. FBX will be saved to: BlenderScripts/Exports/CueStrike_Cue_AAA.fbx
3. Drag FBX into Unity
"""

import bpy
import os
import math

# ═══════════════════════════════════════════════
# CONFIGURATION
# ═══════════════════════════════════════════════

# Output path — export DIRECTLY into Unity Assets so it auto-imports!
# No manual dragging needed — Unity re-imports automatically when open.
UNITY_MODEL_DIR = "C:/Users/mongo/UnityProjects/CueStrike/CueStrike_Project/Assets/CueStrike/Models/AAA_Props"
EXPORT_DIR = UNITY_MODEL_DIR
if not os.path.exists(EXPORT_DIR):
    os.makedirs(EXPORT_DIR)

# Cue proportions (in meters) — standard 57" cue
CUE_LENGTH = 1.47       # ~57 inches
TIP_RADIUS = 0.006      # ~12mm tip
BUTT_RADIUS = 0.015     # ~30mm butt
JOINT_RADIUS = 0.012    # ~24mm joint

SEGMENTS = 32           # Smooth enough for display

# ═══════════════════════════════════════════════
# CLEAN SCENE
# ═══════════════════════════════════════════════

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=True)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)
for mesh in list(bpy.data.meshes):
    bpy.data.meshes.remove(mesh)

print("=" * 60)
print("CREATING AAA CUE STICK...")
print("=" * 60)

# ═══════════════════════════════════════════════
# HELPER: Create tapered cylinder
# ═══════════════════════════════════════════════

def create_tapered_cylinder(name, length, start_radius, end_radius, z_offset=0, segments=SEGMENTS):
    """Create a tapered cylinder along Z axis."""
    
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=segments,
        radius=start_radius,
        depth=length,
        location=(0, 0, z_offset + length / 2)
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.data.name = f"{name}_Mesh"
    
    # Taper the cylinder
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='DESELECT')
    
    # Select top face loop and scale
    mesh = obj.data
    bpy.ops.object.mode_set(mode='OBJECT')
    
    # Scale the top vertices to end_radius/start_radius ratio
    scale_ratio = end_radius / start_radius
    
    # Select top half vertices
    for v in mesh.vertices:
        if v.co.z > length * 0.4:  # Top portion
            v.select = True
    
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.transform.resize(value=(scale_ratio, scale_ratio, 1))
    bpy.ops.object.mode_set(mode='OBJECT')
    
    return obj

# ═══════════════════════════════════════════════
# 1. SHAFT (Ash wood — long tapered section)
# ═══════════════════════════════════════════════

SHAFT_LENGTH = 0.75
shaft = create_tapered_cylinder(
    "Cue_Shaft",
    SHAFT_LENGTH,
    TIP_RADIUS,
    JOINT_RADIUS,
    z_offset=0
)

# Shaft material — light ash wood
shaft_mat = bpy.data.materials.new(name="Cue_Shaft_Material")
shaft_mat.use_nodes = True
nodes = shaft_mat.node_tree.nodes
links = shaft_mat.node_tree.links
nodes.clear()

output = nodes.new(type='ShaderNodeOutputMaterial')
output.location = (800, 0)

principled = nodes.new(type='ShaderNodeBsdfPrincipled')
principled.location = (500, 0)
principled.inputs['Base Color'].default_value = (0.72, 0.58, 0.42, 1.0)  # Ash wood
principled.inputs['Roughness'].default_value = 0.3
principled.inputs['Specular'].default_value = 0.4

# Wood grain texture
tex_coord = nodes.new(type='ShaderNodeTexCoord')
tex_coord.location = (-400, 0)

mapping = nodes.new(type='ShaderNodeMapping')
mapping.location = (-250, 0)
mapping.inputs['Scale'].default_value[2] = 30  # Stretch grain along shaft

wave_tex = nodes.new(type='ShaderNodeTexWave')
wave_tex.location = (-100, 0)
wave_tex.wave_type = 'BANDS'
wave_tex.inputs['Scale'].default_value = 15
wave_tex.inputs['Distortion'].default_value = 2

# Wood grain color variation
color_ramp = nodes.new(type='ShaderNodeValToRGB')
color_ramp.location = (-100, -150)
color_ramp.color_ramp.elements[0].position = 0.3
color_ramp.color_ramp.elements[0].color = (0.65, 0.50, 0.35, 1.0)
color_ramp.color_ramp.elements[1].position = 0.7
color_ramp.color_ramp.elements[1].color = (0.78, 0.62, 0.45, 1.0)

mix_rgb = nodes.new(type='ShaderNodeMixRGB')
mix_rgb.location = (200, -100)
mix_rgb.blend_type = 'MULTIPLY'
mix_rgb.inputs['Fac'].default_value = 0.3

links.new(tex_coord.outputs['UV'], mapping.inputs['Vector'])
links.new(mapping.outputs['Vector'], wave_tex.inputs['Vector'])
links.new(wave_tex.outputs['Fac'], color_ramp.inputs['Fac'])
links.new(color_ramp.outputs['Color'], mix_rgb.inputs['Color1'])
mix_rgb.inputs['Color2'].default_value = (0.72, 0.58, 0.42, 1.0)
links.new(mix_rgb.outputs['Color'], principled.inputs['Base Color'])
links.new(principled.outputs['BSDF'], output.inputs['Surface'])

shaft.data.materials.append(shaft_mat)

print("  ✓ Created Cue_Shaft (Ash wood)")

# ═══════════════════════════════════════════════
# 2. TIP (Leather — blue/green)
# ═══════════════════════════════════════════════

TIP_LENGTH = 0.012  # 12mm
tip = create_tapered_cylinder(
    "Cue_Tip",
    TIP_LENGTH,
    TIP_RADIUS * 1.1,
    TIP_RADIUS * 0.8,
    z_offset=SHAFT_LENGTH
)

tip_mat = bpy.data.materials.new(name="Cue_Tip_Material")
tip_mat.use_nodes = True
nodes = tip_mat.node_tree.nodes
nodes.clear()

output = nodes.new(type='ShaderNodeOutputMaterial')
output.location = (300, 0)
principled = nodes.new(type='ShaderNodeBsdfPrincipled')
principled.location = (100, 0)
principled.inputs['Base Color'].default_value = (0.20, 0.45, 0.30, 1.0)  # Green-blue leather
principled.inputs['Roughness'].default_value = 0.8
principled.inputs['Specular'].default_value = 0.1
links.new(principled.outputs['BSDF'], output.inputs['Surface'])

tip.data.materials.append(tip_mat)
print("  ✓ Created Cue_Tip (leather)")

# ═══════════════════════════════════════════════
# 3. BUTT (Walnut — thick end)
# ═══════════════════════════════════════════════

BUTT_LENGTH = 0.70
butt = create_tapered_cylinder(
    "Cue_Butt",
    BUTT_LENGTH,
    JOINT_RADIUS,
    BUTT_RADIUS,
    z_offset=SHAFT_LENGTH + TIP_LENGTH
)

butt_mat = bpy.data.materials.new(name="Cue_Butt_Material")
butt_mat.use_nodes = True
nodes = butt_mat.node_tree.nodes
links = butt_mat.node_tree.links
nodes.clear()

output = nodes.new(type='ShaderNodeOutputMaterial')
output.location = (800, 0)

principled = nodes.new(type='ShaderNodeBsdfPrincipled')
principled.location = (500, 0)
principled.inputs['Base Color'].default_value = (0.30, 0.18, 0.10, 1.0)  # Walnut
principled.inputs['Roughness'].default_value = 0.2  # Polished
principled.inputs['Specular'].default_value = 0.5

# Dark wood grain
tex_coord = nodes.new(type='ShaderNodeTexCoord')
tex_coord.location = (-400, 0)

mapping = nodes.new(type='ShaderNodeMapping')
mapping.location = (-250, 0)
mapping.inputs['Scale'].default_value[2] = 20

wave_tex = nodes.new(type='ShaderNodeTexWave')
wave_tex.location = (-100, 0)
wave_tex.wave_type = 'BANDS'
wave_tex.inputs['Scale'].default_value = 10
wave_tex.inputs['Distortion'].default_value = 3

color_ramp = nodes.new(type='ShaderNodeValToRGB')
color_ramp.location = (-100, -150)
color_ramp.color_ramp.elements[0].position = 0.4
color_ramp.color_ramp.elements[0].color = (0.25, 0.14, 0.07, 1.0)
color_ramp.color_ramp.elements[1].position = 0.6
color_ramp.color_ramp.elements[1].color = (0.35, 0.22, 0.13, 1.0)

mix_rgb = nodes.new(type='ShaderNodeMixRGB')
mix_rgb.location = (200, -100)
mix_rgb.blend_type = 'MULTIPLY'
mix_rgb.inputs['Fac'].default_value = 0.4

links.new(tex_coord.outputs['UV'], mapping.inputs['Vector'])
links.new(mapping.outputs['Vector'], wave_tex.inputs['Vector'])
links.new(wave_tex.outputs['Fac'], color_ramp.inputs['Fac'])
links.new(color_ramp.outputs['Color'], mix_rgb.inputs['Color1'])
mix_rgb.inputs['Color2'].default_value = (0.30, 0.18, 0.10, 1.0)
links.new(mix_rgb.outputs['Color'], principled.inputs['Base Color'])
links.new(principled.outputs['BSDF'], output.inputs['Surface'])

butt.data.materials.append(butt_mat)
print("  ✓ Created Cue_Butt (Walnut wood)")

# ═══════════════════════════════════════════════
# 4. JOINT (Metal ring)
# ═══════════════════════════════════════════════

JOINT_LENGTH = 0.02  # 2cm
joint = create_tapered_cylinder(
    "Cue_Joint",
    JOINT_LENGTH,
    JOINT_RADIUS * 1.05,
    JOINT_RADIUS * 1.05,
    z_offset=SHAFT_LENGTH + TIP_LENGTH
)

joint_mat = bpy.data.materials.new(name="Cue_Joint_Material")
joint_mat.use_nodes = True
nodes = joint_mat.node_tree.nodes
nodes.clear()

output = nodes.new(type='ShaderNodeOutputMaterial')
output.location = (300, 0)
principled = nodes.new(type='ShaderNodeBsdfPrincipled')
principled.location = (100, 0)
principled.inputs['Base Color'].default_value = (0.7, 0.7, 0.75, 1.0)  # Silver metal
principled.inputs['Roughness'].default_value = 0.2
principled.inputs['Specular'].default_value = 1.0
principled.inputs['Metallic'].default_value = 1.0
links.new(principled.outputs['BSDF'], output.inputs['Surface'])

joint.data.materials.append(joint_mat)
print("  ✓ Created Cue_Joint (metal)")

# ═══════════════════════════════════════════════
# 5. DECORATIVE RING (on butt)
# ═══════════════════════════════════════════════

RING_LENGTH = 0.008
ring = create_tapered_cylinder(
    "Cue_Ring",
    RING_LENGTH,
    BUTT_RADIUS * 1.15,
    BUTT_RADIUS * 1.15,
    z_offset=SHAFT_LENGTH + TIP_LENGTH + BUTT_LENGTH * 0.3
)

ring_mat = bpy.data.materials.new(name="Cue_Ring_Material")
ring_mat.use_nodes = True
nodes = ring_mat.node_tree.nodes
nodes.clear()

output = nodes.new(type='ShaderNodeOutputMaterial')
output.location = (300, 0)
principled = nodes.new(type='ShaderNodeBsdfPrincipled')
principled.location = (100, 0)
principled.inputs['Base Color'].default_value = (0.85, 0.75, 0.55, 1.0)  # Brass
principled.inputs['Roughness'].default_value = 0.3
principled.inputs['Metallic'].default_value = 0.8
principled.inputs['Specular'].default_value = 0.7
links.new(principled.outputs['BSDF'], output.inputs['Surface'])

ring.data.materials.append(ring_mat)
print("  ✓ Created Cue_Ring (brass decorative)")

# ═══════════════════════════════════════════════
# 6. BUMPER (rubber at the butt end)
# ═══════════════════════════════════════════════

BUMPER_LENGTH = 0.015
bumper = create_tapered_cylinder(
    "Cue_Bumper",
    BUMPER_LENGTH,
    BUTT_RADIUS * 1.05,
    BUTT_RADIUS * 0.9,
    z_offset=SHAFT_LENGTH + TIP_LENGTH + BUTT_LENGTH
)

bumper_mat = bpy.data.materials.new(name="Cue_Bumper_Material")
bumper_mat.use_nodes = True
nodes = bumper_mat.node_tree.nodes
nodes.clear()

output = nodes.new(type='ShaderNodeOutputMaterial')
output.location = (300, 0)
principled = nodes.new(type='ShaderNodeBsdfPrincipled')
principled.location = (100, 0)
principled.inputs['Base Color'].default_value = (0.08, 0.08, 0.10, 1.0)  # Dark rubber
principled.inputs['Roughness'].default_value = 0.9
principled.inputs['Specular'].default_value = 0.1
links.new(principled.outputs['BSDF'], output.inputs['Surface'])

bumper.data.materials.append(bumper_mat)
print("  ✓ Created Cue_Bumper (rubber)")

# ═══════════════════════════════════════════════
# COMBINE INTO SINGLE OBJECT
# ═══════════════════════════════════════════════

# Select all cue parts
bpy.ops.object.select_all(action='DESELECT')
cue_parts = [shaft, tip, butt, joint, ring, bumper]
for part in cue_parts:
    part.select_set(True)

bpy.context.view_layer.objects.active = shaft
bpy.ops.object.join()
combined_cue = bpy.context.active_object
combined_cue.name = "CueStrike_Cue"
combined_cue.data.name = "CueStrike_Cue_Mesh"

print("  ✓ Combined all parts into CueStrike_Cue")

# ═══════════════════════════════════════════════
# EXPORT TO FBX
# ═══════════════════════════════════════════════

export_path = os.path.join(EXPORT_DIR, "CueStrike_Cue_AAA.fbx")

bpy.ops.object.select_all(action='DESELECT')
combined_cue.select_set(True)
bpy.context.view_layer.objects.active = combined_cue

bpy.ops.export_scene.fbx(
    filepath=export_path,
    use_selection=True,
    global_scale=1.0,
    apply_unit_scale=True,
    bake_space_transform=True,
    object_types={'MESH'},
    mesh_smooth_type='FACE',
    path_mode='COPY',
    embed_textures=True
)

print(f"\n✅ EXPORTED: {export_path}")
print(f"📦 File size: {os.path.getsize(export_path) / 1024:.1f} KB")
print("=" * 60)
print("DONE! Drag FBX into Unity → assign to Cue Prefab")
print("Then run: Tools → CueStrike → Setup → Apply Cue Materials")