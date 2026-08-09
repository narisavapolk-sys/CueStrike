#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Blender script: create_character_aaa.py
Purpose:
    Generate a high‑quality AAA‑ready humanoid character for CueStrike.
    - Creates a base mesh (low‑poly body) using Blender primitives.
    - Applies a Rigify humanoid rig (T‑pose) and generates an Avatar‑compatible armature.
    - Adds basic weight‑painting for torso, limbs and head.
    - Generates simple PBR textures (placeholder colors) and saves them as PNG.
    - Exports the finished character as FBX + texture PNGs into BlenderScripts/Exports/.

Usage (CLI):
    blender --background --python create_character_aaa.py -- <character_name>
    Example:
    blender --background --python create_character_aaa.py -- Somchay

Requirements:
    - Enable the Rigify addon in Blender (Edit → Preferences → Add‑ons → Rigify).
    - The script runs in headless (background) mode.
"""

import bpy
import sys
import os

# ------------------------------------------------------------
# Helper Functions
# ------------------------------------------------------------
def clean_scene():
    """Remove default objects and start from a clean scene."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

def create_body(name):
    """Create a simple low‑poly humanoid body using cube and cylinder primitives."""
    # Torso
    bpy.ops.mesh.primitive_cube_add(size=0.5, location=(0, 0, 1))
    torso = bpy.context.active_object
    torso.name = f"{name}_Torso"
    torso.scale = (0.35, 0.2, 0.5)

    # Head
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.18, location=(0, 0, 1.75))
    head = bpy.context.active_object
    head.name = f"{name}_Head"

    # Arms
    for side, x_sign in [('L', -1), ('R', 1)]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.07, depth=0.4,
                                            location=(0.35 * x_sign, 0, 1.4))
        arm = bpy.context.active_object
        arm.name = f"{name}_Arm_{side}"
        arm.rotation_euler[1] = 1.5708  # rotate 90° on Y

    # Legs
    for side, x_sign in [('L', -0.125), ('R', 0.125)]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.09, depth=0.5,
                                            location=(x_sign, 0, 0.5))
        leg = bpy.context.active_object
        leg.name = f"{name}_Leg_{side}"
        leg.rotation_euler[0] = 0.0

    # Join all parts into one mesh (optional)
    objs = [obj for obj in bpy.context.scene.objects if obj.name.startswith(name)]
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objs:
        obj.select_set(True)
    bpy.ops.object.join()
    return bpy.context.active_object

def add_rigify(name, body_obj):
    """Add a Rigify metarig and generate the final rig."""
    # Add metarig
    bpy.ops.object.armature_human_metarig_add()
    metarig = bpy.context.active_object
    metarig.name = f"{name}_Metarig"

    # Align metarig to body (simple translation)
    metarig.location = body_obj.location
    metarig.location.z += 0.5  # raise a bit to match torso height

    # Generate the final rig
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.view_layer.objects.active = metarig
    metarig.select_set(True)
    bpy.ops.object.mode_set(mode='POSE')
    bpy.ops.pose.select_all(action='SELECT')
    bpy.ops.pose.rigify_generate()
    rig = bpy.context.active_object
    rig.name = f"{name}_Rig"

    # Parent body mesh to rig (with automatic weights)
    bpy.ops.object.select_all(action='DESELECT')
    body_obj.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')

    return rig

def create_placeholder_texture(name, out_dir):
    """Create a simple colored PNG texture for Albedo, Normal and Roughness."""
    import numpy as np
    from math import sin, cos, pi

    size = 1024  # 1K placeholder (good enough for prototype)

    # Albedo (solid color based on name hash)
    hash_val = sum(ord(c) for c in name) % 255
    albedo = np.full((size, size, 4), [hash_val/255.0, 0.6, 0.8, 1.0], dtype=np.float32)

    # Normal (default pointing Z)
    normal = np.full((size, size, 4), [0.5, 0.5, 1.0, 1.0], dtype=np.float32)

    # Roughness (mid value)
    rough = np.full((size, size, 4), [0.5, 0.5, 0.5, 1.0], dtype=np.float32)

    # Save each as PNG
    def save_np_image(arr, filename):
        img = bpy.data.images.new(name=filename, width=size, height=size, alpha=True, float_buffer=False)
        flat = (arr * 255).astype('uint8').flatten()
        img.pixels = [v / 255.0 for v in flat]
        img.filepath_raw = os.path.join(out_dir, f"{filename}.png")
        img.file_format = 'PNG'
        img.save()

    save_np_image(albedo, f"{name}_Albedo")
    save_np_image(normal, f"{name}_Normal")
    save_np_image(rough, f"{name}_Roughness")

def export_fbx(name, obj, out_dir):
    """Export the rigged character to FBX."""
    export_path = os.path.join(out_dir, f"{name}_AAA.fbx")
    bpy.ops.export_scene.fbx(
        filepath=export_path,
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=True,
        object_types={'ARMATURE', 'MESH'},
        mesh_smooth_type='OFF',
        use_tspace=True,
        add_leaf_bones=False,
        axis_forward='-Z',
        axis_up='Y'
    )
    print(f"Exported FBX: {export_path}")

# ------------------------------------------------------------
# Main execution
# ------------------------------------------------------------
def main():
    # Enable Rigify addon (required for metarig) in headless mode
    try:
        bpy.ops.preferences.addon_enable(module="rigify")
        print("Rigify addon enabled.")
    except Exception as e:
        print(f"WARNING: Could not enable Rigify addon: {e}")

    # Parse arguments after '--'
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1:]
    else:
        argv = []

    if len(argv) < 1:
        print("Usage: blender --background --python create_character_aaa.py -- <CharacterName>")
        return

    char_name = argv[0]

    # Define export directory (use script location so it works in headless mode)
    script_dir = os.path.dirname(os.path.abspath(__file__))
    export_dir = os.path.join(script_dir, "Exports")
    if not os.path.exists(export_dir):
        os.makedirs(export_dir)

    clean_scene()
    body = create_body(char_name)
    rig = add_rigify(char_name, body)
    create_placeholder_texture(char_name, export_dir)
    export_fbx(char_name, rig, export_dir)
    print(f"Character {char_name} generation complete. Files in {export_dir}")

if __name__ == "__main__":
    main()