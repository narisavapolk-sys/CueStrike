"""
CueStrike VR Title Screen Animation — Blender 3.6 Python Script
สร้าง Title Screen "CUE STRIKE" แบบ Cinematic สำหรับ VR Billiards
Author: AI Dev Assistant | Version: 1.0 | Blender: 3.6+

Usage:
    blender --background --python create_title_screen.py -- --output "Assets/CueStrike/Models/TitleScreen/TitleScreen.fbx"
"""

import bpy
import bmesh
import math
import mathutils
import random
import sys
import os
import json
from pathlib import Path

# ============================================================================
# CONFIGURATION
# ============================================================================

CONFIG = {
    "project_name": "CUE STRIKE",
    "subtitle": "VR Billiards",
    "output_path": "Assets/CueStrike/Models/TitleScreen/TitleScreen.fbx",
    "fps": 30,
    "duration_seconds": 8,
    "resolution": (1920, 1080),
    "camera_distance": 8.0,
    "camera_height": 1.6,
    "pool_table_scale": 1.0,
    "lighting": "cinematic",  # "cinematic", "studio", "neon"
    "animation_style": "dramatic",  # "dramatic", "smooth", "energetic"
}

# Derived
TOTAL_FRAMES = CONFIG["fps"] * CONFIG["duration_seconds"]

# ============================================================================
# UTILITY FUNCTIONS
# ============================================================================

def clear_scene():
    """ลบ object ทั้งหมดใน scene เริ่มต้น"""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    # ลบ material/mesh/data ที่ไม่ได้ใช้
    for block in bpy.data.meshes:
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in bpy.data.materials:
        if block.users == 0:
            bpy.data.materials.remove(block)

def create_material(name, color, metallic=0.0, roughness=0.5, emission=None, emission_strength=0.0, alpha=1.0):
    """สร้าง Material แบบ Principled BSDF"""
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    
    # Clear default
    nodes.clear()
    
    # Output
    output = nodes.new('ShaderNodeOutputMaterial')
    output.location = (400, 0)
    
    # Principled BSDF
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.location = (0, 0)
    bsdf.inputs['Base Color'].default_value = (*color, alpha)
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    if emission:
        bsdf.inputs['Emission Color'].default_value = (*emission, 1.0)
        bsdf.inputs['Emission Strength'].default_value = emission_strength
    
    links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    return mat

def animate_property(obj, property_path, frame_start, frame_end, value_start, value_end, interpolation='BEZIER'):
    """เพิ่ม keyframe animation สำหรับ property ใดๆ"""
    obj.keyframe_insert(property_path, frame=frame_start)
    setattr(obj, property_path, value_start)
    obj.keyframe_insert(property_path, frame=frame_start)
    
    setattr(obj, property_path, value_end)
    obj.keyframe_insert(property_path, frame=frame_end)
    
    # Set interpolation
    if obj.animation_data and obj.animation_data.action:
        for fcurve in obj.animation_data.action.fcurves:
            if fcurve.data_path == property_path:
                for kp in fcurve.keyframe_points:
                    kp.interpolation = interpolation

# ============================================================================
# MAIN BUILD FUNCTIONS
# ============================================================================

def build_pool_table():
    """สร้างโต๊ะบิลเลียดระดับ AAA"""
    # Table body
    bpy.ops.mesh.primitive_cube_add(size=2.54, location=(0, 0, 0.35))  # Standard 9ft = 2.54m
    table = bpy.context.active_object
    table.name = "PoolTable_Body"
    table.scale = (1.0, 0.5, 0.275)  # ยาว 2.54m, กว้าง 1.27m, สูง 0.55m
    
    # Cloth material (สีเขียวทรงสรรค์)
    cloth_mat = create_material(
        "Mat_PoolCloth", 
        color=(0.02, 0.15, 0.05), 
        roughness=0.85, 
        metallic=0.0
    )
    table.data.materials.append(cloth_mat)
    
    # Rails (บ่ารอง)
    rail_height = 0.037
    rail_width = 0.05
    rail_positions = [
        (0, 0.66, 0.55),    # Top rail
        (0, -0.66, 0.55),   # Bottom rail
        (1.3, 0, 0.55),     # Right rail
        (-1.3, 0, 0.55),    # Left rail
    ]
    
    rail_mat = create_material("Mat_Rail", color=(0.15, 0.08, 0.03), roughness=0.4, metallic=0.1)
    
    for i, pos in enumerate(rail_positions):
        bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
        rail = bpy.context.active_object
        rail.name = f"PoolTable_Rail_{i}"
        if i < 2:  # Top/bottom
            rail.scale = (1.27, rail_width/2, rail_height/2)
        else:  # Left/right
            rail.scale = (rail_width/2, 0.61, rail_height/2)
        rail.data.materials.append(rail_mat)
    
    # Pockets (หลุม) - 6 หลุม
    pocket_mat = create_material("Mat_Pocket", color=(0.0, 0.0, 0.0), roughness=0.9)
    pocket_positions = [
        (-1.22, 0.61, 0.35), (0, 0.61, 0.35), (1.22, 0.61, 0.35),
        (-1.22, -0.61, 0.35), (0, -0.61, 0.35), (1.22, -0.61, 0.35),
    ]
    
    for i, pos in enumerate(pocket_positions):
        bpy.ops.mesh.primitive_cylinder_add(radius=0.055, depth=0.3, location=pos)
        pocket = bpy.context.active_object
        pocket.name = f"Pocket_{i}"
        pocket.rotation_euler = (math.radians(90), 0, 0)
        pocket.data.materials.append(pocket_mat)
    
    # Cue ball (ลูกขาว) - อยู่บนโต๊ะ
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.0286, location=(0, 0, 0.58))
    cue_ball = bpy.context.active_object
    cue_ball.name = "CueBall_Title"
    cue_ball_mat = create_material("Mat_CueBall", color=(0.95, 0.95, 0.95), roughness=0.1, metallic=0.02)
    cue_ball.data.materials.append(cue_ball_mat)
    
    # Cue stick (ไม้คิว) - วางเอียง
    bpy.ops.mesh.primitive_cylinder_add(radius=0.012, depth=1.45, location=(-1.5, 0.3, 0.8))
    cue_stick = bpy.context.active_object
    cue_stick.name = "CueStick_Title"
    cue_stick.rotation_euler = (0, math.radians(30), math.radians(-15))
    cue_mat = create_material("Mat_CueStick", color=(0.35, 0.2, 0.1), roughness=0.3, metallic=0.1)
    cue_stick.data.materials.append(cue_mat)
    
    # Chalk (ขูด)
    bpy.ops.mesh.primitive_cube_add(size=0.03, location=(1.0, -0.4, 0.58))
    chalk = bpy.context.active_object
    chalk.name = "Chalk_Title"
    chalk_mat = create_material("Mat_Chalk", color=(0.1, 0.3, 0.6), roughness=0.9)
    chalk.data.materials.append(chalk_mat)
    
    return table, cue_ball, cue_stick

def build_title_text():
    """สร้างข้อความ CUE STRIKE แบบ 3D Text"""
    # Main title: CUE
    bpy.ops.object.text_add(location=(-1.8, 0, 1.2))
    text_cue = bpy.context.active_object
    text_cue.name = "Title_CUE"
    text_cue.data.body = "CUE"
    text_cue.data.size = 0.8
    text_cue.data.extrude = 0.06
    text_cue.data.align_x = 'CENTER'
    text_cue.data.font = bpy.data.fonts.load("//Fonts/BebasNeue-Regular.ttf") if "//Fonts/BebasNeue-Regular.ttf" else None
    
    # Main title: STRIKE
    bpy.ops.object.text_add(location=(1.2, 0, 1.2))
    text_strike = bpy.context.active_object
    text_strike.name = "Title_STRIKE"
    text_strike.data.body = "STRIKE"
    text_strike.data.size = 0.8
    text_strike.data.extrude = 0.06
    text_strike.data.align_x = 'CENTER'
    
    # Subtitle: VR Billiards
    bpy.ops.object.text_add(location=(0, 0, 0.3))
    text_sub = bpy.context.active_object
    text_sub.name = "Title_Subtitle"
    text_sub.data.body = "VR BILLIARDS"
    text_sub.data.size = 0.35
    text_sub.data.extrude = 0.03
    text_sub.data.align_x = 'CENTER'
    
    # Gold material for title
    gold_mat = create_material(
        "Mat_Title_Gold",
        color=(1.0, 0.84, 0.0),
        metallic=0.9,
        roughness=0.15,
        emission=(1.0, 0.84, 0.0),
        emission_strength=2.0
    )
    
    # White/Chrome material for subtitle
    chrome_mat = create_material(
        "Mat_Subtitle_Chrome",
        color=(0.9, 0.9, 0.95),
        metallic=0.8,
        roughness=0.2,
        emission=(0.2, 0.2, 0.3),
        emission_strength=0.5
    )
    
    for obj in [text_cue, text_strike]:
        obj.data.materials.append(gold_mat)
    text_sub.data.materials.append(chrome_mat)
    
    # Convert to mesh for better export
    for obj in [text_cue, text_strike, text_sub]:
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.convert(target='MESH')
    
    return text_cue, text_strike, text_sub

def build_lighting():
    """Cinematic Lighting Setup"""
    lights = []
    
    # Key Light - Warm spotlight from top-left
    bpy.ops.object.light_add(type='SUN', location=(5, 5, 10))
    key_light = bpy.context.active_object
    key_light.name = "Light_Key"
    key_light.data.energy = 8.0
    key_light.data.color = (1.0, 0.95, 0.85)
    key_light.rotation_euler = (math.radians(-45), 0, math.radians(-30))
    lights.append(key_light)
    
    # Fill Light - Cool from right
    bpy.ops.object.light_add(type='AREA', location=(6, -3, 4))
    fill_light = bpy.context.active_object
    fill_light.name = "Light_Fill"
    fill_light.data.energy = 150
    fill_light.data.color = (0.7, 0.8, 1.0)
    fill_light.data.size = 3.0
    fill_light.rotation_euler = (math.radians(-30), 0, math.radians(150))
    lights.append(fill_light)
    
    # Rim Light - Behind for edge glow
    bpy.ops.object.light_add(type='SPOT', location=(-4, 0, 5))
    rim_light = bpy.context.active_object
    rim_light.name = "Light_Rim"
    rim_light.data.energy = 500
    rim_light.data.color = (1.0, 0.9, 0.7)
    rim_light.data.spot_size = math.radians(45)
    rim_light.data.spot_blend = 0.3
    rim_light.rotation_euler = (math.radians(-20), 0, math.radians(180))
    lights.append(rim_light)
    
    # Accent - Neon glow under table
    bpy.ops.object.light_add(type='POINT', location=(0, 0, 0.1))
    accent_light = bpy.context.active_object
    accent_light.name = "Light_TableGlow"
    accent_light.data.energy = 200
    accent_light.data.color = (0.0, 0.5, 0.2)
    accent_light.data.shadow_soft_size = 0.5
    lights.append(accent_light)
    
    # World environment
    world = bpy.context.scene.world
    if not world:
        world = bpy.data.worlds.new("World_TitleScreen")
        bpy.context.scene.world = world
    world.use_nodes = True
    bg_node = world.node_tree.nodes.get("Background")
    if bg_node:
        bg_node.inputs[0].default_value = (0.02, 0.02, 0.03, 1.0)  # Dark blue-black
        bg_node.inputs[1].default_value = 0.1
    
    return lights

def build_camera():
    """Camera Setup สำหรับ Title Screen"""
    bpy.ops.object.camera_add(location=(0, -7, 1.6))
    camera = bpy.context.active_object
    camera.name = "Camera_TitleScreen"
    camera.data.lens = 50  # 50mm standard
    camera.data.sensor_width = 36
    camera.data.clip_start = 0.1
    camera.data.clip_end = 100
    
    # Point at center of table
    target = mathutils.Vector((0, 0, 0.55))
    direction = target - camera.location
    rot_quat = direction.to_track_quat('-Z', 'Y')
    camera.rotation_euler = rot_quat.to_euler()
    
    # Depth of field
    camera.data.dof.use_dof = True
    camera.data.dof.aperture_fstop = 2.8
    camera.data.dof.focus_distance = 7.0
    
    bpy.context.scene.camera = camera
    return camera

def build_particle_effects():
    """Particle effects: dust motes, sparkles, cue impact"""
    particles = []
    
    # Dust motes in light beams
    bpy.ops.mesh.primitive_plane_add(size=10, location=(0, 0, 2))
    dust_plane = bpy.context.active_object
    dust_plane.name = "DustMotes_Emitter"
    dust_plane.hide_render = True
    
    ps_dust = dust_plane.modifiers.new("DustMotes", 'PARTICLE_SYSTEM')
    ps_dust.settings.count = 500
    ps_dust.settings.frame_start = 1
    ps_dust.settings.frame_end = TOTAL_FRAMES
    ps_dust.settings.lifetime = TOTAL_FRAMES
    ps_dust.settings.emit_from = 'VOLUME'
    ps_dust.settings.particle_size = 0.005
    ps_dust.settings.physics_type = 'NEWTON'
    ps_dust.settings.mass = 0.01
    ps_dust.settings.effector_weights.gravity = 0.01
    ps_dust.settings.velocity_factor_normal = 0.1
    
    # Dust material
    dust_mat = create_material("Mat_Dust", color=(1.0, 0.95, 0.8), emission=(1.0, 0.95, 0.8), emission_strength=3.0, alpha=0.6)
    dust_mat.blend_method = 'BLENDED'
    dust_mat.shadow_method = 'NONE'
    ps_dust.settings.material_slot = 0
    dust_plane.data.materials.append(dust_mat)
    
    particles.append(dust_plane)
    
    # Cue impact sparkles (at frame ~120)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.002, location=(0, 0, 0.58))
    sparkle = bpy.context.active_object
    sparkle.name = "CueImpact_Sparkle"
    sparkle.hide_render = True
    
    ps_sparkle = sparkle.modifiers.new("ImpactSparkles", 'PARTICLE_SYSTEM')
    ps_sparkle.settings.count = 100
    ps_sparkle.settings.frame_start = 110
    ps_sparkle.settings.frame_end = 115
    ps_sparkle.settings.lifetime = 30
    ps_sparkle.settings.emit_from = 'VERT'
    ps_sparkle.settings.particle_size = 0.01
    ps_sparkle.settings.physics_type = 'NEWTON'
    ps_sparkle.settings.mass = 0.001
    ps_sparkle.settings.effector_weights.gravity = 0.5
    ps_sparkle.settings.velocity_factor_normal = 2.0
    ps_sparkle.settings.normal_factor = 1.0
    ps_sparkle.settings.tangent_factor = 0.5
    ps_sparkle.settings.phase_factor = 1.0
    
    sparkle_mat = create_material("Mat_Sparkle", color=(1.0, 1.0, 0.5), emission=(1.0, 1.0, 0.2), emission_strength=10.0)
    ps_sparkle.settings.material_slot = 0
    sparkle.data.materials.append(sparkle_mat)
    
    particles.append(sparkle)
    
    return particles

def animate_title_screen(table, cue_ball, cue_stick, title_cue, title_strike, title_sub, camera, lights):
    """Animation Sequence สำหรับ Title Screen"""
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = TOTAL_FRAMES
    
    # ============================================================
    # PHASE 1: Frames 1-60 (0-2s) - Dramatic Reveal
    # ============================================================
    # Camera starts far, dollies in
    camera.location = (0, -12, 2.5)
    camera.keyframe_insert("location", frame=1)
    camera.location = (0, -7, 1.6)
    camera.keyframe_insert("location", frame=60)
    
    # Title text starts off-screen, slides in with glow
    title_cue.location = (-3, 0, 1.2)
    title_cue.keyframe_insert("location", frame=1)
    title_cue.location = (-1.8, 0, 1.2)
    title_cue.keyframe_insert("location", frame=40)
    
    title_strike.location = (3, 0, 1.2)
    title_strike.keyframe_insert("location", frame=1)
    title_strike.location = (1.2, 0, 1.2)
    title_strike.keyframe_insert("location", frame=40)
    
    # Subtitle fades in later
    title_sub.location = (0, 0, 0.3)
    title_sub.scale = (1, 1, 1)
    title_sub.keyframe_insert("location", frame=45)
    title_sub.keyframe_insert("scale", frame=45)
    title_sub.location = (0, -0.5, 0.3)
    title_sub.scale = (0.5, 0.5, 0.5)
    title_sub.keyframe_insert("location", frame=30)
    title_sub.keyframe_insert("scale", frame=30)
    
    # ============================================================
    # PHASE 2: Frames 60-120 (2-4s) - Cue Action
    # ============================================================
    # Cue stick pulls back
    cue_stick.location = (-1.5, 0.3, 0.8)
    cue_stick.keyframe_insert("location", frame=60)
    cue_stick.location = (-2.2, 0.3, 0.8)
    cue_stick.keyframe_insert("location", frame=100)
    
    # Cue ball waits
    cue_ball.location = (0, 0, 0.58)
    cue_ball.keyframe_insert("location", frame=60)
    cue_ball.keyframe_insert("location", frame=100)
    
    # ============================================================
    # PHASE 3: Frames 120-150 (4-5s) - Impact & Title Lock
    # ============================================================
    # Cue strikes forward
    cue_stick.location = (-0.5, 0.3, 0.8)
    cue_stick.keyframe_insert("location", frame=118)
    cue_stick.location = (0.3, 0.3, 0.58)
    cue_stick.keyframe_insert("location", frame=122)
    
    # Cue ball shoots
    cue_ball.location = (0, 0, 0.58)
    cue_ball.keyframe_insert("location", frame=120)
    cue_ball.location = (1.5, 0, 0.58)
    cue_ball.keyframe_insert("location", frame=150)
    cue_ball.rotation_euler = (0, 0, 0)
    cue_ball.keyframe_insert("rotation_euler", frame=120)
    cue_ball.rotation_euler = (math.radians(720), 0, 0)
    cue_ball.keyframe_insert("rotation_euler", frame=150)
    
    # Camera shake on impact
    camera.location = (0, -7, 1.6)
    camera.keyframe_insert("location", frame=119)
    camera.location = (0.05, -7.02, 1.58)
    camera.keyframe_insert("location", frame=121)
    camera.location = (-0.03, -6.98, 1.62)
    camera.keyframe_insert("location", frame=123)
    camera.location = (0, -7, 1.6)
    camera.keyframe_insert("location", frame=125)
    
    # Title text "locks" with pulse glow
    for obj in [title_cue, title_strike]:
        obj.scale = (1, 1, 1)
        obj.keyframe_insert("scale", frame=120)
        obj.scale = (1.05, 1.05, 1.05)
        obj.keyframe_insert("scale", frame=122)
        obj.scale = (1, 1, 1)
        obj.keyframe_insert("scale", frame=125)
    
    # ============================================================
    # PHASE 4: Frames 150-240 (5-8s) - Hold & Loop Setup
    # ============================================================
    # Subtle floating animation for title
    for obj in [title_cue, title_strike]:
        obj.location = obj.location
        obj.keyframe_insert("location", frame=150)
        obj.location = (obj.location.x, obj.location.y + 0.02, obj.location.z)
        obj.keyframe_insert("location", frame=195)
        obj.location = obj.location
        obj.keyframe_insert("location", frame=240)
    
    # Subtitle gentle pulse
    title_sub.scale = (1, 1, 1)
    title_sub.keyframe_insert("scale", frame=150)
    title_sub.scale = (1.02, 1.02, 1.02)
    title_sub.keyframe_insert("scale", frame=195)
    title_sub.scale = (1, 1, 1)
    title_sub.keyframe_insert("scale", frame=240)
    
    # Camera slow orbit
    camera.location = (0, -7, 1.6)
    camera.keyframe_insert("location", frame=150)
    camera.location = (0.5, -7.2, 1.7)
    camera.keyframe_insert("location", frame=240)
    
    # Light animation - key light subtle pulse
    key_light = next(l for l in lights if l.name == "Light_Key")
    key_light.data.energy = 8.0
    key_light.keyframe_insert("data.energy", frame=1)
    key_light.data.energy = 10.0
    key_light.keyframe_insert("data.energy", frame=120)
    key_light.data.energy = 8.0
    key_light.keyframe_insert("data.energy", frame=240)
    
    # Rim light color shift
    rim_light = next(l for l in lights if l.name == "Light_Rim")
    rim_light.data.color = (1.0, 0.9, 0.7)
    rim_light.keyframe_insert("data.color", frame=1)
    rim_light.data.color = (1.0, 0.7, 0.4)
    rim_light.keyframe_insert("data.color", frame=120)
    rim_light.data.color = (1.0, 0.9, 0.7)
    rim_light.keyframe_insert("data.color", frame=240)
    
    # Set interpolation to BEZIER for smooth motion
    for obj in bpy.data.objects:
        if obj.animation_data and obj.animation_data.action:
            for fcurve in obj.animation_data.action.fcurves:
                for kp in fcurve.keyframe_points:
                    kp.interpolation = 'BEZIER'
                    kp.handle_left_type = 'AUTO'
                    kp.handle_right_type = 'AUTO'

def setup_render_settings():
    """ตั้งค่า Render สำหรับ Export"""
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = TOTAL_FRAMES
    scene.render.fps = CONFIG["fps"]
    scene.render.resolution_x = CONFIG["resolution"][0]
    scene.render.resolution_y = CONFIG["resolution"][1]
    scene.render.resolution_percentage = 100
    scene.render.engine = 'CYCLES'  # ใช้ Cycles สำหรับคุณภาพสูง
    scene.cycles.samples = 64
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = 'Filmic'
    scene.view_settings.look = 'Medium High Contrast'
    
    # Color management
    scene.sequencer_colorspace_settings.name = 'sRGB'

def export_fbx(output_path):
    """Export เป็น FBX สำหรับ Unity"""
    # Select all objects
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.data.objects:
        if obj.type in ['MESH', 'ARMATURE', 'LIGHT', 'CAMERA']:
            obj.select_set(True)
    
    # Ensure output directory exists
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    
    # Export FBX
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True,
        object_types={'MESH', 'ARMATURE', 'LIGHT', 'CAMERA'},
        use_mesh_modifiers=True,
        use_mesh_modifiers_render=True,
        mesh_smooth_type='FACE',
        use_subsurf=False,
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        use_armature_deform_only=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=True,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=1.0,
        path_mode='COPY',
        embed_textures=False,
    )
    print(f"✅ Exported FBX to: {output_path}")

def save_blend_file(output_path):
    """บันทึก .blend file สำหรับแก้ไขภายหลัง"""
    blend_path = output_path.replace('.fbx', '.blend')
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"✅ Saved .blend to: {blend_path}")

# ============================================================================
# MAIN EXECUTION
# ============================================================================

def main():
    print("=" * 60)
    print("🎱 CueStrike VR Title Screen Generator")
    print("=" * 60)
    
    # Parse command line arguments
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1:]
    
    output_path = CONFIG["output_path"]
    for i, arg in enumerate(argv):
        if arg == "--output" and i + 1 < len(argv):
            output_path = argv[i + 1]
    
    # Make path absolute
    output_path = os.path.abspath(output_path)
    
    print(f"📁 Output: {output_path}")
    print(f"⏱️  Duration: {CONFIG['duration_seconds']}s @ {CONFIG['fps']}fps = {TOTAL_FRAMES} frames")
    print(f"🎬 Style: {CONFIG['animation_style']} | Lighting: {CONFIG['lighting']}")
    
    # Build scene
    print("\n🔨 Building scene...")
    clear_scene()
    
    print("  📐 Pool table + props...")
    table, cue_ball, cue_stick = build_pool_table()
    
    print("  ✨ Title text...")
    title_cue, title_strike, title_sub = build_title_text()
    
    print("  💡 Cinematic lighting...")
    lights = build_lighting()
    
    print("  📷 Camera setup...")
    camera = build_camera()
    
    print("  ✨ Particle effects...")
    particles = build_particle_effects()
    
    print("  🎞️ Animating...")
    animate_title_screen(table, cue_ball, cue_stick, title_cue, title_strike, title_sub, camera, lights)
    
    print("  ⚙️ Render settings...")
    setup_render_settings()
    
    print("  📤 Exporting FBX...")
    export_fbx(output_path)
    
    print("  💾 Saving .blend...")
    save_blend_file(output_path)
    
    print("\n" + "=" * 60)
    print("✅ TITLE SCREEN GENERATION COMPLETE!")
    print("=" * 60)
    print(f"📁 FBX: {output_path}")
    print(f"📁 Blend: {output_path.replace('.fbx', '.blend')}")
    print("\n📋 Next Steps in Unity:")
    print("  1. Import FBX → Assets/CueStrike/Models/TitleScreen/")
    print("  2. Create TitleScreen scene")
    print("  3. Set Animation → Loop Time ✓ | Loop Pose ✓")
    print("  4. Add AudioSource for title music")
    print("  5. Hook up to TitleScreenManager.cs")

if __name__ == "__main__":
    main()