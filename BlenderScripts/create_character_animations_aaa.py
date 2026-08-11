#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Blender script: create_character_animations_aaa.py
Purpose:
    Create 4 character animation clips for CueStrike mascots (UncleNok / BoPanda)
    using the existing AAA character rig (Rigify, 706 bones):
        Idle          - breathing + subtle sway (loop)
        Celebrate     - arms up, hop, head up (victory)
        Disappointed  - head down, shoulders slump
        Speak         - jaw motion + small head bob + arm gesture
    The rig bone names are identical across all characters (verified), and the
    mesh is skinned to DEF-* bones (72 vertex groups), so we pose DEF bones
    directly — one set of clips works for every prefab (Somchay rig variant).

    We only keyframe the bones that move and bake only those (not all 706),
    keeping exported FBX small.

Usage (headless):
    blender --background --python create_character_animations_aaa.py \
        -- "C:/path/to/UncleNok_AAA.fbx" "C:/path/to/Exports/Animations"

Output (per clip, armature-only FBX with baked animation):
    <out>/Idle.fbx, <out>/Celebrate.fbx, <out>/Disappointed.fbx, <out>/Speak.fbx
"""

import bpy
import sys
import os
import math
from mathutils import Quaternion

# Bones that move per clip (DEF bones — direct mesh skin targets)
IDLE_BONES = [
    "DEF-spine.001", "DEF-spine.003", "DEF-spine.004",
    "DEF-forehead.L", "DEF-forehead.R", "DEF-temple.L", "DEF-temple.R",
    "DEF-shoulder.L", "DEF-shoulder.R",
    "DEF-upper_arm.L", "DEF-upper_arm.R",
]
CELEBRATE_BONES = [
    "DEF-spine", "DEF-spine.001", "DEF-spine.003", "DEF-spine.004", "DEF-spine.006",
    "DEF-shoulder.L", "DEF-shoulder.R",
    "DEF-upper_arm.L", "DEF-upper_arm.R",
    "DEF-forearm.L", "DEF-forearm.R",
    "DEF-forehead.L", "DEF-forehead.R",
]
DISAPPOINTED_BONES = [
    "DEF-spine", "DEF-spine.001", "DEF-spine.003",
    "DEF-shoulder.L", "DEF-shoulder.R",
    "DEF-upper_arm.L", "DEF-upper_arm.R",
    "DEF-forearm.L", "DEF-forearm.R",
    "DEF-forehead.L", "DEF-forehead.R", "DEF-jaw",
]
SPEAK_BONES = [
    "DEF-jaw", "DEF-jaw.L", "DEF-jaw.R",
    "DEF-forehead.L", "DEF-forehead.R",
    "DEF-nose", "DEF-nose.001",
    "DEF-shoulder.R", "DEF-upper_arm.R", "DEF-forearm.R",
]


def quat(x=0.0, y=0.0, z=0.0):
    """Quaternion from Euler degrees (X=pitch, Y=yaw, Z=roll)."""
    ex, ey, ez = math.radians(x), math.radians(y), math.radians(z)
    qx = Quaternion((math.cos(ex / 2), math.sin(ex / 2), 0, 0))
    qy = Quaternion((math.cos(ey / 2), 0, math.sin(ey / 2), 0))
    qz = Quaternion((math.cos(ez / 2), 0, 0, math.sin(ez / 2)))
    return qy @ qx @ qz


def clear_pose(arm):
    for pb in arm.pose.bones:
        pb.rotation_quaternion = (1.0, 0.0, 0.0, 0.0)
        pb.location = (0.0, 0.0, 0.0)


def prune_to(arm, bones):
    """Delete all armature bones not in `bones` (keep hierarchy paths intact).

    The DEF bones we animate sit under long Rigify chains (MCH-/ORG- bones).
    Unity matches animation curves by transform path, so we keep every ancestor
    of each kept bone and delete the rest. This shrinks the exported FBX from
    706 bones to ~30-50 while preserving identical paths.
    """
    keep = set(bones)
    changed = True
    while changed:
        changed = False
        for b in arm.data.bones:
            if b.name in keep and b.parent is not None and b.parent.name not in keep:
                keep.add(b.parent.name)
                changed = True
    to_delete = [b for b in arm.data.bones if b.name not in keep]
    if not to_delete:
        return
    bpy.ops.object.mode_set(mode='EDIT')
    for b in to_delete:
        eb = arm.data.edit_bones.get(b.name)
        if eb is not None:
            arm.data.edit_bones.remove(eb)
    bpy.ops.object.mode_set(mode='OBJECT')
    print(f"  pruned armature: {len(arm.data.bones)} bones kept (of {len(bones) + len(to_delete)} original path set)")


def set_pose(arm, transforms):
    """Apply {bone_name: quat}. Only keyframed bones move."""
    clear_pose(arm)
    for name, q in transforms.items():
        pb = arm.pose.bones.get(name)
        if pb is None:
            print(f"  WARN: bone '{name}' not found, skipped")
            continue
        pb.rotation_quaternion = q


def keyframe_bones(arm, bones, frame):
    for name in bones:
        pb = arm.pose.bones.get(name)
        if pb is not None:
            pb.keyframe_insert(data_path="rotation_quaternion", frame=frame)


# ------------------------------------------------------------
# Action builders — each returns (bones, total_frames)
# ------------------------------------------------------------
def build_idle(arm, fps):
    bones = IDLE_BONES
    total = int(3.0 * fps)
    for f in range(0, total + 1):
        t = f / fps
        breath = math.sin(2 * math.pi * t / 2.0)          # 2s breath cycle
        sway = math.sin(2 * math.pi * t / 4.0)            # 4s gentle sway
        set_pose(arm, {
            "DEF-spine.001": quat(x=breath * 2.5),
            "DEF-spine.003": quat(x=breath * 1.5),
            "DEF-spine.004": quat(x=breath * 0.8),
            "DEF-forehead.L": quat(y=sway * 1.2),
            "DEF-forehead.R": quat(y=sway * 1.2),
            "DEF-temple.L": quat(y=sway * 0.8),
            "DEF-temple.R": quat(y=sway * 0.8),
            "DEF-shoulder.L": quat(z=sway * 1.0),
            "DEF-shoulder.R": quat(z=-sway * 1.0),
            "DEF-upper_arm.L": quat(z=sway * 1.5),
            "DEF-upper_arm.R": quat(z=-sway * 1.5),
        })
        keyframe_bones(arm, bones, f + 1)
    return bones, total + 1


def build_celebrate(arm, fps):
    bones = CELEBRATE_BONES
    total = int(2.0 * fps)
    # Arms raised + hop, head up
    keyframes = {
        1: {"DEF-spine": quat(x=-4), "DEF-spine.001": quat(x=-3),
            "DEF-upper_arm.L": quat(x=-110, z=15), "DEF-upper_arm.R": quat(x=-110, z=-15),
            "DEF-forearm.L": quat(x=-80), "DEF-forearm.R": quat(x=-80),
            "DEF-shoulder.L": quat(x=-20, z=10), "DEF-shoulder.R": quat(x=-20, z=-10),
            "DEF-forehead.L": quat(x=-10), "DEF-forehead.R": quat(x=-10)},
        int(0.4 * fps): {"DEF-spine": quat(x=-8), "DEF-spine.001": quat(x=-6),
            "DEF-spine.003": quat(x=-4), "DEF-spine.006": quat(x=-2),
            "DEF-upper_arm.L": quat(x=-140, z=25), "DEF-upper_arm.R": quat(x=-140, z=-25),
            "DEF-forearm.L": quat(x=-70), "DEF-forearm.R": quat(x=-70),
            "DEF-shoulder.L": quat(x=-25, z=15), "DEF-shoulder.R": quat(x=-25, z=-15),
            "DEF-forehead.L": quat(x=-18), "DEF-forehead.R": quat(x=-18)},
        int(0.9 * fps): {"DEF-spine": quat(x=-4), "DEF-spine.001": quat(x=-3),
            "DEF-upper_arm.L": quat(x=-110, z=15), "DEF-upper_arm.R": quat(x=-110, z=-15),
            "DEF-forearm.L": quat(x=-80), "DEF-forearm.R": quat(x=-80),
            "DEF-shoulder.L": quat(x=-20, z=10), "DEF-shoulder.R": quat(x=-20, z=-10),
            "DEF-forehead.L": quat(x=-10), "DEF-forehead.R": quat(x=-10)},
        int(1.4 * fps): {"DEF-spine": quat(x=-6), "DEF-spine.001": quat(x=-5),
            "DEF-upper_arm.L": quat(x=-120, z=20), "DEF-upper_arm.R": quat(x=-120, z=-20),
            "DEF-forearm.L": quat(x=-75), "DEF-forearm.R": quat(x=-75),
            "DEF-shoulder.L": quat(x=-22, z=12), "DEF-shoulder.R": quat(x=-22, z=-12),
            "DEF-forehead.L": quat(x=-14), "DEF-forehead.R": quat(x=-14)},
    }
    last = keyframes[1]
    for f in range(1, total + 1):
        pose = keyframes.get(f, last)
        if f in keyframes:
            last = pose
        set_pose(arm, pose)
        keyframe_bones(arm, bones, f)
    return bones, total


def build_disappointed(arm, fps):
    bones = DISAPPOINTED_BONES
    total = int(2.0 * fps)
    keyframes = {
        1: {"DEF-spine": quat(x=6), "DEF-spine.001": quat(x=5),
            "DEF-shoulder.L": quat(x=15, z=8), "DEF-shoulder.R": quat(x=15, z=-8),
            "DEF-upper_arm.L": quat(x=25, z=10), "DEF-upper_arm.R": quat(x=25, z=-10),
            "DEF-forearm.L": quat(x=35), "DEF-forearm.R": quat(x=35),
            "DEF-forehead.L": quat(x=18, y=4), "DEF-forehead.R": quat(x=18, y=4),
            "DEF-jaw": quat(x=5)},
        int(0.6 * fps): {"DEF-spine": quat(x=9), "DEF-spine.001": quat(x=8),
            "DEF-shoulder.L": quat(x=20, z=10), "DEF-shoulder.R": quat(x=20, z=-10),
            "DEF-upper_arm.L": quat(x=30, z=12), "DEF-upper_arm.R": quat(x=30, z=-12),
            "DEF-forearm.L": quat(x=40), "DEF-forearm.R": quat(x=40),
            "DEF-forehead.L": quat(x=25, y=6), "DEF-forehead.R": quat(x=25, y=6),
            "DEF-jaw": quat(x=8)},
        int(1.2 * fps): {"DEF-spine": quat(x=8), "DEF-spine.001": quat(x=7),
            "DEF-shoulder.L": quat(x=18, z=9), "DEF-shoulder.R": quat(x=18, z=-9),
            "DEF-upper_arm.L": quat(x=28, z=11), "DEF-upper_arm.R": quat(x=28, z=-11),
            "DEF-forearm.L": quat(x=38), "DEF-forearm.R": quat(x=38),
            "DEF-forehead.L": quat(x=22, y=-4), "DEF-forehead.R": quat(x=22, y=-4),
            "DEF-jaw": quat(x=6)},
        int(1.8 * fps): {"DEF-spine": quat(x=6), "DEF-spine.001": quat(x=5),
            "DEF-shoulder.L": quat(x=15, z=8), "DEF-shoulder.R": quat(x=15, z=-8),
            "DEF-upper_arm.L": quat(x=25, z=10), "DEF-upper_arm.R": quat(x=25, z=-10),
            "DEF-forearm.L": quat(x=35), "DEF-forearm.R": quat(x=35),
            "DEF-forehead.L": quat(x=18, y=4), "DEF-forehead.R": quat(x=18, y=4),
            "DEF-jaw": quat(x=5)},
    }
    last = keyframes[1]
    for f in range(1, total + 1):
        pose = keyframes.get(f, last)
        if f in keyframes:
            last = pose
        set_pose(arm, pose)
        keyframe_bones(arm, bones, f)
    return bones, total


def build_speak(arm, fps):
    bones = SPEAK_BONES
    total = int(2.0 * fps)
    for f in range(1, total + 1):
        t = (f - 1) / fps
        jaw_open = max(0.0, math.sin(2 * math.pi * t * 2.2))   # 2.2 Hz chatter
        bob = math.sin(2 * math.pi * t * 2.0) * 2.0
        gesture = math.sin(2 * math.pi * t / 1.0) * 6.0
        set_pose(arm, {
            "DEF-jaw": quat(x=jaw_open * 18.0),
            "DEF-jaw.L": quat(x=jaw_open * 10.0),
            "DEF-jaw.R": quat(x=jaw_open * 10.0),
            "DEF-forehead.L": quat(y=bob * 0.6),
            "DEF-forehead.R": quat(y=bob * 0.6),
            "DEF-nose": quat(y=bob * 0.5),
            "DEF-nose.001": quat(y=bob * 0.5),
            "DEF-shoulder.R": quat(z=-gesture),
            "DEF-upper_arm.R": quat(x=-15, z=-gesture),
            "DEF-forearm.R": quat(x=gesture * 2.0),
        })
        keyframe_bones(arm, bones, f)
    return bones, total


BUILDERS = {
    "Idle": build_idle,
    "Celebrate": build_celebrate,
    "Disappointed": build_disappointed,
    "Speak": build_speak,
}


def main():
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1:]
    if len(argv) < 2:
        print("Usage: blender --background --python create_character_animations_aaa.py -- <fbx> <out_dir>")
        sys.exit(1)

    fbx_path = os.path.abspath(argv[0])
    out_dir = os.path.abspath(argv[1])
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx_path)

    arm = next((o for o in bpy.context.scene.objects if o.type == 'ARMATURE'), None)
    if arm is None:
        print("ERROR: No armature found in FBX")
        sys.exit(1)
    print(f"Rig: {arm.name} ({len(arm.data.bones)} bones)")

    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')
    fps = bpy.context.scene.render.fps

    for name, builder in BUILDERS.items():
        action = bpy.data.actions.new(name)
        action.use_fake_user = True
        arm.animation_data_create()
        arm.animation_data.action = action

        clear_pose(arm)
        bpy.context.scene.frame_start = 1
        bones, frame_end = builder(arm, fps)
        bpy.context.scene.frame_end = frame_end
        print(f"Action '{name}': frames 1..{frame_end} on {len(bones)} bones")

        prune_to(arm, bones)

        bpy.ops.object.mode_set(mode='OBJECT')
        bpy.ops.object.select_all(action='DESELECT')
        arm.select_set(True)
        bpy.context.view_layer.objects.active = arm
        out_path = os.path.join(out_dir, f"{name}.fbx")
        bpy.ops.export_scene.fbx(
            filepath=out_path,
            use_selection=True,
            apply_unit_scale=True,
            bake_space_transform=True,
            object_types={'ARMATURE'},
            bake_anim=True,
            bake_anim_use_all_bones=False,   # only keyframed bones
            bake_anim_simplify_factor=1.0,
            add_leaf_bones=False,
            axis_forward='-Z',
            axis_up='Y',
        )
        print(f"Exported: {out_path}")
        bpy.ops.object.mode_set(mode='POSE')

        # Restore full rig for next action: back to OBJECT mode, delete, re-import.
        bpy.ops.object.mode_set(mode='OBJECT')
        bpy.ops.object.select_all(action='SELECT')
        bpy.ops.object.delete(use_global=False)
        bpy.ops.import_scene.fbx(filepath=fbx_path)
        arm = next((o for o in bpy.context.scene.objects if o.type == 'ARMATURE'), None)
        bpy.context.view_layer.objects.active = arm
        bpy.ops.object.mode_set(mode='POSE')

    print("DONE: 4 animation clips exported.")


if __name__ == "__main__":
    main()
