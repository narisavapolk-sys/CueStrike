# CueStrike Title Screen Generator - Blender Python Script
# Run in Blender: Text Editor -> Run Script
# Generates: Animated "CUE STRIKE" title screen with billiard-themed visuals

import bpy
import bmesh
import math
import random
from mathutils import Vector, Euler, Color

# ============================================================
# CONFIGURATION
# ============================================================
TITLE_TEXT = "CUE STRIKE"
SUBTITLE_TEXT = "PRECISION • STRATEGY • STYLE"
FRAME_START = 1
FRAME_END = 180  # 6 seconds at 30fps
FPS = 30
RESOLUTION_X = 1920
RESOLUTION_Y = 1080

# Color palette (billiard-themed)
FELT_GREEN = (0.05, 0.35, 0.15, 1.0)
GOLD = (1.0, 0.8, 0.1, 1.0)
WHITE_BALL = (0.98, 0.98, 0.95, 1.0)
CUE_WOOD = (0.4, 0.25, 0.15, 1.0)
CHALK_BLUE = (0.1, 0.3, 0.8, 1.0)
RED_BALL = (0.85, 0.1, 0.1, 1.0)
BLACK_8BALL = (0.05, 0.05, 0.05, 1.0)

# Output
OUTPUT_DIR = "//renders/title_screen/"
RENDER_NAME = "CUE_STRIKE_Title"

# ============================================================
# CLEANUP & SETUP
# ============================================================
def clean_scene():
    """Remove all objects, materials, collections from scene"""
    # Delete all objects
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    
    # Remove orphaned data
    for block in bpy.data.meshes:
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in bpy.data.materials:
        if block.users == 0:
            bpy.data.materials.remove(block)
    for block in bpy.data.curves:
        if block.users == 0:
            bpy.data.curves.remove(block)
    for block in bpy.data.collections:
        if block.users == 0:
            bpy.data.collections.remove(block)

def setup_scene():
    """Configure render settings"""
    scene = bpy.context.scene
    scene.frame_start = FRAME_START
    scene.frame_end = FRAME_END
    scene.render.fps = FPS
    scene.render.resolution_x = RESOLUTION_X
    scene.render.resolution_y = RESOLUTION_Y
    scene.render.resolution_percentage = 100
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 128
    scene.cycles.use_denoising = True
    scene.render.film_transparent = True
    scene.render.filepath = OUTPUT_DIR + RENDER_NAME
    scene.render.image_settings.file_format = 'FFMPEG'
    scene.render.ffmpeg.format = 'MPEG4'
    scene.render.ffmpeg.codec = 'H264'
    scene.render.ffmpeg.constant_rate_factor = 'PERC_LOSSLESS'
    scene.render.ffmpeg.ffmpeg_preset = 'GOOD'
    
    # Color management
    scene.view_settings.view_transform = 'Filmic'
    scene.view_settings.look = 'Medium High Contrast'
    scene.view_settings.exposure = 0.5
    scene.view_settings.gamma = 1.0

def setup_world():
    """Create world environment"""
    world = bpy.data.worlds.new("TitleWorld")
    bpy.context.scene.world = world
    world.use_nodes = True
    nodes = world.node_tree.nodes
    links = world.node_tree.links
    nodes.clear()
    
    # Background
    bg = nodes.new('ShaderNodeBackground')
    bg.inputs['Color'].default_value = (0.02, 0.02, 0.03, 1.0)
    bg.inputs['Strength'].default_value = 0.1
    
    # Output
    output = nodes.new('ShaderNodeOutputWorld')
    links.new(bg.outputs['Background'], output.inputs['Surface'])
    
    bg.location = (-200, 0)
    output.location = (0, 0)

# ============================================================
# MATERIALS
# ============================================================
def create_material(name, base_color, metallic=0.0, roughness=0.5, emission=None, emission_strength=0):
    """Create a Principled BSDF material"""
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Base Color'].default_value = base_color
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Specular'].default_value = 0.5
    
    if emission:
        bsdf.inputs['Emission Color'].default_value = emission
        bsdf.inputs['Emission Strength'].default_value = emission_strength
    
    output = nodes.new('ShaderNodeOutputMaterial')
    links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    
    bsdf.location = (-200, 0)
    output.location = (0, 0)
    return mat

def create_felt_material():
    """Pool table felt material with subtle texture"""
    mat = bpy.data.materials.new(name="Felt_Green")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    
    # Coordinates
    tex_coord = nodes.new('ShaderNodeTexCoord')
    mapping = nodes.new('ShaderNodeMapping')
    mapping.inputs['Scale'].default_value = (50, 50, 50)
    
    # Noise for felt texture
    noise = nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = 200.0
    noise.inputs['Detail'].default_value = 8.0
    noise.inputs['Roughness'].default_value = 0.7
    noise.inputs['Distortion'].default_value = 0.3
    
    # Color ramp for felt fibers
    ramp = nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = 0.4
    ramp.color_ramp.elements[0].color = (0.04, 0.3, 0.12, 1.0)
    ramp.color_ramp.elements[1].position = 0.6
    ramp.color_ramp.elements[1].color = (0.06, 0.4, 0.18, 1.0)
    
    # Bump
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = 0.02
    bump.inputs['Distance'].default_value = 0.001
    
    # Principled
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value = 0.9
    bsdf.inputs['Specular'].default_value = 0.1
    
    output = nodes.new('ShaderNodeOutputMaterial')
    
    # Connections
    links.new(tex_coord.outputs['Object'], mapping.inputs['Vector'])
    links.new(mapping.outputs['Vector'], noise.inputs['Vector'])
    links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], bsdf.inputs['Base Color'])
    links.new(noise.outputs['Fac'], bump.inputs['Height'])
    links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    
    # Layout
    tex_coord.location = (-600, 0)
    mapping.location = (-400, 0)
    noise.location = (-200, 100)
    ramp.location = (0, 100)
    bump.location = (0, -100)
    bsdf.location = (200, 0)
    output.location = (400, 0)
    
    return mat

def create_ball_material(ball_type):
    """Create billiard ball material based on type"""
    if ball_type == "cue":
        return create_material("Ball_Cue", WHITE_BALL, roughness=0.05, metallic=0.02)
    elif ball_type == "8ball":
        mat = create_material("Ball_8Ball", BLACK_8BALL, roughness=0.05, metallic=0.02)
        return mat
    elif ball_type == "red":
        return create_material("Ball_Red", RED_BALL, roughness=0.05, metallic=0.02)
    elif ball_type == "yellow":
        return create_material("Ball_Yellow", (1.0, 0.9, 0.1, 1.0), roughness=0.05, metallic=0.02)
    elif ball_type == "blue":
        return create_material("Ball_Blue", (0.1, 0.3, 0.9, 1.0), roughness=0.05, metallic=0.02)
    elif ball_type == "purple":
        return create_material("Ball_Purple", (0.5, 0.1, 0.7, 1.0), roughness=0.05, metallic=0.02)
    elif ball_type == "orange":
        return create_material("Ball_Orange", (1.0, 0.5, 0.0, 1.0), roughness=0.05, metallic=0.02)
    elif ball_type == "green":
        return create_material("Ball_Green", (0.1, 0.7, 0.2, 1.0), roughness=0.05, metallic=0.02)
    elif ball_type == "brown":
        return create_material("Ball_Brown", (0.5, 0.25, 0.1, 1.0), roughness=0.05, metallic=0.02)
    else:
        return create_material("Ball_Default", WHITE_BALL, roughness=0.05)

def create_cue_material():
    """Wood cue material"""
    mat = bpy.data.materials.new(name="Cue_Wood")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    
    # Wood texture using noise
    tex_coord = nodes.new('ShaderNodeTexCoord')
    mapping = nodes.new('ShaderNodeMapping')
    mapping.inputs['Scale'].default_value = (1, 20, 1)
    
    noise = nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = 50.0
    noise.inputs['Detail'].default_value = 10.0
    noise.inputs['Roughness'].default_value = 0.6
    noise.inputs['Distortion'].default_value = 0.2
    
    ramp = nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = 0.3
    ramp.color_ramp.elements[0].color = (0.35, 0.2, 0.1, 1.0)
    ramp.color_ramp.elements[1].position = 0.7
    ramp.color_ramp.elements[1].color = (0.45, 0.3, 0.18, 1.0)
    
    # Bump for wood grain
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = 0.05
    
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value = 0.4
    bsdf.inputs['Specular'].default_value = 0.3
    bsdf.inputs['Anisotropic'].default_value = 0.3
    bsdf.inputs['Anisotropic Rotation'].default_value = 1.57  # Along length
    
    output = nodes.new('ShaderNodeOutputMaterial')
    
    links.new(tex_coord.outputs['Object'], mapping.inputs['Vector'])
    links.new(mapping.outputs['Vector'], noise.inputs['Vector'])
    links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], bsdf.inputs['Base Color'])
    links.new(noise.outputs['Fac'], bump.inputs['Height'])
    links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    
    tex_coord.location = (-600, 0)
    mapping.location = (-400, 0)
    noise.location = (-200, 50)
    ramp.location = (0, 50)
    bump.location = (0, -50)
    bsdf.location = (200, 0)
    output.location = (400, 0)
    
    return mat

def create_gold_material():
    """Shiny gold material for text accents"""
    return create_material("Gold_Accent", GOLD, metallic=1.0, roughness=0.1, emission=GOLD, emission_strength=0.5)

def create_chalk_material():
    """Blue chalk material"""
    return create_material("Chalk_Blue", CHALK_BLUE, roughness=0.8, metallic=0.0)

# ============================================================
# OBJECT CREATION
# ============================================================
def create_pool_table():
    """Create pool table surface"""
    # Table bed
    bpy.ops.mesh.primitive_plane_add(size=4.0, location=(0, 0, 0))
    table = bpy.context.active_object
    table.name = "Pool_Table"
    
    # Scale to 9-foot table proportions (2.54m x 1.27m)
    table.scale = (2.54, 1.27, 1.0)
    
    # Add thickness
    bpy.ops.object.modifier_add(type='SOLIDIFY')
    table.modifiers["Solidify"].thickness = 0.05
    table.modifiers["Solidify"].offset = 0
    
    # Apply
    bpy.ops.object.modifier_apply(modifier="Solidify")
    
    # Felt material
    felt_mat = create_felt_material()
    table.data.materials.append(felt_mat)
    
    # Add rails
    create_rails()
    
    return table

def create_rails():
    """Create table rails/cushions"""
    rail_height = 0.06
    rail_width = 0.08
    table_w = 2.54
    table_h = 1.27
    
    # Top rail
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, table_h/2 + rail_width/2, rail_height/2))
    top_rail = bpy.context.active_object
    top_rail.name = "Rail_Top"
    top_rail.scale = (table_w + rail_width*2, rail_width, rail_height)
    
    # Bottom rail
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, -table_h/2 - rail_width/2, rail_height/2))
    bot_rail = bpy.context.active_object
    bot_rail.name = "Rail_Bottom"
    bot_rail.scale = (table_w + rail_width*2, rail_width, rail_height)
    
    # Left rail
    bpy.ops.mesh.primitive_cube_add(size=1, location=(-table_w/2 - rail_width/2, 0, rail_height/2))
    left_rail = bpy.context.active_object
    left_rail.name = "Rail_Left"
    left_rail.scale = (rail_width, table_h, rail_height)
    
    # Right rail
    bpy.ops.mesh.primitive_cube_add(size=1, location=(table_w/2 + rail_width/2, 0, rail_height/2))
    right_rail = bpy.context.active_object
    right_rail.name = "Rail_Right"
    right_rail.scale = (rail_width, table_h, rail_height)
    
    # Rail material (darker felt)
    rail_mat = create_material("Rail_Felt", (0.03, 0.2, 0.1, 1.0), roughness=0.8)
    for rail in [top_rail, bot_rail, left_rail, right_rail]:
        rail.data.materials.append(rail_mat)
    
    # Pockets (simple cylinders)
    pocket_positions = [
        (-table_w/2, table_h/2), (0, table_h/2), (table_w/2, table_h/2),
        (-table_w/2, -table_h/2), (0, -table_h/2), (table_w/2, -table_h/2)
    ]
    
    pocket_mat = create_material("Pocket_Black", (0.0, 0.0, 0.0, 1.0), roughness=0.9)
    
    for i, (px, py) in enumerate(pocket_positions):
        bpy.ops.mesh.primitive_cylinder_add(radius=0.07, depth=0.15, location=(px, py, -0.05))
        pocket = bpy.context.active_object
        pocket.name = f"Pocket_{i}"
        pocket.data.materials.append(pocket_mat)

def create_cue_ball():
    """Create the cue ball with number decal"""
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.057, segments=32, ring_count=16, location=(0, -0.3, 0.057))
    ball = bpy.context.active_object
    ball.name = "Cue_Ball"
    ball.data.materials.append(create_ball_material("cue"))
    
    # Add subtle shine
    bpy.ops.object.modifier_add(type='SUBSURF')
    ball.modifiers["Subdivision"].levels = 2
    ball.modifiers["Subdivision"].render_levels = 3
    
    return ball

def create_8ball():
    """Create the 8-ball"""
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.057, segments=32, ring_count=16, location=(0.15, 0.2, 0.057))
    ball = bpy.context.active_object
    ball.name = "Eight_Ball"
    ball.data.materials.append(create_ball_material("8ball"))
    
    bpy.ops.object.modifier_add(type='SUBSURF')
    ball.modifiers["Subdivision"].levels = 2
    ball.modifiers["Subdivision"].render_levels = 3
    
    # Add "8" decal (text on ball surface)
    create_ball_decal(ball, "8", (1.0, 1.0, 1.0, 1.0))
    
    return ball

def create_ball_decal(ball_obj, text, color):
    """Create text decal on ball"""
    bpy.ops.object.text_add(location=ball_obj.location + Vector((0, 0, 0.06)))
    decal = bpy.context.active_object
    decal.name = f"Decal_{ball_obj.name}"
    decal.data.body = text
    decal.data.size = 0.03
    decal.data.align_x = 'CENTER'
    decal.data.align_y = 'CENTER'
    decal.data.extrude = 0.001
    
    decal_mat = create_material(f"Decal_{text}", color, roughness=0.1, metallic=0.5)
    decal.data.materials.append(decal_mat)
    
    # Parent to ball
    decal.parent = ball_obj
    
    return decal

def create_scattered_balls():
    """Create additional balls scattered on table"""
    ball_types = ["red", "yellow", "blue", "purple", "orange", "green", "brown"]
    positions = [
        (-0.4, 0.1), (-0.2, -0.1), (0.3, 0.2), (0.4, -0.2),
        (-0.5, -0.3), (0.5, 0.3), (-0.1, 0.4)
    ]
    
    balls = []
    for i, (pos, btype) in enumerate(zip(positions, ball_types)):
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.057, segments=24, ring_count=12, location=(pos[0], pos[1], 0.057))
        ball = bpy.context.active_object
        ball.name = f"Ball_{btype}_{i}"
        ball.data.materials.append(create_ball_material(btype))
        
        bpy.ops.object.modifier_add(type='SUBSURF')
        ball.modifiers["Subdivision"].levels = 2
        
        balls.append(ball)
    
    return balls

def create_cue_stick():
    """Create pool cue stick"""
    # Main shaft
    bpy.ops.mesh.primitive_cylinder_add(radius=0.015, depth=1.45, location=(0.8, -0.6, 0.7))
    cue = bpy.context.active_object
    cue.name = "Cue_Stick"
    cue.rotation_euler = Euler((0.3, 0, -0.4))
    cue.data.materials.append(create_cue_material())
    
    # Tip (ferrule + tip)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.013, depth=0.025, location=(0.8 - 1.45/2 * 0.9, -0.6 + 1.45/2 * 0.3, 0.7 + 1.45/2 * 0.1))
    tip = bpy.context.active_object
    tip.name = "Cue_Tip"
    tip.rotation_euler = Euler((0.3, 0, -0.4))
    tip.data.materials.append(create_material("Cue_Tip_Mat", (0.9, 0.9, 0.85, 1.0), roughness=0.3))
    tip.parent = cue
    
    # Chalk on table
    bpy.ops.mesh.primitive_cube_add(size=0.035, location=(-0.8, 0.4, 0.03))
    chalk = bpy.context.active_object
    chalk.name = "Chalk_Cube"
    chalk.rotation_euler = Euler((0, 0, 0.3))
    chalk.data.materials.append(create_chalk_material())
    
    return cue

def create_title_text():
    """Create animated CUE STRIKE text"""
    # Main title
    bpy.ops.object.text_add(location=(0, 0, 0.5))
    title = bpy.context.active_object
    title.name = "Title_Main"
    title.data.body = TITLE_TEXT
    title.data.size = 0.35
    title.data.align_x = 'CENTER'
    title.data.align_y = 'CENTER'
    title.data.extrude = 0.03
    title.data.bevel_depth = 0.008
    title.data.bevel_resolution = 4
    
    # Font (use built-in)
    title.data.font = bpy.data.fonts.load("//fonts/Bold.ttf") if "//fonts/Bold.ttf" else None
    
    # Gold material for front face, dark for sides
    gold_mat = create_gold_material()
    dark_mat = create_material("Text_Dark", (0.05, 0.03, 0.01, 1.0), metallic=0.2, roughness=0.4)
    
    title.data.materials.append(gold_mat)
    title.data.materials.append(dark_mat)
    title.data.materials.append(gold_mat)  # Bevel
    
    # Subtitle
    bpy.ops.object.text_add(location=(0, 0, 0.15))
    subtitle = bpy.context.active_object
    subtitle.name = "Title_Subtitle"
    subtitle.data.body = SUBTITLE_TEXT
    subtitle.data.size = 0.08
    subtitle.data.align_x = 'CENTER'
    subtitle.data.align_y = 'CENTER'
    subtitle.data.extrude = 0.01
    subtitle.data.bevel_depth = 0.003
    
    sub_mat = create_material("Subtitle_Gold", GOLD, metallic=0.8, roughness=0.2, emission=GOLD, emission_strength=1.0)
    subtitle.data.materials.append(sub_mat)
    
    return title, subtitle

def create_particle_system():
    """Create floating dust particles / sparkles"""
    bpy.ops.mesh.primitive_plane_add(size=0.01, location=(0, 0, 0.5))
    emitter = bpy.context.active_object
    emitter.name = "Particle_Emitter"
    emitter.scale = (4, 2, 1)
    
    # Particle system
    ps = emitter.modifiers.new(name="Title_Particles", type='PARTICLE_SYSTEM')
    pset = ps.particle_system.settings
    pset.count = 500
    pset.frame_start = FRAME_START - 30
    pset.frame_end = FRAME_END
    pset.lifetime = 60
    pset.emit_from = 'FACE'
    pset.use_emit_random = True
    
    # Physics
    pset.physics_type = 'NEWTON'
    pset.effector_weights.gravity = 0.0
    pset.normal_factor = 0.1
    pset.factor_random = 0.5
    
    # Render
    pset.render_type = 'HALO'
    pset.halo_size = 0.02
    pset.use_render_emission = True
    
    # Particle material
    particle_mat = create_material("Particle_Gold", GOLD, emission=GOLD, emission_strength=5.0, roughness=0.1)
    pset.material = len(emitter.data.materials)
    emitter.data.materials.append(particle_mat)
    
    return emitter

# ============================================================
# LIGHTING
# ============================================================
def create_lighting():
    """Set up dramatic lighting for title screen"""
    lights = []
    
    # Key light - warm from top-left
    bpy.ops.object.light_add(type='AREA', location=(-2, 2, 3))
    key = bpy.context.active_object
    key.name = "Key_Light"
    key.data.energy = 1000
    key.data.color = (1.0, 0.95, 0.85)
    key.data.size = 2.0
    key.data.size_y = 1.5
    key.rotation_euler = Euler((-0.8, 0, 0.5))
    lights.append(key)
    
    # Fill light - cool from right
    bpy.ops.object.light_add(type='AREA', location=(2.5, -1, 2))
    fill = bpy.context.active_object
    fill.name = "Fill_Light"
    fill.data.energy = 300
    fill.data.color = (0.7, 0.8, 1.0)
    fill.data.size = 3.0
    fill.rotation_euler = Euler((-0.6, 0, -0.5))
    lights.append(fill)
    
    # Rim light - gold from behind
    bpy.ops.object.light_add(type='AREA', location=(0, -3, 1.5))
    rim = bpy.context.active_object
    rim.name = "Rim_Light"
    rim.data.energy = 500
    rim.data.color = (1.0, 0.8, 0.2)
    rim.data.size = 2.0
    rim.rotation_euler = Euler((0.3, 0, 0))
    lights.append(rim)
    
    # Accent lights on text
    for i, pos in enumerate([(-1.5, 0, 0.8), (1.5, 0, 0.8)]):
        bpy.ops.object.light_add(type='SPOT', location=pos)
        accent = bpy.context.active_object
        accent.name = f"Accent_Light_{i}"
        accent.data.energy = 200
        accent.data.color = (1.0, 0.9, 0.5)
        accent.data.spot_size = 0.8
        accent.data.spot_blend = 0.5
        accent.rotation_euler = Euler((-0.5, 0, 0 if i == 0 else 3.14))
        lights.append(accent)
    
    return lights

# ============================================================
# CAMERA
# ============================================================
def create_camera():
    """Create and animate camera"""
    bpy.ops.object.camera_add(location=(0, -3.5, 1.2))
    cam = bpy.context.active_object
    cam.name = "Title_Camera"
    cam.rotation_euler = Euler((0.6, 0, 0))
    cam.data.lens = 50
    cam.data.sensor_width = 36
    cam.data.dof.use_dof = True
    cam.data.dof.aperture_fstop = 2.8
    cam.data.dof.focus_distance = 3.5
    
    # Set as active
    bpy.context.scene.camera = cam
    
    return cam

def animate_camera(cam):
    """Animate camera for dramatic intro"""
    # Start: Low, close to table
    cam.location = (0, -2.5, 0.6)
    cam.rotation_euler = (0.8, 0, 0)
    cam.keyframe_insert(data_path="location", frame=FRAME_START)
    cam.keyframe_insert(data_path="rotation_euler", frame=FRAME_START)
    cam.data.keyframe_insert(data_path="dof_focus_distance", frame=FRAME_START)
    
    # Mid: Rise and pull back
    cam.location = (0, -3.5, 1.2)
    cam.rotation_euler = (0.5, 0, 0)
    cam.data.dof.focus_distance = 3.5
    cam.keyframe_insert(data_path="location", frame=FRAME_START + 60)
    cam.keyframe_insert(data_path="rotation_euler", frame=FRAME_START + 60)
    cam.data.keyframe_insert(data_path="dof_focus_distance", frame=FRAME_START + 60)
    
    # End: Slight drift
    cam.location = (0.2, -4, 1.5)
    cam.rotation_euler = (0.4, 0, 0.05)
    cam.data.dof.focus_distance = 4.0
    cam.keyframe_insert(data_path="location", frame=FRAME_END)
    cam.keyframe_insert(data_path="rotation_euler", frame=FRAME_END)
    cam.data.keyframe_insert(data_path="dof_focus_distance", frame=FRAME_END)
    
    # Smooth interpolation
    for fcu in cam.animation_data.action.fcurves:
        for kf in fcu.keyframe_points:
            kf.interpolation = 'BEZIER'
            kf.handle_left_type = 'AUTO_CLAMPED'
            kf.handle_right_type = 'AUTO_CLAMPED'

# ============================================================
# ANIMATION
# ============================================================
def animate_title_text(title, subtitle):
    """Animate text entrance"""
    # Title: Scale up from 0 with bounce
    title.scale = (0, 0, 0)
    title.keyframe_insert(data_path="scale", frame=FRAME_START)
    
    title.scale = (1.2, 1.2, 1.2)
    title.keyframe_insert(data_path="scale", frame=FRAME_START + 30)
    
    title.scale = (0.95, 0.95, 0.95)
    title.keyframe_insert(data_path="scale", frame=FRAME_START + 40)
    
    title.scale = (1.0, 1.0, 1.0)
    title.keyframe_insert(data_path="scale", frame=FRAME_START + 50)
    
    # Subtitle: Fade in
    subtitle.hide_viewport = True
    subtitle.hide_render = True
    subtitle.keyframe_insert(data_path="hide_viewport", frame=FRAME_START)
    subtitle.keyframe_insert(data_path="hide_render", frame=FRAME_START)
    
    subtitle.hide_viewport = False
    subtitle.hide_render = False
    subtitle.keyframe_insert(data_path="hide_viewport", frame=FRAME_START + 45)
    subtitle.keyframe_insert(data_path="hide_render", frame=FRAME_START + 45)
    
    subtitle.scale = (0, 0, 0)
    subtitle.keyframe_insert(data_path="scale", frame=FRAME_START + 45)
    
    subtitle.scale = (1.0, 1.0, 1.0)
    subtitle.keyframe_insert(data_path="scale", frame=FRAME_START + 65)
    
    # Subtle floating animation for both
    for obj, offset in [(title, 0), (subtitle, 10)]:
        for frame in range(FRAME_START + 60, FRAME_END, 30):
            obj.location.z += 0.01 * math.sin(frame * 0.1)
            obj.keyframe_insert(data_path="location", frame=frame)
            obj.location.z -= 0.01 * math.sin(frame * 0.1)
            obj.keyframe_insert(data_path="location", frame=frame + 15)

def animate_balls(cue_ball, eight_ball, scattered_balls):
    """Animate balls - subtle movement"""
    # Cue ball: gentle roll into position
    cue_ball.location = (0, -0.8, 0.057)
    cue_ball.keyframe_insert(data_path="location", frame=FRAME_START)
    
    cue_ball.location = (0, -0.3, 0.057)
    cue_ball.keyframe_insert(data_path="location", frame=FRAME_START + 40)
    
    # Add rotation for rolling
    cue_ball.rotation_euler = (0, 0, 0)
    cue_ball.keyframe_insert(data_path="rotation_euler", frame=FRAME_START)
    cue_ball.rotation_euler = (4.0, 0, 0)  # ~2 rotations
    cue_ball.keyframe_insert(data_path="rotation_euler", frame=FRAME_START + 40)
    
    # 8-ball: subtle idle
    for frame in range(FRAME_START, FRAME_END, 40):
        eight_ball.location.x += 0.01 * math.sin(frame * 0.1)
        eight_ball.keyframe_insert(data_path="location", frame=frame)
        eight_ball.location.x -= 0.01 * math.sin(frame * 0.1)
        eight_ball.keyframe_insert(data_path="location", frame=frame + 20)
    
    # Scattered balls: very subtle breathing
    for i, ball in enumerate(scattered_balls):
        orig_z = ball.location.z
        for frame in range(FRAME_START, FRAME_END, 60):
            ball.location.z = orig_z + 0.002 * math.sin(frame * 0.05 + i)
            ball.keyframe_insert(data_path="location", frame=frame)

def animate_cue(cue):
    """Animate cue stick - subtle sway"""
    orig_loc = cue.location.copy()
    orig_rot = cue.rotation_euler.copy()
    
    for frame in range(FRAME_START, FRAME_END, 50):
        cue.location = orig_loc + Vector((0.02 * math.sin(frame * 0.05), 0.01 * math.cos(frame * 0.03), 0))
        cue.rotation_euler = orig_rot + Euler((0.02 * math.sin(frame * 0.07), 0, 0.01 * math.cos(frame * 0.05)))
        cue.keyframe_insert(data_path="location", frame=frame)
        cue.keyframe_insert(data_path="rotation_euler", frame=frame)
    
    # Return to original
    cue.location = orig_loc
    cue.rotation_euler = orig_rot
    cue.keyframe_insert(data_path="location", frame=FRAME_END)
    cue.keyframe_insert(data_path="rotation_euler", frame=FRAME_END)

def animate_lights(lights):
    """Animate light intensity for mood"""
    for light in lights:
        if light.data.type == 'AREA':
            base_energy = light.data.energy
            for frame in range(FRAME_START, FRAME_END, 30):
                light.data.energy = base_energy * (1 + 0.1 * math.sin(frame * 0.1))
                light.data.keyframe_insert(data_path="energy", frame=frame)

# ============================================================
# COMPOSITING
# ============================================================
def setup_compositing():
    """Set up compositor for final look"""
    scene = bpy.context.scene
    scene.use_nodes = True
    tree = scene.node_tree
    nodes = tree.nodes
    links = tree.links
    nodes.clear()
    
    # Render layers
    rl = nodes.new('CompositorNodeRLayers')
    
    # Glare for gold text
    glare = nodes.new('CompositorNodeGlare')
    glare.glare_type = 'FOG_GLOW'
    glare.quality = 'HIGH'
    glare.size = 7
    glare.mix = 0.3
    glare.threshold = 0.8
    
    # Color balance
    colorbal = nodes.new('CompositorNodeColorBalance')
    colorbal.correction_method = 'LIFT_GAMMA_GAIN'
    # Slight teal/orange grade
    colorbal.lift = [0.95, 0.98, 1.05]
    colorbal.gamma = [1.02, 1.0, 0.98]
    colorbal.gain = [1.05, 1.0, 0.95]
    
    # Vignette
    vignette = nodes.new('CompositorNodeVignette')
    vignette.factor = 0.3
    vignette.color = (0, 0, 0)
    
    # Output
    comp = nodes.new('CompositorNodeComposite')
    
    # Viewer
    viewer = nodes.new('CompositorNodeViewer')
    
    # Layout
    rl.location = (-400, 0)
    glare.location = (-100, 100)
    colorbal.location = (100, 0)
    vignette.location = (300, -100)
    comp.location = (500, 0)
    viewer.location = (500, 200)
    
    # Links
    links.new(rl.outputs['Image'], glare.inputs['Image'])
    links.new(glare.outputs['Image'], colorbal.inputs['Image'])
    links.new(colorbal.outputs['Image'], vignette.inputs['Image'])
    links.new(vignette.outputs['Image'], comp.inputs['Image'])
    links.new(vignette.outputs['Image'], viewer.inputs['Image'])

# ============================================================
# MAIN EXECUTION
# ============================================================
def main():
    print("=" * 60)
    print("CUE STRIKE - Title Screen Generator")
    print("=" * 60)
    
    # Clean and setup
    print("🧹 Cleaning scene...")
    clean_scene()
    
    print("⚙️ Setting up scene...")
    setup_scene()
    setup_world()
    
    print("🎱 Creating pool table...")
    table = create_pool_table()
    
    print("🎱 Creating balls...")
    cue_ball = create_cue_ball()
    eight_ball = create_8ball()
    scattered = create_scattered_balls()
    
    print("🎱 Creating cue stick...")
    cue = create_cue_stick()
    
    print("📝 Creating title text...")
    title, subtitle = create_title_text()
    
    print("✨ Creating particles...")
    particles = create_particle_system()
    
    print("💡 Setting up lighting...")
    lights = create_lighting()
    
    print("📷 Creating camera...")
    cam = create_camera()
    
    print("🎬 Animating...")
    animate_camera(cam)
    animate_title_text(title, subtitle)
    animate_balls(cue_ball, eight_ball, scattered)
    animate_cue(cue)
    animate_lights(lights)
    
    print("🎨 Setting up compositing...")
    setup_compositing()
    
    # Final setup
    bpy.context.scene.frame_set(FRAME_START)
    
    # Save blend file
    blend_path = "//CUE_STRIKE_Title_Screen.blend"
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"💾 Saved blend file: {blend_path}")
    
    print("=" * 60)
    print("✅ Title screen setup complete!")
    print(f"📁 Render output: {OUTPUT_DIR}")
    print(f"🎞️ Frames: {FRAME_START}-{FRAME_END} ({FPS}fps = {(FRAME_END-FRAME_START)/FPS:.1f}s)")
    print("=" * 60)
    print("\nTo render:")
    print("  1. Review camera/lighting in viewport")
    print("  2. Press F12 for single frame test")
    print("  3. Ctrl+F12 for animation render")
    print("  4. Or run: bpy.ops.render.render(animation=True)")
    
    return True

# Run
if __name__ == "__main__":
    main()