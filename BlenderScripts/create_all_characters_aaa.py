#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Blender script: create_all_characters_aaa.py
Purpose:
    Generate ALL missing AAA humanoid characters in ONE batch run.
    Built on top of create_character_aaa.py but with per-character theming
    (silhouette, outfit, colors) so each playable character is distinctive.

    Characters generated (from CHARACTER_SYSTEM_PLAN.md, 9 remaining):
      MeiLing, Gentleman, PanPan, Finn, KingFlex, Tusker, Phantom, Cassidy, Bones
    (Somchay already exists as Somchay_AAA.fbx)

Usage (headless):
    blender --background --python create_all_characters_aaa.py

Output:
    BlenderScripts/Exports/<Name>_AAA.fbx + <Name>_Albedo.png, _Normal.png, _Roughness.png
    Also copies/moves FBX into Assets/CueStrike/Models/ automatically.

Requirements:
    - Blender 3.6 (tested) with Rigify addon enabled.
    - The script runs in background (headless) mode.
"""

import bpy
import sys
import os
import json
import shutil

# ------------------------------------------------------------
# Character Theme Table
# ------------------------------------------------------------
# Each entry = (id, display_name) + theme params:
#   primary        : main outfit color (RGB)
#   accent         : accent/highlight color (RGB)
#   skin           : body/skin color (RGB)
#   silhouette     : base shape modifier. e.g. 'elephant' => wider/taller,
#                    'panda'  => shorter/rounder, 'skeleton' => thin, etc.
#   horns          : add horns/ears props (True/False)
#   hat            : add hat/helmet prop (type string or None)
#   glow           : add an emissive skull/aura (True/False)
#   extra          : extra props (list of strings)

CHARACTERS = [
    {
        "id": "meiling",
        "name": "MeiLing",
        "primary": (0.85, 0.15, 0.20),      # Chinese red
        "accent":  (0.95, 0.75, 0.15),      # gold
        "skin":    (0.90, 0.75, 0.60),
        "silhouette": "human",              # graceful female
        "hat": "cherry_blossom",
        "extra": ["flower"],
    },
    {
        "id": "gentleman",
        "name": "Gentleman",
        "primary": (0.20, 0.20, 0.25),      # charcoal suit
        "accent":  (0.85, 0.82, 0.75),      # ivory
        "skin":    (0.75, 0.62, 0.50),
        "silhouette": "elephant",           # big rounded body + trunk
        "hat": "top_hat",
        "extra": ["monocle"],
    },
    {
        "id": "panpan",
        "name": "PanPan",
        "primary": (0.95, 0.95, 0.95),      # white panda fur
        "accent":  (0.10, 0.10, 0.12),      # black ears/patches
        "skin":    (0.90, 0.90, 0.90),
        "silhouette": "panda",              # round + short
        "hat": None,
        "extra": ["bamboo"],
    },
    {
        "id": "finn",
        "name": "Finn",
        "primary": (0.10, 0.35, 0.75),      # deep blue wetsuit/diver
        "accent":  (0.95, 0.60, 0.05),      # orange (dive accents)
        "skin":    (0.80, 0.72, 0.62),
        "silhouette": "human",
        "hat": "diving_mask",
        "extra": ["oxygen_tank", "fins"],
    },
    {
        "id": "kingflex",
        "name": "KingFlex",
        "primary": (0.95, 0.85, 0.10),      # gold
        "accent":  (0.30, 0.30, 0.35),      # dark
        "skin":    (0.80, 0.70, 0.58),
        "silhouette": "muscular",           # big shoulders/chest
        "hat": "crown",
        "extra": ["gold_chain", "ring"],
    },
    {
        "id": "tusker",
        "name": "Tusker",
        "primary": (0.35, 0.45, 0.55),      # grey-blue elephant
        "accent":  (0.95, 0.92, 0.80),      # tusks ivory
        "skin":    (0.70, 0.72, 0.75),
        "silhouette": "elephant",
        "hat": "fez",
        "extra": ["tusks", "memory_glow"],
    },
    {
        "id": "phantom",
        "name": "Phantom",
        "primary": (0.12, 0.12, 0.18),      # cloak dark
        "accent":  (0.20, 0.85, 0.95),      # spectral cyan glow
        "skin":    (0.15, 0.15, 0.20),
        "silhouette": "ghost",              # floating cloak
        "hat": None,
        "extra": ["spectral_aura", "glow_eyes"],
    },
    {
        "id": "cassidy",
        "name": "Cassidy",
        "primary": (0.72, 0.55, 0.35),      # leather brown
        "accent":  (0.90, 0.85, 0.60),      # tan hat
        "skin":    (0.85, 0.70, 0.58),
        "silhouette": "human",
        "hat": "cowboy_hat",
        "extra": ["revolver", "chaps"],
    },
    {
        "id": "bones",
        "name": "Bones",
        "primary": (0.85, 0.85, 0.85),      # bone white
        "accent":  (0.15, 0.80, 0.20),      # toxic/energy green
        "skin":    (0.90, 0.90, 0.90),
        "silhouette": "skeleton",           # thin + ribcage
        "hat": None,
        "extra": ["ribs", "glow_eyes"],
    },
    {
        "id": "bopanda",
        "name": "BoPanda",
        "primary": (0.95, 0.95, 0.95),      # white panda fur
        "accent":  (0.10, 0.10, 0.12),      # black ears/patches
        "skin":    (0.90, 0.88, 0.95),
        "silhouette": "panda",              # round short mascot
        "hat": "bamboo_hat",
        "extra": ["bamboo", "hint_glow"],
    },
    {
        "id": "unclenok",
        "name": "UncleNok",
        "primary": (0.62, 0.70, 0.82),      # grey-blue elephant
        "accent":  (0.95, 0.92, 0.80),      # ivory tusks
        "skin":    (0.70, 0.72, 0.75),
        "silhouette": "elephant",           # big good-natured old elephant
        "hat": "bowler_hat",
        "extra": ["tusks", "nice_aura"],
    },
]

UNITY_MODELS_DIR = os.path.normpath(
    os.path.join(os.path.dirname(os.path.abspath(__file__)),
                 "..", "Assets", "CueStrike", "Models")
)

# ------------------------------------------------------------
# Scene / Object Helpers
# ------------------------------------------------------------
def clean_scene():
    """Remove default objects and start from a clean scene."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)


def new_material(name, color, roughness=0.6, metallic=0.0, emission=None, emission_strength=0.0):
    """Create a PBR material (works with URP after FBX import conversion)."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links

    # Remove default Principled BSDF (we'll add our own standard PBR)
    for node in nodes:
        if node.type == 'BSDF_PRINCIPLED':
            nodes.remove(node)

    # Create Principled BSDF (Blender 3.6 uses 'Emission Color' for Principled BSDF)
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Metallic'].default_value = metallic
    if emission is not None:
        # Blender 3.6: 'Emission Color'; older versions used 'Emission' — support both.
        emission_input = bsdf.inputs.get('Emission Color') or bsdf.inputs.get('Emission')
        if emission_input is not None:
            emission_input.default_value = (*emission, 1.0)
        emission_strength_input = bsdf.inputs.get('Emission Strength')
        if emission_strength_input is not None:
            emission_strength_input.default_value = emission_strength

    # Output
    output = nodes.new('ShaderNodeOutputMaterial')

    links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    return mat


def apply_material(obj, mat):
    """Assign material to object."""
    if obj.data.materials:
        obj.data.materials[0] = mat
    else:
        obj.data.materials.append(mat)


def set_color(obj, color, roughness=0.6, metallic=0.0, emission=None, emission_strength=0.0):
    """Convenience: assign a new material by color."""
    mat = new_material(f"{obj.name}_mat", color, roughness, metallic, emission, emission_strength)
    apply_material(obj, mat)


# ------------------------------------------------------------
# Body Builders (silhouette-based)
# ------------------------------------------------------------
def _primitive_kind(kind, size=0.5):
    """Create a primitive and return it."""
    if kind == 'cube':
        bpy.ops.mesh.primitive_cube_add(size=size)
    elif kind == 'sphere':
        bpy.ops.mesh.primitive_uv_sphere_add(radius=size)
    elif kind == 'cylinder':
        bpy.ops.mesh.primitive_cylinder_add(radius=size, depth=size * 2)
    elif kind == 'cone':
        bpy.ops.mesh.primitive_cone_add(radius1=size, radius2=size * 0.1, depth=size * 2)
    elif kind == 'torus':
        bpy.ops.mesh.primitive_torus_add(major_radius=size, minor_radius=size * 0.15)
    return bpy.context.active_object


def build_human_body(name, cfg):
    """Standard stylized humanoid (cube + sphere + cylinders)."""
    parts = []

    # Torso
    torso = _primitive_kind('cube', 0.5)
    torso.name = f"{name}_Torso"
    torso.location = (0, 0, 1.1)
    torso.scale = (0.40, 0.22, 0.55)
    set_color(torso, cfg["primary"])
    parts.append(torso)

    # Head
    head = _primitive_kind('sphere', 0.22)
    head.name = f"{name}_Head"
    head.location = (0, 0, 1.85)
    set_color(head, cfg["skin"])
    parts.append(head)

    # Arms
    for side, sign in [('L', -1), ('R', 1)]:
        arm = _primitive_kind('cylinder', 0.07)
        arm.name = f"{name}_Arm_{side}"
        arm.location = (0.42 * sign, 0, 1.4)
        arm.rotation_euler[1] = 1.5708
        arm.scale = (1, 1, 1.8)
        set_color(arm, cfg["primary"])
        parts.append(arm)

    # Legs
    for side, sign in [('L', -0.14), ('R', 0.14)]:
        leg = _primitive_kind('cylinder', 0.09)
        leg.name = f"{name}_Leg_{side}"
        leg.location = (sign, 0, 0.55)
        leg.scale = (1, 1, 1.6)
        set_color(leg, cfg["primary"])
        parts.append(leg)

    return parts


def build_elephant_body(name, cfg):
    """Elephant-like: big rounded torso, trunk, ears, tusks, legs."""
    parts = []

    # Torso (big rounded cube)
    torso = _primitive_kind('cube', 0.6)
    torso.name = f"{name}_Torso"
    torso.location = (0, 0, 1.2)
    torso.scale = (0.55, 0.35, 0.60)
    set_color(torso, cfg["primary"])
    parts.append(torso)

    # Head (big sphere)
    head = _primitive_kind('sphere', 0.30)
    head.name = f"{name}_Head"
    head.location = (0, 0, 1.85)
    head.scale = (1.1, 0.9, 1.0)
    set_color(head, cfg["primary"])
    parts.append(head)

    # Trunk
    trunk = _primitive_kind('cylinder', 0.10)
    trunk.name = f"{name}_Trunk"
    trunk.location = (0, 0.10, 1.45)
    trunk.scale = (1, 1, 2.2)
    set_color(trunk, cfg["primary"])
    parts.append(trunk)

    # Ears
    for side, sign in [('L', -1), ('R', 1)]:
        ear = _primitive_kind('sphere', 0.10)
        ear.name = f"{name}_Ear_{side}"
        ear.location = (0.42 * sign, 0.0, 1.95)
        ear.scale = (0.5, 1.7, 1.2)
        set_color(ear, cfg["primary"])
        parts.append(ear)

    # Legs (thick)
    for side, sign in [('L', -0.18), ('R', 0.18)]:
        leg = _primitive_kind('cylinder', 0.13)
        leg.name = f"{name}_Leg_{side}"
        leg.location = (sign, 0, 0.55)
        leg.scale = (1, 1, 1.6)
        set_color(leg, cfg["primary"])
        parts.append(leg)

    # Tusks (accent)
    if "tusks" in cfg.get("extra", []):
        for side, sign in [('L', -0.12), ('R', 0.12)]:
            tusk = _primitive_kind('cone', 0.05)
            tusk.name = f"{name}_Tusk_{side}"
            tusk.location = (sign, 0.32, 1.55)
            tusk.rotation_euler[0] = -0.5
            tusk.scale = (1, 1, 1.5)
            set_color(tusk, cfg["accent"])
            parts.append(tusk)

    return parts


def build_panda_body(name, cfg):
    """Panda: round white body, black ears/patches, short legs."""
    parts = []

    # Body (round white)
    body = _primitive_kind('sphere', 0.35)
    body.name = f"{name}_Body"
    body.location = (0, 0, 1.0)
    body.scale = (1.1, 0.85, 0.9)
    set_color(body, cfg["primary"])
    parts.append(body)

    # Head (round white)
    head = _primitive_kind('sphere', 0.25)
    head.name = f"{name}_Head"
    head.location = (0, 0.05, 1.75)
    set_color(head, cfg["primary"])
    parts.append(head)

    # Black ear patches
    for side, sign in [('L', -1), ('R', 1)]:
        ear = _primitive_kind('sphere', 0.08)
        ear.name = f"{name}_Ear_{side}"
        ear.location = (0.22 * sign, 0.02, 1.95)
        set_color(ear, cfg["accent"])
        parts.append(ear)

    # Black eye patches
    for side, sign in [('L', -0.09), ('R', 0.09)]:
        eye = _primitive_kind('sphere', 0.04)
        eye.name = f"{name}_EyePatch_{side}"
        eye.location = (sign, 0.30, 1.82)
        set_color(eye, cfg["accent"])
        parts.append(eye)

    # Arms (black)
    for side, sign in [('L', -1), ('R', 1)]:
        arm = _primitive_kind('cylinder', 0.08)
        arm.name = f"{name}_Arm_{side}"
        arm.location = (0.38 * sign, 0, 1.3)
        arm.rotation_euler[1] = 1.5708
        arm.scale = (1, 1, 1.2)
        set_color(arm, cfg["accent"])
        parts.append(arm)

    # Legs (black, short)
    for side, sign in [('L', -0.15), ('R', 0.15)]:
        leg = _primitive_kind('cylinder', 0.10)
        leg.name = f"{name}_Leg_{side}"
        leg.location = (sign, 0, 0.45)
        leg.scale = (1, 1, 1.0)
        set_color(leg, cfg["accent"])
        parts.append(leg)

    return parts


def build_muscular_body(name, cfg):
    """Muscular: wide shoulders, big chest, thick limbs."""
    parts = []

    # Chest (big upper body)
    chest = _primitive_kind('cube', 0.55)
    chest.name = f"{name}_Chest"
    chest.location = (0, 0, 1.3)
    chest.scale = (0.55, 0.25, 0.45)
    set_color(chest, cfg["primary"])
    parts.append(chest)

    # Head
    head = _primitive_kind('sphere', 0.22)
    head.name = f"{name}_Head"
    head.location = (0, 0, 1.95)
    set_color(head, cfg["skin"])
    parts.append(head)

    # Arms (thick)
    for side, sign in [('L', -1), ('R', 1)]:
        arm = _primitive_kind('cylinder', 0.10)
        arm.name = f"{name}_Arm_{side}"
        arm.location = (0.52 * sign, 0, 1.5)
        arm.rotation_euler[1] = 1.5708
        arm.scale = (1, 1, 1.6)
        set_color(arm, cfg["primary"])
        parts.append(arm)

    # Legs (thick)
    for side, sign in [('L', -0.17), ('R', 0.17)]:
        leg = _primitive_kind('cylinder', 0.12)
        leg.name = f"{name}_Leg_{side}"
        leg.location = (sign, 0, 0.55)
        leg.scale = (1, 1, 1.6)
        set_color(leg, cfg["primary"])
        parts.append(leg)

    return parts


def build_ghost_body(name, cfg):
    """Ghost: floating cloak, no legs, spectral glow."""
    parts = []

    # Cloak (cone-like)
    bpy.ops.mesh.primitive_cone_add(radius1=0.45, radius2=0.1, depth=1.4)
    cloak = bpy.context.active_object
    cloak.name = f"{name}_Cloak"
    cloak.location = (0, 0, 1.0)
    set_color(cloak, cfg["primary"], emission=cfg["accent"], emission_strength=0.6)
    parts.append(cloak)

    # Head (ghostly sphere)
    head = _primitive_kind('sphere', 0.20)
    head.name = f"{name}_Head"
    head.location = (0, 0, 1.9)
    set_color(head, cfg["accent"], emission=cfg["accent"], emission_strength=1.0)
    parts.append(head)

    # Eyes glow
    for side, sign in [('L', -0.07), ('R', 0.07)]:
        eye = _primitive_kind('sphere', 0.03)
        eye.name = f"{name}_Eye_{side}"
        eye.location = (sign, 0.16, 1.95)
        set_color(eye, (1.0, 1.0, 1.0), emission=(1.0, 1.0, 1.0), emission_strength=2.0)
        parts.append(eye)

    # Spectral aura (big transparent-ish emitter)
    aura = _primitive_kind('sphere', 0.55)
    aura.name = f"{name}_Aura"
    aura.location = (0, 0, 1.1)
    set_color(aura, cfg["accent"], emission=cfg["accent"], emission_strength=0.35)
    parts.append(aura)

    return parts


def build_skeleton_body(name, cfg):
    """Skeleton: thin bones, ribcage, skull with glow eyes."""
    parts = []

    # Spine (thin)
    spine = _primitive_kind('cylinder', 0.06)
    spine.name = f"{name}_Spine"
    spine.location = (0, 0, 1.2)
    spine.scale = (1, 1, 1.8)
    set_color(spine, cfg["primary"])
    parts.append(spine)

    # Pelvis
    pelvis = _primitive_kind('cube', 0.25)
    pelvis.name = f"{name}_Pelvis"
    pelvis.location = (0, 0, 0.8)
    pelvis.scale = (1.2, 0.6, 0.5)
    set_color(pelvis, cfg["primary"])
    parts.append(pelvis)

    # Ribs (several small spheres/cylinders)
    for i in range(4):
        rib = _primitive_kind('torus', 0.12)
        rib.name = f"{name}_Rib_{i}"
        rib.location = (0, 0, 1.1 + i * 0.12)
        rib.scale = (1.4, 0.6, 0.8)
        set_color(rib, cfg["primary"])
        parts.append(rib)

    # Head (skull)
    skull = _primitive_kind('sphere', 0.18)
    skull.name = f"{name}_Skull"
    skull.location = (0, 0, 1.8)
    set_color(skull, cfg["primary"])
    parts.append(skull)

    # Glowing eyes
    for side, sign in [('L', -0.06), ('R', 0.06)]:
        eye = _primitive_kind('sphere', 0.03)
        eye.name = f"{name}_Eye_{side}"
        eye.location = (sign, 0.14, 1.85)
        set_color(eye, cfg["accent"], emission=cfg["accent"], emission_strength=2.0)
        parts.append(eye)

    # Arms (thin)
    for side, sign in [('L', -1), ('R', 1)]:
        arm = _primitive_kind('cylinder', 0.05)
        arm.name = f"{name}_Arm_{side}"
        arm.location = (0.36 * sign, 0, 1.4)
        arm.rotation_euler[1] = 1.5708
        arm.scale = (1, 1, 1.5)
        set_color(arm, cfg["primary"])
        parts.append(arm)

    # Legs (thin)
    for side, sign in [('L', -0.10), ('R', 0.10)]:
        leg = _primitive_kind('cylinder', 0.05)
        leg.name = f"{name}_Leg_{side}"
        leg.location = (sign, 0, 0.5)
        leg.scale = (1, 1, 1.6)
        set_color(leg, cfg["primary"])
        parts.append(leg)

    return parts


# ------------------------------------------------------------
# Body factory
# ------------------------------------------------------------
def build_body(name, cfg):
    """Dispatch to body builder based on silhouette."""
    sil = cfg.get("silhouette", "human")
    if sil == "elephant":
        return build_elephant_body(name, cfg)
    elif sil == "panda":
        return build_panda_body(name, cfg)
    elif sil == "muscular":
        return build_muscular_body(name, cfg)
    elif sil == "ghost":
        return build_ghost_body(name, cfg)
    elif sil == "skeleton":
        return build_skeleton_body(name, cfg)
    else:
        return build_human_body(name, cfg)


# ------------------------------------------------------------
# Outfit / Props
# ------------------------------------------------------------
def add_hat(obj_name, cfg, hat_type):
    """Add a hat/helmet on top of the head."""
    if hat_type == "top_hat":
        bpy.ops.mesh.primitive_cylinder_add(radius=0.15, depth=0.15, location=(0, 0, 2.12))
        brim = bpy.context.active_object
        brim.name = f"{obj_name}_TopHatBrim"
        set_color(brim, (0.10, 0.10, 0.12))
        bpy.ops.mesh.primitive_cylinder_add(radius=0.10, depth=0.22, location=(0, 0, 2.25))
        top = bpy.context.active_object
        top.name = f"{obj_name}_TopHat"
        set_color(top, (0.10, 0.10, 0.12))

    elif hat_type == "cowboy_hat":
        bpy.ops.mesh.primitive_cylinder_add(radius=0.20, depth=0.03, location=(0, 0, 2.05))
        brim = bpy.context.active_object
        brim.name = f"{obj_name}_CowboyBrim"
        set_color(brim, cfg["accent"])
        bpy.ops.mesh.primitive_cone_add(radius1=0.13, radius2=0.06, depth=0.15, location=(0, 0, 2.12))
        top = bpy.context.active_object
        top.name = f"{obj_name}_CowboyTop"
        set_color(top, cfg["accent"])
        # Give it a slight tilt
        top.rotation_euler[0] = 0.15

    elif hat_type == "crown":
        bpy.ops.mesh.primitive_cylinder_add(radius=0.12, depth=0.05, location=(0, 0, 2.08))
        base = bpy.context.active_object
        base.name = f"{obj_name}_CrownBase"
        set_color(base, cfg["primary"])
        # Spikes
        for i in range(5):
            bpy.ops.mesh.primitive_cone_add(radius1=0.02, radius2=0.0, depth=0.10,
                                            location=(0, 0, 2.15))
            spike = bpy.context.active_object
            spike.name = f"{obj_name}_CrownSpike_{i}"
            spike.rotation_euler[2] = i * (6.283 / 5)
            spike.location = (0.10 * (0.7 if i % 2 else 1.0) * (1 if i % 2 else -1),
                              0.0,
                              2.1)
            set_color(spike, cfg["primary"])

    elif hat_type == "diving_mask":
        bpy.ops.mesh.primitive_torus_add(major_radius=0.12, minor_radius=0.02, location=(0, 0.17, 1.9))
        mask = bpy.context.active_object
        mask.name = f"{obj_name}_DiveMask"
        set_color(mask, cfg["accent"])
        bpy.ops.mesh.primitive_cylinder_add(radius=0.05, depth=0.05, location=(0, 0.17, 1.9))
        glass = bpy.context.active_object
        glass.name = f"{obj_name}_DiveGlass"
        glass.rotation_euler[1] = 1.5708
        set_color(glass, cfg["accent"], emission=cfg["accent"], emission_strength=0.5)

    elif hat_type == "fez":
        bpy.ops.mesh.primitive_cylinder_add(radius=0.12, depth=0.12, location=(0, 0, 2.08))
        fez = bpy.context.active_object
        fez.name = f"{obj_name}_Fez"
        set_color(fez, (0.90, 0.15, 0.15))
        # Tassle
        bpy.ops.mesh.primitive_cylinder_add(radius=0.02, depth=0.12, location=(0, 0, 2.18))
        tassle = bpy.context.active_object
        tassle.name = f"{obj_name}_FezTassle"
        set_color(tassle, (0.95, 0.85, 0.10))

    elif hat_type == "cherry_blossom":
        # Small flower on head
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.03, location=(0.05, 0.0, 2.10))
        flower = bpy.context.active_object
        flower.name = f"{obj_name}_Flower"
        set_color(flower, (0.95, 0.55, 0.70))

    elif hat_type == "bamboo_hat":
        # BoPanda: coolie bamboo hat
        bpy.ops.mesh.primitive_cone_add(radius1=0.22, radius2=0.05, depth=0.10, location=(0, 0, 2.05))
        hat = bpy.context.active_object
        hat.name = f"{obj_name}_BambooHat"
        hat.rotation_euler[1] = 3.14159
        set_color(hat, (0.80, 0.65, 0.35))
        bpy.ops.mesh.primitive_cylinder_add(radius=0.24, depth=0.02, location=(0, 0, 2.09))
        brim = bpy.context.active_object
        brim.name = f"{obj_name}_BambooHatBrim"
        set_color(brim, (0.80, 0.65, 0.35))

    elif hat_type == "bowler_hat":
        # UncleNok: classic bowler hat (grey)
        bpy.ops.mesh.primitive_cylinder_add(radius=0.16, depth=0.03, location=(0, 0, 2.10))
        brim = bpy.context.active_object
        brim.name = f"{obj_name}_BowlerBrim"
        set_color(brim, (0.35, 0.35, 0.38))
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.12, location=(0, 0, 2.22))
        dome = bpy.context.active_object
        dome.name = f"{obj_name}_BowlerDome"
        dome.scale = (1, 1, 0.75)
        set_color(dome, (0.35, 0.35, 0.38))


def add_extra_props(obj_name, cfg):
    """Add extra props based on cfg['extra']."""
    extra = cfg.get("extra", [])

    if "gold_chain" in extra:
        # Neck chain (torus)
        bpy.ops.mesh.primitive_torus_add(major_radius=0.25, minor_radius=0.015, location=(0, 0, 1.55))
        chain = bpy.context.active_object
        chain.name = f"{obj_name}_GoldChain"
        set_color(chain, (0.95, 0.85, 0.10), metallic=1.0)

    if "flower" in extra:
        # Small flower on head (already handled by cherry_blossom)
        pass

    if "bamboo" in extra:
        # Bamboo stick (cylinder)
        bpy.ops.mesh.primitive_cylinder_add(radius=0.03, depth=1.0, location=(0.5, 0, 1.2))
        bamboo = bpy.context.active_object
        bamboo.name = f"{obj_name}_Bamboo"
        bamboo.rotation_euler[0] = 0.8
        set_color(bamboo, (0.30, 0.80, 0.30))

    if "oxygen_tank" in extra:
        # Back tank
        bpy.ops.mesh.primitive_cylinder_add(radius=0.10, depth=0.5, location=(0, -0.25, 1.3))
        tank = bpy.context.active_object
        tank.name = f"{obj_name}_Tank"
        tank.scale = (1, 1, 1.4)
        set_color(tank, cfg["accent"])
        # Hose (torus on shoulder)
        bpy.ops.mesh.primitive_torus_add(major_radius=0.15, minor_radius=0.015, location=(0.1, 0.15, 1.6))
        hose = bpy.context.active_object
        hose.name = f"{obj_name}_Hose"
        hose.rotation_euler[1] = 1.5708
        set_color(hose, cfg["accent"])

    if "fins" in extra:
        # Leg fins (flat cones on feet)
        for side, sign in [('L', -0.14), ('R', 0.14)]:
            bpy.ops.mesh.primitive_cone_add(radius1=0.08, radius2=0.0, depth=0.3,
                                            location=(sign, 0.25, 0.12))
            fin = bpy.context.active_object
            fin.name = f"{obj_name}_Fin_{side}"
            fin.rotation_euler[1] = 1.2
            set_color(fin, cfg["accent"])

    if "monocle" in extra:
        # Small gold ring on face
        bpy.ops.mesh.primitive_torus_add(major_radius=0.05, minor_radius=0.005, location=(0.12, 0.28, 1.9))
        mono = bpy.context.active_object
        mono.name = f"{obj_name}_Monocle"
        set_color(mono, (0.95, 0.85, 0.10), metallic=1.0)

    if "revolver" in extra:
        # Simple revolver (two cylinders)
        bpy.ops.mesh.primitive_cylinder_add(radius=0.03, depth=0.30, location=(0.55, 0.1, 1.1))
        barrel = bpy.context.active_object
        barrel.name = f"{obj_name}_RevolverBarrel"
        barrel.rotation_euler[1] = 1.5708
        set_color(barrel, (0.20, 0.20, 0.22), metallic=0.8)
        bpy.ops.mesh.primitive_cylinder_add(radius=0.04, depth=0.08, location=(0.45, 0.1, 1.1))
        grip = bpy.context.active_object
        grip.name = f"{obj_name}_RevolverGrip"
        grip.rotation_euler[1] = 1.5708
        set_color(grip, (0.50, 0.30, 0.10))

    if "chaps" in extra:
        # Leather strips on legs (thin cylinders)
        for side, sign in [('L', -0.14), ('R', 0.14)]:
            for i in range(2):
                bpy.ops.mesh.primitive_cylinder_add(radius=0.025, depth=0.4,
                                                    location=(sign, 0.06 * i, 0.7))
                chap = bpy.context.active_object
                chap.name = f"{obj_name}_Chap_{side}_{i}"
                set_color(chap, (0.72, 0.55, 0.35))

    if "memory_glow" in extra:
        # Glowing gem on head (Tusker's memory)
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.04, location=(0, 0, 2.0))
        gem = bpy.context.active_object
        gem.name = f"{obj_name}_MemoryGem"
        set_color(gem, (0.30, 0.85, 1.0), emission=(0.30, 0.85, 1.0), emission_strength=2.0)

    if "hint_glow" in extra:
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.05, location=(0, 0, 1.35))
        hint = bpy.context.active_object
        hint.name = f"{obj_name}_HintGlow"
        set_color(hint, (0.40, 0.95, 0.40), emission=(0.40, 0.95, 0.40), emission_strength=1.8)

    if "nice_aura" in extra:
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.50, location=(0, 0, 1.1))
        aura = bpy.context.active_object
        aura.name = f"{obj_name}_NiceAura"
        set_color(aura, (0.95, 0.85, 0.50), emission=(0.95, 0.85, 0.50), emission_strength=0.4)

    if "spectral_aura" in extra:
        # Handled in ghost body builder
        pass

    if "glow_eyes" in extra and "eye" not in [p.name for p in [] ]:
        # Ensure eyes glow (skeleton/phantom body builder already did)
        pass

    if "tusks" in extra:
        # Handled in elephant body builder
        pass

    if "ribs" in extra:
        # Handled in skeleton body builder
        pass


def add_outfit_and_props(name, cfg):
    """Add hat + extra props after body is built."""
    hat = cfg.get("hat")
    if hat:
        add_hat(name, cfg, hat)

    add_extra_props(name, cfg)


# ------------------------------------------------------------
# Join all parts into one mesh
# ------------------------------------------------------------
def join_all(name):
    """Join all objects named with prefix <name>_ into a single mesh."""
    objs = [obj for obj in bpy.context.scene.objects if obj.name.startswith(f"{name}_")]
    if not objs:
        print(f"WARNING: No objects found for {name}")
        return None

    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objs:
        obj.select_set(True)
    bpy.ops.object.join()
    return bpy.context.active_object


# ------------------------------------------------------------
# Simple PBR textures (per-character colors baked into albedo)
# ------------------------------------------------------------
def create_textures(name, cfg, out_dir):
    """Generate Albedo/Normal/Roughness PNG textures matching the theme."""
    import numpy as np
    from math import sin, cos, pi

    size = 1024
    primary = cfg["primary"]
    accent = cfg["accent"]
    skin = cfg["skin"]

    # Build a simple pattern: base color + a few diagonal accent stripes
    albedo = np.zeros((size, size, 4), dtype=np.float32)
    for x in range(size):
        for y in range(size):
            u = x / size
            v = y / size
            # Base = primary
            base = primary
            # Stripes on upper third (like a shirt pattern)
            if v > 0.65:
                stripe = (int(u * 8) % 2 == 0)
                base = accent if stripe else primary
            albedo[x, y, 0] = base[0]
            albedo[x, y, 1] = base[1]
            albedo[x, y, 2] = base[2]
            albedo[x, y, 3] = 1.0

    # Normal (flat)
    normal = np.full((size, size, 4), [0.5, 0.5, 1.0, 1.0], dtype=np.float32)

    # Roughness (varied)
    rough = np.zeros((size, size, 4), dtype=np.float32)
    for x in range(size):
        for y in range(size):
            v = y / size
            rough[x, y, 0] = 0.3 if v > 0.65 else 0.6
            rough[x, y, 1] = 0.3 if v > 0.65 else 0.6
            rough[x, y, 2] = 0.3 if v > 0.65 else 0.6
            rough[x, y, 3] = 1.0

    def save_np_image(arr, filename):
        img = bpy.data.images.new(name=filename, width=size, height=size, alpha=True)
        flat = (arr * 255).astype('uint8').flatten()
        img.pixels = [v / 255.0 for v in flat]
        img.filepath_raw = os.path.join(out_dir, f"{filename}.png")
        img.file_format = 'PNG'
        img.save()

    save_np_image(albedo, f"{name}_Albedo")
    save_np_image(normal, f"{name}_Normal")
    save_np_image(rough, f"{name}_Roughness")
    print(f"Textures saved for {name}")


# ------------------------------------------------------------
# Rigify rig
# ------------------------------------------------------------
def add_rigify(name, body_obj):
    """Add a Rigify metarig and generate the final rig."""
    try:
        bpy.ops.preferences.addon_enable(module="rigify")
    except Exception as e:
        print(f"WARNING: Could not enable Rigify addon: {e}")

    # Add metarig
    bpy.ops.object.armature_human_metarig_add()
    metarig = bpy.context.active_object
    metarig.name = f"{name}_Metarig"

    # Align metarig to body
    metarig.location = body_obj.location
    metarig.location.z += 0.5

    # Generate the final rig
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.view_layer.objects.active = metarig
    metarig.select_set(True)
    bpy.ops.object.mode_set(mode='POSE')
    bpy.ops.pose.select_all(action='SELECT')
    bpy.ops.pose.rigify_generate()
    rig = bpy.context.active_object
    rig.name = f"{name}_Rig"

    # Parent body mesh to rig (automatic weights)
    bpy.ops.object.select_all(action='DESELECT')
    body_obj.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')

    return rig


# ------------------------------------------------------------
# FBX Export
# ------------------------------------------------------------
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

    # Copy FBX into Unity Models dir when available
    if os.path.isdir(UNITY_MODELS_DIR):
        dest = os.path.join(UNITY_MODELS_DIR, f"{name}_AAA.fbx")
        try:
            shutil.copy2(export_path, dest)
            print(f"Copied FBX to Unity Models: {dest}")
        except Exception as e:
            print(f"WARNING: Could not copy FBX to Unity: {e}")
    else:
        print(f"INFO: Unity Models dir {UNITY_MODELS_DIR} not found; skip copy.")


# ------------------------------------------------------------
# Per-character generation
# ------------------------------------------------------------
def generate_character(cfg):
    """Generate a single character. Returns True on success."""
    name = cfg["name"]

    print(f"\n{'='*60}")
    print(f"Generating: {name}")
    print(f"{'='*60}")

    # Reset scene
    clean_scene()

    # Build themed body
    body_parts = build_body(name, cfg)
    added_props = add_outfit_and_props(name, cfg)

    # Join all into one mesh
    joined = join_all(name)

    # Add rig
    rig = add_rigify(name, joined)

    # Textures
    export_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Exports")
    if not os.path.exists(export_dir):
        os.makedirs(export_dir)
    create_textures(name, cfg, export_dir)

    # Export
    export_fbx(name, rig, export_dir)

    print(f"✅ {name} generation complete.")
    return True


# ------------------------------------------------------------
# Main
# ------------------------------------------------------------
def main():
    # Optional filter: only regenerate specific characters.
    # Usage: CHARACTERS_ONLY="Gentleman,Finn" blender --background --python ...
    only = os.environ.get("CHARACTERS_ONLY", "").strip()
    only_set = {n.strip() for n in only.split(",") if n.strip()}

    print("\n==========================================")
    print("CueStrike — Create ALL Missing AAA Characters")
    print("==========================================")

    # Enable Rigify (global)
    try:
        bpy.ops.preferences.addon_enable(module="rigify")
        print("Rigify addon enabled.")
    except Exception as e:
        print(f"WARNING: Could not enable Rigify addon: {e}")

    targets = [cfg for cfg in CHARACTERS if not only_set or cfg["name"] in only_set]

    success = 0
    for cfg in targets:
        try:
            if generate_character(cfg):
                success += 1
        except Exception as e:
            print(f"❌ ERROR generating {cfg['name']}: {e}")

    print("\n==========================================")
    print(f"Done! {success}/{len(targets)} characters generated.")
    print("==========================================")

if __name__ == "__main__":
    main()

# Compatibility note for Blender 3.6:
# This script works with Blender 3.6 API (confirmed by create_character_aaa.py).
# Rigify metarig requires the Rigify addon which we enable at start.
