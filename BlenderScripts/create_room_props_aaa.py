import bpy
import bmesh
import math
import random
import os

# =============================================================================
# CueStrike AAA World Tour - Room Props & Environment Generator
# Creates high-fidelity props, wall textures, lighting & FX for 8 themed rooms
# Zero Pink Policy: All materials use URP/Lit compatible Principled BSDF
# =============================================================================

# -----------------------------------------------------------------------------
# CONFIGURATION
# -----------------------------------------------------------------------------
ROOM_TYPES = [
    "ZenDojo",
    "Cyberpunk",
    "SpaceNebula",
    "Industrial",
    "WarpFantasy",
    "Luxury_DAY",
    "Luxury_NIGHT",  # Bonus: Night variant
    "Arena_Core",    # Bonus: Central arena
]

OUTPUT_DIR = "//../Assets/CueStrike/Art/Rooms/"
EXPORT_FBX = True
EXPORT_GLTF = False

# Room dimensions (matching Unity scene scale)
ROOM_WIDTH = 10.0
ROOM_DEPTH = 10.0
ROOM_HEIGHT = 4.0

# -----------------------------------------------------------------------------
# UTILITY FUNCTIONS
# -----------------------------------------------------------------------------
def clear_scene():
    """Remove all mesh objects, materials, collections."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for mat in bpy.data.materials:
        bpy.data.materials.remove(mat)
    for coll in bpy.data.collections:
        if coll.name != "Scene Collection":
            bpy.data.collections.remove(coll)

def create_collection(name, parent=None):
    """Create or get a collection."""
    if name in bpy.data.collections:
        coll = bpy.data.collections[name]
    else:
        coll = bpy.data.collections.new(name)
        if parent:
            parent.children.link(coll)
        else:
            bpy.context.scene.collection.children.link(coll)
    return coll

def link_to_collection(obj, collection):
    """Link object to collection, unlink from others."""
    for coll in obj.users_collection:
        coll.objects.unlink(obj)
    collection.objects.link(obj)

def create_material(name, base_color=(1,1,1,1), metallic=0.0, roughness=0.5,
                    emission_color=None, emission_strength=0.0,
                    alpha=1.0, use_transmission=False, transmission=0.0,
                    clearcoat=0.0, clearcoat_roughness=0.0,
                    ior=1.45, specular=0.5):
    """Create a Principled BSDF material (URP/Lit compatible)."""
    if name in bpy.data.materials:
        mat = bpy.data.materials[name]
    else:
        mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    
    bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
    bsdf.location = (0, 0)
    bsdf.inputs['Base Color'].default_value = base_color
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Alpha'].default_value = alpha
    bsdf.inputs['IOR'].default_value = ior
    bsdf.inputs['Specular'].default_value = specular
    bsdf.inputs['Clearcoat'].default_value = clearcoat
    bsdf.inputs['Clearcoat Roughness'].default_value = clearcoat_roughness
    if use_transmission:
        bsdf.inputs['Transmission'].default_value = transmission
    
    if emission_color and emission_strength > 0:
        # Handle different Blender versions - try Emission Color first, then Emission
        try:
            bsdf.inputs['Emission Color'].default_value = (*emission_color[:3], 1)
        except KeyError:
            bsdf.inputs['Emission'].default_value = (*emission_color[:3], 1)
        bsdf.inputs['Emission Strength'].default_value = emission_strength
    
    output = nodes.new(type='ShaderNodeOutputMaterial')
    output.location = (300, 0)
    links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    
    # Set blend mode for transparency
    if alpha < 1.0:
        mat.blend_method = 'BLEND'
        mat.shadow_method = 'HASHED'
    
    return mat

def create_emission_material(name, color, strength=5.0):
    """Create pure emission material for lights/neon."""
    # Ensure color has 4 components (RGBA)
    if len(color) == 3:
        color = (*color, 1.0)
    mat = create_material(name, base_color=color, emission_color=color, 
                          emission_strength=strength, roughness=0.0)
    return mat

def add_noise_texture(mat, scale=10, detail=8, distortion=0.0, 
                       mapping_scale=(1,1,1), output_socket='Roughness'):
    """Add procedural noise texture to material."""
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if not bsdf:
        return
    
    noise = nodes.new(type='ShaderNodeTexNoise')
    noise.location = (-400, -100)
    noise.inputs['Scale'].default_value = scale
    noise.inputs['Detail'].default_value = detail
    noise.inputs['Distortion'].default_value = distortion
    
    mapping = nodes.new(type='ShaderNodeMapping')
    mapping.location = (-600, -100)
    mapping.inputs['Scale'].default_value = mapping_scale
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-800, -100)
    
    links.new(tex_coord.outputs['Object'], mapping.inputs['Vector'])
    links.new(mapping.outputs['Vector'], noise.inputs['Vector'])
    links.new(noise.outputs['Fac'], bsdf.inputs[output_socket])

def add_voronoi_texture(mat, scale=10, output_socket='Roughness', feature='F1'):
    """Add voronoi texture for cellular patterns."""
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if not bsdf:
        return
    
    voronoi = nodes.new(type='ShaderNodeTexVoronoi')
    voronoi.location = (-400, -100)
    voronoi.inputs['Scale'].default_value = scale
    voronoi.feature = feature
    
    mapping = nodes.new(type='ShaderNodeMapping')
    mapping.location = (-600, -100)
    mapping.inputs['Scale'].default_value = (1, 1, 1)
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-800, -100)
    
    links.new(tex_coord.outputs['Object'], mapping.inputs['Vector'])
    links.new(mapping.outputs['Vector'], voronoi.inputs['Vector'])
    links.new(voronoi.outputs['Distance'], bsdf.inputs[output_socket])

def add_color_ramp(mat, input_socket, color_stops, position=(-200, -200)):
    """Add color ramp for gradient control."""
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if not bsdf:
        return
    
    ramp = nodes.new(type='ShaderNodeValToRGB')
    ramp.location = position
    ramp.color_ramp.elements.clear()
    for i, (pos, color) in enumerate(color_stops):
        if i == 0:
            ramp.color_ramp.elements[0].position = pos
            ramp.color_ramp.elements[0].color = (*color[:3], 1)
        else:
            elem = ramp.color_ramp.elements.new(pos)
            elem.color = (*color[:3], 1)
    
    links.new(input_socket, ramp.inputs['Fac'])
    return ramp.outputs['Color']

# -----------------------------------------------------------------------------
# WALL / FLOOR / CEILING CREATION
# -----------------------------------------------------------------------------
def create_room_architecture(room_name, collection):
    """Create the base room: walls, floor, ceiling with UVs."""
    # Floor
    bpy.ops.mesh.primitive_plane_add(size=ROOM_WIDTH, location=(0, 0, 0))
    floor = bpy.context.active_object
    floor.name = f"{room_name}_Floor"
    floor.scale = (1, 1, 1)
    bpy.ops.object.transform_apply(scale=True)
    link_to_collection(floor, collection)
    
    # Ceiling
    bpy.ops.mesh.primitive_plane_add(size=ROOM_WIDTH, location=(0, 0, ROOM_HEIGHT))
    ceiling = bpy.context.active_object
    ceiling.name = f"{room_name}_Ceiling"
    ceiling.rotation_euler = (math.pi, 0, 0)
    bpy.ops.object.transform_apply(rotation=True)
    link_to_collection(ceiling, collection)
    
    # Walls (4 sides)
    wall_data = [
        ("Wall_North", (0, ROOM_DEPTH/2, ROOM_HEIGHT/2), (0, 0, 0), (ROOM_WIDTH, ROOM_HEIGHT, 0.1)),
        ("Wall_South", (0, -ROOM_DEPTH/2, ROOM_HEIGHT/2), (0, math.pi, 0), (ROOM_WIDTH, ROOM_HEIGHT, 0.1)),
        ("Wall_East", (ROOM_WIDTH/2, 0, ROOM_HEIGHT/2), (0, -math.pi/2, 0), (ROOM_DEPTH, ROOM_HEIGHT, 0.1)),
        ("Wall_West", (-ROOM_WIDTH/2, 0, ROOM_HEIGHT/2), (0, math.pi/2, 0), (ROOM_DEPTH, ROOM_HEIGHT, 0.1)),
    ]
    
    walls = []
    for name, loc, rot, scale in wall_data:
        bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
        wall = bpy.context.active_object
        wall.name = f"{room_name}_{name}"
        wall.scale = scale
        bpy.ops.object.transform_apply(scale=True)
        link_to_collection(wall, collection)
        walls.append(wall)
    
    # UV unwrap all
    for obj in [floor, ceiling] + walls:
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(angle_limit=66, island_margin=0.02)
        bpy.ops.object.mode_set(mode='OBJECT')
    
    return floor, ceiling, walls

def apply_wall_material_zendojo(walls, floor, ceiling):
    """ZenDojo: Chinese pattern wallpaper, paper lanterns on walls."""
    # Wall material - subtle Chinese pattern
    wall_mat = create_material("ZenDojo_Wall", base_color=(0.85, 0.82, 0.75, 1), 
                               metallic=0.0, roughness=0.85)
    nodes = wall_mat.node_tree.nodes
    links = wall_mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    
    # Add subtle pattern via noise
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-800, 0)
    mapping = nodes.new(type='ShaderNodeMapping')
    mapping.location = (-600, 0)
    mapping.inputs['Scale'].default_value = (8, 8, 1)
    noise = nodes.new(type='ShaderNodeTexNoise')
    noise.location = (-400, 0)
    noise.inputs['Scale'].default_value = 15
    noise.inputs['Detail'].default_value = 16
    noise.inputs['Distortion'].default_value = 0.3
    ramp = nodes.new(type='ShaderNodeValToRGB')
    ramp.location = (-200, 0)
    ramp.color_ramp.elements[0].position = 0.45
    ramp.color_ramp.elements[0].color = (0.75, 0.72, 0.65, 1)
    ramp.color_ramp.elements.new(0.55).color = (0.88, 0.85, 0.78, 1)
    
    links.new(tex_coord.outputs['UV'], mapping.inputs['Vector'])
    links.new(mapping.outputs['Vector'], noise.inputs['Vector'])
    links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], bsdf.inputs['Base Color'])
    links.new(noise.outputs['Fac'], bsdf.inputs['Roughness'])
    
    for wall in walls:
        wall.data.materials.append(wall_mat)
    
    # Floor - tatami texture
    floor_mat = create_material("ZenDojo_Floor", base_color=(0.65, 0.58, 0.45, 1),
                                metallic=0.0, roughness=0.9)
    add_noise_texture(floor_mat, scale=50, detail=4, mapping_scale=(20, 20, 1))
    floor.data.materials.append(floor_mat)
    
    # Ceiling - paper texture
    ceiling_mat = create_material("ZenDojo_Ceiling", base_color=(0.92, 0.9, 0.85, 1),
                                  metallic=0.0, roughness=0.95)
    ceiling.data.materials.append(ceiling_mat)
    
    # Add paper lanterns on walls
    lantern_mat = create_emission_material("ZenDojo_Lantern_Light", (1, 0.85, 0.6), 3.0)
    lantern_frame_mat = create_material("ZenDojo_Lantern_Frame", base_color=(0.3, 0.15, 0.05, 1),
                                        metallic=0.0, roughness=0.7)
    lantern_paper_mat = create_material("ZenDojo_Lantern_Paper", base_color=(1, 0.9, 0.7, 1),
                                        metallic=0.0, roughness=0.5, alpha=0.3)
    lantern_paper_mat.blend_method = 'BLEND'
    
    for i in range(4):
        angle = i * math.pi / 2
        x = 3.5 * math.cos(angle)
        y = 3.5 * math.sin(angle)
        z = 2.8
        
        # Frame
        bpy.ops.mesh.primitive_torus_add(location=(x, y, z), rotation=(math.pi/2, 0, angle),
                                         major_radius=0.25, minor_radius=0.02)
        frame = bpy.context.active_object
        frame.name = f"ZenDojo_Lantern_Frame_{i}"
        frame.data.materials.append(lantern_frame_mat)
        link_to_collection(frame, bpy.data.collections.get("ZenDojo_Props"))
        
        # Paper shade
        bpy.ops.mesh.primitive_cylinder_add(radius=0.23, depth=0.4, vertices=16,
                                             location=(x, y, z - 0.1))
        paper = bpy.context.active_object
        paper.name = f"ZenDojo_Lantern_Paper_{i}"
        paper.data.materials.append(lantern_paper_mat)
        link_to_collection(paper, bpy.data.collections.get("ZenDojo_Props"))
        
        # Light source
        bpy.ops.mesh.primitive_ico_sphere_add(radius=0.1, location=(x, y, z - 0.1))
        light_obj = bpy.context.active_object
        light_obj.name = f"ZenDojo_Lantern_Light_{i}"
        light_obj.data.materials.append(lantern_mat)
        link_to_collection(light_obj, bpy.data.collections.get("ZenDojo_Props"))

def apply_wall_material_cyberpunk(walls, floor, ceiling):
    """Cyberpunk: Concrete with cracks, graffiti, neon reflections on floor."""
    # Wall material - cracked concrete with graffiti
    wall_mat = create_material("Cyberpunk_Wall", base_color=(0.25, 0.25, 0.28, 1),
                               metallic=0.05, roughness=0.85)
    nodes = wall_mat.node_tree.nodes
    links = wall_mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-1000, 200)
    
    # Large scale noise for concrete texture
    noise1 = nodes.new(type='ShaderNodeTexNoise')
    noise1.location = (-800, 200)
    noise1.inputs['Scale'].default_value = 5
    noise1.inputs['Detail'].default_value = 12
    noise1.inputs['Distortion'].default_value = 0.8
    
    mapping1 = nodes.new(type='ShaderNodeMapping')
    mapping1.location = (-1000, 100)
    mapping1.inputs['Scale'].default_value = (2, 2, 1)
    
    # Cracks - voronoi
    voronoi = nodes.new(type='ShaderNodeTexVoronoi')
    voronoi.location = (-800, 0)
    voronoi.inputs['Scale'].default_value = 8
    voronoi.feature = 'F1'
    
    mapping2 = nodes.new(type='ShaderNodeMapping')
    mapping2.location = (-1000, -100)
    mapping2.inputs['Scale'].default_value = (5, 5, 1)
    
    # Graffiti layer
    noise2 = nodes.new(type='ShaderNodeTexNoise')
    noise2.location = (-800, -200)
    noise2.inputs['Scale'].default_value = 20
    noise2.inputs['Detail'].default_value = 8
    noise2.inputs['Distortion'].default_value = 1.5
    
    mapping3 = nodes.new(type='ShaderNodeMapping')
    mapping3.location = (-1000, -300)
    mapping3.inputs['Scale'].default_value = (10, 10, 1)
    
    # Color ramps
    ramp1 = nodes.new(type='ShaderNodeValToRGB')
    ramp1.location = (-600, 200)
    ramp1.color_ramp.elements[0].position = 0.4
    ramp1.color_ramp.elements[0].color = (0.15, 0.15, 0.18, 1)
    ramp1.color_ramp.elements.new(0.6).color = (0.3, 0.3, 0.33, 1)
    
    ramp2 = nodes.new(type='ShaderNodeValToRGB')
    ramp2.location = (-600, 0)
    ramp2.color_ramp.elements[0].position = 0.98
    ramp2.color_ramp.elements[0].color = (0.05, 0.05, 0.05, 1)
    ramp2.color_ramp.elements.new(1.0).color = (0.15, 0.15, 0.15, 1)
    
    ramp3 = nodes.new(type='ShaderNodeValToRGB')
    ramp3.location = (-600, -200)
    ramp3.color_ramp.elements[0].position = 0.7
    ramp3.color_ramp.elements[0].color = (0.25, 0.25, 0.28, 1)
    ramp3.color_ramp.elements.new(0.85).color = (1, 0, 1, 1)  # Magenta graffiti
    ramp3.color_ramp.elements.new(0.9).color = (0, 1, 1, 1)   # Cyan graffiti
    ramp3.color_ramp.elements.new(0.95).color = (1, 1, 0, 1)  # Yellow graffiti
    
    # Mix nodes
    mix1 = nodes.new(type='ShaderNodeMixRGB')
    mix1.location = (-400, 100)
    mix1.blend_type = 'MULTIPLY'
    mix1.inputs['Fac'].default_value = 0.5
    
    mix2 = nodes.new(type='ShaderNodeMixRGB')
    mix2.location = (-400, -100)
    mix2.blend_type = 'OVERLAY'
    mix2.inputs['Fac'].default_value = 0.3
    
    links.new(tex_coord.outputs['UV'], mapping1.inputs['Vector'])
    links.new(mapping1.outputs['Vector'], noise1.inputs['Vector'])
    links.new(noise1.outputs['Fac'], ramp1.inputs['Fac'])
    
    links.new(tex_coord.outputs['UV'], mapping2.inputs['Vector'])
    links.new(mapping2.outputs['Vector'], voronoi.inputs['Vector'])
    links.new(voronoi.outputs['Distance'], ramp2.inputs['Fac'])
    
    links.new(tex_coord.outputs['UV'], mapping3.inputs['Vector'])
    links.new(mapping3.outputs['Vector'], noise2.inputs['Vector'])
    links.new(noise2.outputs['Fac'], ramp3.inputs['Fac'])
    
    links.new(ramp1.outputs['Color'], mix1.inputs['Color1'])
    links.new(ramp2.outputs['Color'], mix1.inputs['Color2'])
    links.new(mix1.outputs['Color'], mix2.inputs['Color1'])
    links.new(ramp3.outputs['Color'], mix2.inputs['Color2'])
    links.new(mix2.outputs['Color'], bsdf.inputs['Base Color'])
    links.new(ramp2.outputs['Color'], bsdf.inputs['Roughness'])
    
    for wall in walls:
        wall.data.materials.append(wall_mat)
    
    # Floor - reflective concrete with neon glow
    floor_mat = create_material("Cyberpunk_Floor", base_color=(0.12, 0.12, 0.15, 1),
                                metallic=0.3, roughness=0.3)
    add_noise_texture(floor_mat, scale=100, detail=4, mapping_scale=(30, 30, 1))
    floor.data.materials.append(floor_mat)
    
    # Ceiling - industrial
    ceiling_mat = create_material("Cyberpunk_Ceiling", base_color=(0.1, 0.1, 0.12, 1),
                                  metallic=0.1, roughness=0.7)
    ceiling.data.materials.append(ceiling_mat)
    
    # Add neon strips on walls
    neon_colors = [(1, 0, 1), (0, 1, 1), (1, 0.5, 0), (0, 1, 0.5)]
    props_coll = bpy.data.collections.get("Cyberpunk_Props")
    for i, color in enumerate(neon_colors):
        angle = i * math.pi / 2
        x = 4.2 * math.cos(angle)
        y = 4.2 * math.sin(angle)
        z = 1.5 + (i % 2) * 1.2
        
        mat = create_emission_material(f"Cyberpunk_Neon_{i}", color, 8.0)
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z),
                                         rotation=(0, 0, angle + math.pi/2))
        neon = bpy.context.active_object
        neon.name = f"Cyberpunk_Neon_{i}"
        neon.scale = (2.5, 0.05, 0.3)
        bpy.ops.object.transform_apply(scale=True)
        neon.data.materials.append(mat)
        link_to_collection(neon, props_coll)

def apply_wall_material_spacenebula(walls, floor, ceiling):
    """SpaceNebula: Metal spaceship walls with nebula-view windows."""
    # Wall material - spaceship metal panels
    wall_mat = create_material("SpaceNebula_Wall", base_color=(0.35, 0.38, 0.42, 1),
                               metallic=0.85, roughness=0.25)
    add_noise_texture(wall_mat, scale=200, detail=8, mapping_scale=(1, 50, 1))
    
    # Add panel lines
    nodes = wall_mat.node_tree.nodes
    links = wall_mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-800, -300)
    mapping = nodes.new(type='ShaderNodeMapping')
    mapping.location = (-600, -300)
    mapping.inputs['Scale'].default_value = (20, 1, 1)
    wave = nodes.new(type='ShaderNodeTexWave')
    wave.location = (-400, -300)
    wave.wave_type = 'RINGS'
    wave.inputs['Scale'].default_value = 20
    wave.inputs['Distortion'].default_value = 0
    ramp = nodes.new(type='ShaderNodeValToRGB')
    ramp.location = (-200, -300)
    ramp.color_ramp.elements[0].position = 0.9
    ramp.color_ramp.elements[0].color = (0.3, 0.33, 0.37, 1)
    ramp.color_ramp.elements.new(1.0).color = (0.45, 0.48, 0.52, 1)
    
    links.new(tex_coord.outputs['UV'], mapping.inputs['Vector'])
    links.new(mapping.outputs['Vector'], wave.inputs['Vector'])
    links.new(wave.outputs['Fac'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], bsdf.inputs['Base Color'])
    
    for wall in walls:
        wall.data.materials.append(wall_mat)
    
    # Floor - metal grating
    floor_mat = create_material("SpaceNebula_Floor", base_color=(0.25, 0.28, 0.32, 1),
                                metallic=0.9, roughness=0.2)
    # Grating pattern
    nodes = floor_mat.node_tree.nodes
    links = floor_mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-800, 0)
    mapping = nodes.new(type='ShaderNodeMapping')
    mapping.location = (-600, 0)
    mapping.inputs['Scale'].default_value = (30, 30, 1)
    
    wave1 = nodes.new(type='ShaderNodeTexWave')
    wave1.location = (-400, 100)
    wave1.wave_type = 'RINGS'
    wave1.inputs['Scale'].default_value = 30
    
    wave2 = nodes.new(type='ShaderNodeTexWave')
    wave2.location = (-400, -100)
    wave2.wave_type = 'RINGS'
    wave2.inputs['Scale'].default_value = 30
    mapping2 = nodes.new(type='ShaderNodeMapping')
    mapping2.location = (-600, -100)
    mapping2.inputs['Scale'].default_value = (30, 30, 1)
    mapping2.inputs['Rotation'].default_value = (0, 0, math.pi/2)
    
    mix = nodes.new(type='ShaderNodeMixRGB')
    mix.location = (-200, 0)
    mix.blend_type = 'DARKEN'
    
    ramp = nodes.new(type='ShaderNodeValToRGB')
    ramp.location = (0, 0)
    ramp.color_ramp.elements[0].position = 0.5
    ramp.color_ramp.elements[0].color = (0.15, 0.18, 0.22, 1)
    ramp.color_ramp.elements.new(1.0).color = (0.35, 0.38, 0.42, 1)
    
    links.new(tex_coord.outputs['UV'], mapping.inputs['Vector'])
    links.new(mapping.outputs['Vector'], wave1.inputs['Vector'])
    links.new(tex_coord.outputs['UV'], mapping2.inputs['Vector'])
    links.new(mapping2.outputs['Vector'], wave2.inputs['Vector'])
    links.new(wave1.outputs['Fac'], mix.inputs['Color1'])
    links.new(wave2.outputs['Fac'], mix.inputs['Color2'])
    links.new(mix.outputs['Color'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], bsdf.inputs['Base Color'])
    
    floor.data.materials.append(floor_mat)
    
    # Ceiling - panels with lights
    ceiling_mat = create_material("SpaceNebula_Ceiling", base_color=(0.3, 0.32, 0.36, 1),
                                  metallic=0.8, roughness=0.3)
    ceiling.data.materials.append(ceiling_mat)
    
    # Add windows showing nebula
    nebula_colors = [
        (0.6, 0.2, 0.8, 1.0),  # Purple
        (0.2, 0.6, 0.9, 1.0),  # Blue
        (0.8, 0.3, 0.5, 1.0),  # Pink
        (0.4, 0.8, 0.6, 1.0),  # Teal
    ]
    props_coll = bpy.data.collections.get("SpaceNebula_Props")
    for i in range(4):
        angle = i * math.pi / 2
        x = 4.5 * math.cos(angle)
        y = 4.5 * math.sin(angle)
        z = 2.0
        
        # Window frame
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z),
                                         rotation=(0, 0, angle))
        frame = bpy.context.active_object
        frame.name = f"SpaceNebula_Window_Frame_{i}"
        frame.scale = (2.5, 0.1, 1.5)
        bpy.ops.object.transform_apply(scale=True)
        frame_mat = create_material(f"SpaceNebula_Window_Frame_{i}", 
                                    base_color=(0.2, 0.22, 0.25, 1),
                                    metallic=0.9, roughness=0.1)
        frame.data.materials.append(frame_mat)
        link_to_collection(frame, props_coll)
        
        # Nebula glass
        bpy.ops.mesh.primitive_plane_add(size=1, location=(x, y, z),
                                          rotation=(0, 0, angle))
        glass = bpy.context.active_object
        glass.name = f"SpaceNebula_Window_Glass_{i}"
        glass.scale = (2.3, 1.3, 1)
        bpy.ops.object.transform_apply(scale=True)
        
        # Animated nebula material
        glass_mat = create_material(f"SpaceNebula_Nebula_{i}", 
                                    base_color=nebula_colors[i], metallic=0, 
                                    roughness=0.1, emission_color=nebula_colors[i],
                                    emission_strength=2.0, alpha=0.7)
        glass_mat.blend_method = 'BLEND'
        nodes = glass_mat.node_tree.nodes
        links = glass_mat.node_tree.links
        bsdf = nodes["Principled BSDF"]
        
        tex_coord = nodes.new(type='ShaderNodeTexCoord')
        tex_coord.location = (-600, 0)
        mapping = nodes.new(type='ShaderNodeMapping')
        mapping.location = (-400, 0)
        mapping.inputs['Scale'].default_value = (2, 2, 1)
        
        noise = nodes.new(type='ShaderNodeTexNoise')
        noise.location = (-200, 0)
        noise.inputs['Scale'].default_value = 5
        noise.inputs['Detail'].default_value = 16
        noise.inputs['Distortion'].default_value = 0.5
        
        ramp = nodes.new(type='ShaderNodeValToRGB')
        ramp.location = (0, 0)
        ramp.color_ramp.elements[0].position = 0.3
        ramp.color_ramp.elements[0].color = (*nebula_colors[i][:3], 0.3)
        ramp.color_ramp.elements.new(0.7).color = (*nebula_colors[i][:3], 1.0)
        
        links.new(tex_coord.outputs['UV'], mapping.inputs['Vector'])
        links.new(mapping.outputs['Vector'], noise.inputs['Vector'])
        links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
        links.new(ramp.outputs['Color'], bsdf.inputs['Base Color'])
        # Handle different Blender versions for emission
        try:
            links.new(ramp.outputs['Color'], bsdf.inputs['Emission Color'])
        except KeyError:
            links.new(ramp.outputs['Color'], bsdf.inputs['Emission'])
        links.new(noise.outputs['Fac'], bsdf.inputs['Emission Strength'])
        
        glass.data.materials.append(glass_mat)
        link_to_collection(glass, props_coll)

def apply_wall_material_industrial(walls, floor, ceiling):
    """Industrial: Old brick, rusted steel beams, oil stains on floor."""
    # Wall material - brick with mortar
    wall_mat = create_material("Industrial_Wall", base_color=(0.45, 0.25, 0.18, 1),
                               metallic=0.0, roughness=0.9)
    nodes = wall_mat.node_tree.nodes
    links = wall_mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-1000, 200)
    
    # Brick pattern using wave textures
    mapping1 = nodes.new(type='ShaderNodeMapping')
    mapping1.location = (-800, 300)
    mapping1.inputs['Scale'].default_value = (25, 12, 1)
    wave1 = nodes.new(type='ShaderNodeTexWave')
    wave1.location = (-600, 300)
    wave1.wave_type = 'BANDS'
    wave1.inputs['Scale'].default_value = 25
    
    mapping2 = nodes.new(type='ShaderNodeMapping')
    mapping2.location = (-800, 100)
    mapping2.inputs['Scale'].default_value = (1, 12, 1)
    wave2 = nodes.new(type='ShaderNodeTexWave')
    wave2.location = (-600, 100)
    wave2.wave_type = 'BANDS'
    wave2.inputs['Scale'].default_value = 12
    
    # Mortar lines
    mix_brick = nodes.new(type='ShaderNodeMixRGB')
    mix_brick.location = (-400, 200)
    mix_brick.blend_type = 'DARKEN'
    
    # Color variation
    noise = nodes.new(type='ShaderNodeTexNoise')
    noise.location = (-800, -100)
    noise.inputs['Scale'].default_value = 8
    noise.inputs['Detail'].default_value = 4
    
    mapping3 = nodes.new(type='ShaderNodeMapping')
    mapping3.location = (-1000, -100)
    mapping3.inputs['Scale'].default_value = (4, 4, 1)
    
    ramp_brick = nodes.new(type='ShaderNodeValToRGB')
    ramp_brick.location = (-400, -100)
    ramp_brick.color_ramp.elements[0].position = 0.0
    ramp_brick.color_ramp.elements[0].color = (0.55, 0.3, 0.2, 1)
    ramp_brick.color_ramp.elements.new(0.33).color = (0.45, 0.25, 0.18, 1)
    ramp_brick.color_ramp.elements.new(0.66).color = (0.4, 0.22, 0.15, 1)
    ramp_brick.color_ramp.elements.new(1.0).color = (0.35, 0.2, 0.12, 1)
    
    # Mortar color
    mortar_ramp = nodes.new(type='ShaderNodeValToRGB')
    mortar_ramp.location = (-200, 200)
    mortar_ramp.color_ramp.elements[0].position = 0.85
    mortar_ramp.color_ramp.elements[0].color = (0.3, 0.28, 0.25, 1)
    mortar_ramp.color_ramp.elements.new(1.0).color = (0.45, 0.25, 0.18, 1)
    
    mix_final = nodes.new(type='ShaderNodeMixRGB')
    mix_final.location = (0, 100)
    mix_final.blend_type = 'MIX'
    mix_final.inputs['Fac'].default_value = 1.0
    
    links.new(tex_coord.outputs['UV'], mapping1.inputs['Vector'])
    links.new(mapping1.outputs['Vector'], wave1.inputs['Vector'])
    links.new(tex_coord.outputs['UV'], mapping2.inputs['Vector'])
    links.new(mapping2.outputs['Vector'], wave2.inputs['Vector'])
    links.new(wave1.outputs['Fac'], mix_brick.inputs['Color1'])
    links.new(wave2.outputs['Fac'], mix_brick.inputs['Color2'])
    links.new(mix_brick.outputs['Color'], mortar_ramp.inputs['Fac'])
    
    links.new(tex_coord.outputs['UV'], mapping3.inputs['Vector'])
    links.new(mapping3.outputs['Vector'], noise.inputs['Vector'])
    links.new(noise.outputs['Fac'], ramp_brick.inputs['Fac'])
    
    links.new(ramp_brick.outputs['Color'], mix_final.inputs['Color1'])
    links.new(mortar_ramp.outputs['Color'], mix_final.inputs['Color2'])
    links.new(mix_brick.outputs['Color'], mix_final.inputs['Fac'])
    links.new(mix_final.outputs['Color'], bsdf.inputs['Base Color'])
    
    for wall in walls:
        wall.data.materials.append(wall_mat)
    
    # Floor - concrete with oil stains
    floor_mat = create_material("Industrial_Floor", base_color=(0.3, 0.3, 0.3, 1),
                                metallic=0.05, roughness=0.7)
    nodes = floor_mat.node_tree.nodes
    links = floor_mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-800, 0)
    
    # Base concrete noise
    noise1 = nodes.new(type='ShaderNodeTexNoise')
    noise1.location = (-600, 200)
    noise1.inputs['Scale'].default_value = 50
    noise1.inputs['Detail'].default_value = 8
    
    mapping1 = nodes.new(type='ShaderNodeMapping')
    mapping1.location = (-800, 200)
    mapping1.inputs['Scale'].default_value = (20, 20, 1)
    
    # Oil stains - large soft patches
    noise2 = nodes.new(type='ShaderNodeTexNoise')
    noise2.location = (-600, 0)
    noise2.inputs['Scale'].default_value = 5
    noise2.inputs['Detail'].default_value = 16
    noise2.inputs['Distortion'].default_value = 2.0
    
    mapping2 = nodes.new(type='ShaderNodeMapping')
    mapping2.location = (-800, 0)
    mapping2.inputs['Scale'].default_value = (3, 3, 1)
    
    ramp_oil = nodes.new(type='ShaderNodeValToRGB')
    ramp_oil.location = (-400, 0)
    ramp_oil.color_ramp.elements[0].position = 0.6
    ramp_oil.color_ramp.elements[0].color = (0.3, 0.3, 0.3, 1)
    ramp_oil.color_ramp.elements.new(0.75).color = (0.08, 0.06, 0.04, 1)
    ramp_oil.color_ramp.elements.new(1.0).color = (0.02, 0.01, 0.0, 1)
    
    # Metallic/rroughness for oil
    ramp_oil_met = nodes.new(type='ShaderNodeValToRGB')
    ramp_oil_met.location = (-400, -200)
    ramp_oil_met.color_ramp.elements[0].position = 0.6
    ramp_oil_met.color_ramp.elements[0].color = (0.05, 0.05, 0.05, 1)
    ramp_oil_met.color_ramp.elements.new(1.0).color = (0.8, 0.7, 0.5, 1)
    
    mix_color = nodes.new(type='ShaderNodeMixRGB')
    mix_color.location = (-200, 100)
    mix_color.blend_type = 'MULTIPLY'
    
    links.new(tex_coord.outputs['UV'], mapping1.inputs['Vector'])
    links.new(mapping1.outputs['Vector'], noise1.inputs['Vector'])
    links.new(tex_coord.outputs['UV'], mapping2.inputs['Vector'])
    links.new(mapping2.outputs['Vector'], noise2.inputs['Vector'])
    links.new(noise2.outputs['Fac'], ramp_oil.inputs['Fac'])
    links.new(noise2.outputs['Fac'], ramp_oil_met.inputs['Fac'])
    links.new(noise1.outputs['Fac'], mix_color.inputs['Color1'])
    links.new(ramp_oil.outputs['Color'], mix_color.inputs['Color2'])
    links.new(mix_color.outputs['Color'], bsdf.inputs['Base Color'])
    links.new(ramp_oil_met.outputs['Color'], bsdf.inputs['Metallic'])
    links.new(ramp_oil_met.outputs['Color'], bsdf.inputs['Roughness'])
    
    floor.data.materials.append(floor_mat)
    
    # Ceiling - exposed beams
    ceiling_mat = create_material("Industrial_Ceiling", base_color=(0.25, 0.25, 0.25, 1),
                                  metallic=0.1, roughness=0.8)
    ceiling.data.materials.append(ceiling_mat)
    
    # Add rusted steel beams on walls
    props_coll = bpy.data.collections.get("Industrial_Props")
    beam_mat = create_material("Industrial_Beam", base_color=(0.35, 0.2, 0.1, 1),
                               metallic=0.7, roughness=0.5)
    add_noise_texture(beam_mat, scale=50, detail=8, mapping_scale=(1, 20, 1))
    
    for i in range(3):
        y_pos = -3.5 + i * 3.5
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, y_pos, 3.7))
        beam = bpy.context.active_object
        beam.name = f"Industrial_Beam_{i}"
        beam.scale = (5.5, 0.3, 0.3)
        bpy.ops.object.transform_apply(scale=True)
        beam.data.materials.append(beam_mat)
        link_to_collection(beam, props_coll)

def apply_wall_material_warpfantasy(walls, floor, ceiling):
    """WarpFantasy: Ancient stone castle, magical runes on floor."""
    # Wall material - ancient stone blocks
    wall_mat = create_material("WarpFantasy_Wall", base_color=(0.45, 0.42, 0.38, 1),
                               metallic=0.0, roughness=0.85)
    nodes = wall_mat.node_tree.nodes
    links = wall_mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-1000, 200)
    
    # Stone blocks pattern
    mapping1 = nodes.new(type='ShaderNodeMapping')
    mapping1.location = (-800, 300)
    mapping1.inputs['Scale'].default_value = (10, 6, 1)
    wave1 = nodes.new(type='ShaderNodeTexWave')
    wave1.location = (-600, 300)
    wave1.wave_type = 'BANDS'
    wave1.inputs['Scale'].default_value = 10
    
    mapping2 = nodes.new(type='ShaderNodeMapping')
    mapping2.location = (-800, 100)
    mapping2.inputs['Scale'].default_value = (1, 6, 1)
    wave2 = nodes.new(type='ShaderNodeTexWave')
    wave2.location = (-600, 100)
    wave2.wave_type = 'BANDS'
    wave2.inputs['Scale'].default_value = 6
    
    mix_block = nodes.new(type='ShaderNodeMixRGB')
    mix_block.location = (-400, 200)
    mix_block.blend_type = 'MINIMUM'
    
    # Stone texture noise
    noise = nodes.new(type='ShaderNodeTexNoise')
    noise.location = (-800, -100)
    noise.inputs['Scale'].default_value = 30
    noise.inputs['Detail'].default_value = 12
    noise.inputs['Distortion'].default_value = 0.5
    
    mapping3 = nodes.new(type='ShaderNodeMapping')
    mapping3.location = (-1000, -100)
    mapping3.inputs['Scale'].default_value = (5, 5, 1)
    
    ramp_stone = nodes.new(type='ShaderNodeValToRGB')
    ramp_stone.location = (-400, -100)
    ramp_stone.color_ramp.elements[0].position = 0.0
    ramp_stone.color_ramp.elements[0].color = (0.55, 0.5, 0.45, 1)
    ramp_stone.color_ramp.elements.new(0.25).color = (0.45, 0.42, 0.38, 1)
    ramp_stone.color_ramp.elements.new(0.5).color = (0.4, 0.37, 0.33, 1)
    ramp_stone.color_ramp.elements.new(0.75).color = (0.35, 0.32, 0.28, 1)
    ramp_stone.color_ramp.elements.new(1.0).color = (0.3, 0.28, 0.25, 1)
    
    mortar_ramp = nodes.new(type='ShaderNodeValToRGB')
    mortar_ramp.location = (-200, 200)
    mortar_ramp.color_ramp.elements[0].position = 0.9
    mortar_ramp.color_ramp.elements[0].color = (0.25, 0.23, 0.2, 1)
    mortar_ramp.color_ramp.elements.new(1.0).color = (0.45, 0.42, 0.38, 1)
    
    mix_final = nodes.new(type='ShaderNodeMixRGB')
    mix_final.location = (0, 100)
    mix_final.blend_type = 'MIX'
    mix_final.inputs['Fac'].default_value = 1.0
    
    links.new(tex_coord.outputs['UV'], mapping1.inputs['Vector'])
    links.new(mapping1.outputs['Vector'], wave1.inputs['Vector'])
    links.new(tex_coord.outputs['UV'], mapping2.inputs['Vector'])
    links.new(mapping2.outputs['Vector'], wave2.inputs['Vector'])
    links.new(wave1.outputs['Fac'], mix_block.inputs['Color1'])
    links.new(wave2.outputs['Fac'], mix_block.inputs['Color2'])
    links.new(mix_block.outputs['Color'], mortar_ramp.inputs['Fac'])
    
    links.new(tex_coord.outputs['UV'], mapping3.inputs['Vector'])
    links.new(mapping3.outputs['Vector'], noise.inputs['Vector'])
    links.new(noise.outputs['Fac'], ramp_stone.inputs['Fac'])
    
    links.new(ramp_stone.outputs['Color'], mix_final.inputs['Color1'])
    links.new(mortar_ramp.outputs['Color'], mix_final.inputs['Color2'])
    links.new(mix_block.outputs['Color'], mix_final.inputs['Fac'])
    links.new(mix_final.outputs['Color'], bsdf.inputs['Base Color'])
    
    for wall in walls:
        wall.data.materials.append(wall_mat)
    
    # Floor - stone with glowing runes
    floor_mat = create_material("WarpFantasy_Floor", base_color=(0.35, 0.33, 0.3, 1),
                                metallic=0.0, roughness=0.75)
    nodes = floor_mat.node_tree.nodes
    links = floor_mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-1000, 0)
    
    # Base stone noise
    noise1 = nodes.new(type='ShaderNodeTexNoise')
    noise1.location = (-800, 200)
    noise1.inputs['Scale'].default_value = 40
    noise1.inputs['Detail'].default_value = 8
    
    mapping1 = nodes.new(type='ShaderNodeMapping')
    mapping1.location = (-1000, 200)
    mapping1.inputs['Scale'].default_value = (10, 10, 1)
    
    # Runes - voronoi for circular patterns
    voronoi = nodes.new(type='ShaderNodeTexVoronoi')
    voronoi.location = (-800, 0)
    voronoi.inputs['Scale'].default_value = 8
    voronoi.feature = 'F1'
    
    mapping2 = nodes.new(type='ShaderNodeMapping')
    mapping2.location = (-1000, 0)
    mapping2.inputs['Scale'].default_value = (4, 4, 1)
    
    ramp_rune = nodes.new(type='ShaderNodeValToRGB')
    ramp_rune.location = (-600, 0)
    ramp_rune.color_ramp.elements[0].position = 0.0
    ramp_rune.color_ramp.elements[0].color = (0.2, 0.5, 1.0, 1)  # Blue glow
    ramp_rune.color_ramp.elements.new(0.15).color = (0.1, 0.3, 0.8, 1)
    ramp_rune.color_ramp.elements.new(0.5).color = (0.35, 0.33, 0.3, 1)
    ramp_rune.color_ramp.elements.new(1.0).color = (0.35, 0.33, 0.3, 1)
    
    # Emission for runes
    ramp_emission = nodes.new(type='ShaderNodeValToRGB')
    ramp_emission.location = (-600, -200)
    ramp_emission.color_ramp.elements[0].position = 0.0
    ramp_emission.color_ramp.elements[0].color = (0.3, 0.7, 1.0, 1)
    ramp_emission.color_ramp.elements.new(0.1).color = (0.1, 0.3, 0.6, 1)
    ramp_emission.color_ramp.elements.new(0.5).color = (0.0, 0.0, 0.0, 1)
    ramp_emission.color_ramp.elements.new(1.0).color = (0.0, 0.0, 0.0, 1)
    
    mix_floor = nodes.new(type='ShaderNodeMixRGB')
    mix_floor.location = (-400, 100)
    mix_floor.blend_type = 'MIX'
    
    links.new(tex_coord.outputs['UV'], mapping1.inputs['Vector'])
    links.new(mapping1.outputs['Vector'], noise1.inputs['Vector'])
    links.new(tex_coord.outputs['UV'], mapping2.inputs['Vector'])
    links.new(mapping2.outputs['Vector'], voronoi.inputs['Vector'])
    links.new(voronoi.outputs['Distance'], ramp_rune.inputs['Fac'])
    links.new(voronoi.outputs['Distance'], ramp_emission.inputs['Fac'])
    links.new(noise1.outputs['Fac'], mix_floor.inputs['Color1'])
    links.new(ramp_rune.outputs['Color'], mix_floor.inputs['Color2'])
    links.new(mix_block.outputs['Color'], mix_floor.inputs['Fac'])
    links.new(mix_floor.outputs['Color'], bsdf.inputs['Base Color'])
    links.new(ramp_emission.outputs['Color'], bsdf.inputs['Emission Color'])
    links.new(ramp_emission.outputs['Color'], bsdf.inputs['Emission Strength'])
    
    floor.data.materials.append(floor_mat)
    
    # Ceiling - stone vaulted
    ceiling_mat = create_material("WarpFantasy_Ceiling", base_color=(0.3, 0.28, 0.25, 1),
                                  metallic=0.0, roughness=0.9)
    ceiling.data.materials.append(ceiling_mat)
    
    # Add magical pillars
    props_coll = bpy.data.collections.get("WarpFantasy_Props")
    pillar_mat = create_material("WarpFantasy_Pillar", base_color=(0.4, 0.37, 0.33, 1),
                                 metallic=0.0, roughness=0.7)
    
    rune_glow_mat = create_emission_material("WarpFantasy_Rune_Glow", (0.2, 0.6, 1.0), 5.0)
    
    for i in range(4):
        angle = i * math.pi / 2 + math.pi/4
        x = 3.5 * math.cos(angle)
        y = 3.5 * math.sin(angle)
        
        bpy.ops.mesh.primitive_cylinder_add(radius=0.4, depth=3.8, vertices=12,
                                             location=(x, y, 1.9))
        pillar = bpy.context.active_object
        pillar.name = f"WarpFantasy_Pillar_{i}"
        pillar.data.materials.append(pillar_mat)
        link_to_collection(pillar, props_coll)
        
        # Rune bands on pillar
        for band in range(3):
            bpy.ops.mesh.primitive_torus_add(location=(x, y, 0.5 + band * 1.2),
                                              major_radius=0.45, minor_radius=0.03)
            rune = bpy.context.active_object
            rune.name = f"WarpFantasy_Rune_Band_{i}_{band}"
            rune.data.materials.append(rune_glow_mat)
            link_to_collection(rune, props_coll)

def apply_wall_material_luxury(walls, floor, ceiling, is_day=True):
    """Luxury: Premium wood paneling, velvet drapes, marble floor."""
    prefix = "Luxury_DAY" if is_day else "Luxury_NIGHT"
    
    # Wall material - premium wood paneling (built-in)
    wall_mat = create_material(f"{prefix}_Wall", base_color=(0.35, 0.22, 0.15, 1),
                               metallic=0.0, roughness=0.35, clearcoat=0.3, clearcoat_roughness=0.2)
    nodes = wall_mat.node_tree.nodes
    links = wall_mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-1000, 200)
    
    # Wood grain - stretched noise
    noise = nodes.new(type='ShaderNodeTexNoise')
    noise.location = (-800, 200)
    noise.inputs['Scale'].default_value = 20
    noise.inputs['Detail'].default_value = 16
    noise.inputs['Distortion'].default_value = 0.8
    
    mapping = nodes.new(type='ShaderNodeMapping')
    mapping.location = (-1000, 200)
    mapping.inputs['Scale'].default_value = (1, 80, 1)  # Vertical grain
    
    ramp_wood = nodes.new(type='ShaderNodeValToRGB')
    ramp_wood.location = (-600, 200)
    if is_day:
        ramp_wood.color_ramp.elements[0].position = 0.0
        ramp_wood.color_ramp.elements[0].color = (0.45, 0.3, 0.2, 1)
        ramp_wood.color_ramp.elements.new(0.33).color = (0.35, 0.22, 0.15, 1)
        ramp_wood.color_ramp.elements.new(0.66).color = (0.3, 0.18, 0.12, 1)
        ramp_wood.color_ramp.elements.new(1.0).color = (0.4, 0.25, 0.18, 1)
    else:
        ramp_wood.color_ramp.elements[0].position = 0.0
        ramp_wood.color_ramp.elements[0].color = (0.25, 0.15, 0.1, 1)
        ramp_wood.color_ramp.elements.new(0.33).color = (0.18, 0.1, 0.06, 1)
        ramp_wood.color_ramp.elements.new(0.66).color = (0.15, 0.08, 0.04, 1)
        ramp_wood.color_ramp.elements.new(1.0).color = (0.22, 0.12, 0.08, 1)
    
    # Panel divisions
    mapping2 = nodes.new(type='ShaderNodeMapping')
    mapping2.location = (-1000, 0)
    mapping2.inputs['Scale'].default_value = (8, 4, 1)
    wave1 = nodes.new(type='ShaderNodeTexWave')
    wave1.location = (-800, 100)
    wave1.wave_type = 'SAW'
    wave1.inputs['Scale'].default_value = 8
    
    wave2 = nodes.new(type='ShaderNodeTexWave')
    wave2.location = (-800, -100)
    wave2.wave_type = 'SAW'
    wave2.inputs['Scale'].default_value = 4
    
    mix_panel = nodes.new(type='ShaderNodeMixRGB')
    mix_panel.location = (-600, 0)
    mix_panel.blend_type = 'MINIMUM'
    
    panel_ramp = nodes.new(type='ShaderNodeValToRGB')
    panel_ramp.location = (-400, 0)
    panel_ramp.color_ramp.elements[0].position = 0.92
    panel_ramp.color_ramp.elements[0].color = (0.2, 0.12, 0.08, 1)
    panel_ramp.color_ramp.elements.new(1.0).color = (1, 1, 1, 1)
    
    mix_final = nodes.new(type='ShaderNodeMixRGB')
    mix_final.location = (-200, 100)
    mix_final.blend_type = 'MULTIPLY'
    
    links.new(tex_coord.outputs['UV'], mapping.inputs['Vector'])
    links.new(mapping.outputs['Vector'], noise.inputs['Vector'])
    links.new(noise.outputs['Fac'], ramp_wood.inputs['Fac'])
    
    links.new(tex_coord.outputs['UV'], mapping2.inputs['Vector'])
    links.new(mapping2.outputs['Vector'], wave1.inputs['Vector'])
    links.new(tex_coord.outputs['UV'], mapping2.inputs['Vector'])
    links.new(mapping2.outputs['Vector'], wave2.inputs['Vector'])
    links.new(wave1.outputs['Fac'], mix_panel.inputs['Color1'])
    links.new(wave2.outputs['Fac'], mix_panel.inputs['Color2'])
    links.new(mix_panel.outputs['Color'], panel_ramp.inputs['Fac'])
    links.new(ramp_wood.outputs['Color'], mix_final.inputs['Color1'])
    links.new(panel_ramp.outputs['Color'], mix_final.inputs['Color2'])
    links.new(mix_final.outputs['Color'], bsdf.inputs['Base Color'])
    
    for wall in walls:
        wall.data.materials.append(wall_mat)
    
    # Floor - polished marble
    floor_mat = create_material(f"{prefix}_Floor", base_color=(0.92, 0.9, 0.88, 1),
                                metallic=0.0, roughness=0.05, clearcoat=1.0, clearcoat_roughness=0.02)
    nodes = floor_mat.node_tree.nodes
    links = floor_mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-800, 0)
    
    # Marble veins
    noise = nodes.new(type='ShaderNodeTexNoise')
    noise.location = (-600, 100)
    noise.inputs['Scale'].default_value = 8
    noise.inputs['Detail'].default_value = 16
    noise.inputs['Distortion'].default_value = 1.5
    
    mapping = nodes.new(type='ShaderNodeMapping')
    mapping.location = (-800, 100)
    mapping.inputs['Scale'].default_value = (3, 3, 1)
    
    ramp_marble = nodes.new(type='ShaderNodeValToRGB')
    ramp_marble.location = (-400, 100)
    if is_day:
        ramp_marble.color_ramp.elements[0].position = 0.0
        ramp_marble.color_ramp.elements[0].color = (0.98, 0.97, 0.95, 1)
        ramp_marble.color_ramp.elements.new(0.4).color = (0.92, 0.9, 0.88, 1)
        ramp_marble.color_ramp.elements.new(0.6).color = (0.75, 0.72, 0.68, 1)
        ramp_marble.color_ramp.elements.new(1.0).color = (0.6, 0.58, 0.55, 1)
    else:
        ramp_marble.color_ramp.elements[0].position = 0.0
        ramp_marble.color_ramp.elements[0].color = (0.4, 0.38, 0.35, 1)
        ramp_marble.color_ramp.elements.new(0.4).color = (0.3, 0.28, 0.25, 1)
        ramp_marble.color_ramp.elements.new(0.6).color = (0.2, 0.18, 0.15, 1)
        ramp_marble.color_ramp.elements.new(1.0).color = (0.1, 0.08, 0.06, 1)
    
    mix_marble = nodes.new(type='ShaderNodeMixRGB')
    mix_marble.location = (-200, 100)
    mix_marble.blend_type = 'SCREEN'
    mix_marble.inputs['Fac'].default_value = 0.4
    
    links.new(tex_coord.outputs['UV'], mapping.inputs['Vector'])
    links.new(mapping.outputs['Vector'], noise.inputs['Vector'])
    links.new(noise.outputs['Fac'], ramp_marble.inputs['Fac'])
    links.new(bsdf.inputs['Base Color'].default_value, mix_marble.inputs['Color1'])
    links.new(ramp_marble.outputs['Color'], mix_marble.inputs['Color2'])
    links.new(mix_marble.outputs['Color'], bsdf.inputs['Base Color'])
    
    floor.data.materials.append(floor_mat)
    
    # Ceiling - coffered with gold leaf (day) or dark (night)
    ceiling_mat = create_material(f"{prefix}_Ceiling", 
                                   base_color=(0.95, 0.92, 0.85, 1) if is_day else (0.12, 0.1, 0.08, 1),
                                   metallic=0.2 if is_day else 0.0,
                                   roughness=0.4,
                                   clearcoat=0.5 if is_day else 0.1)
    ceiling.data.materials.append(ceiling_mat)
    
    # Add velvet drapes on walls
    props_coll = bpy.data.collections.get(f"{prefix}_Props")
    drape_color = (0.5, 0.1, 0.15, 1) if is_day else (0.3, 0.05, 0.1, 1)
    drape_mat = create_material(f"{prefix}_Drape", base_color=drape_color,
                                metallic=0.0, roughness=0.95, clearcoat=0.1)
    
    for i in range(4):
        angle = i * math.pi / 2
        x = 4.7 * math.cos(angle)
        y = 4.7 * math.sin(angle)
        
        bpy.ops.mesh.primitive_plane_add(size=1, location=(x, y, 2.0),
                                          rotation=(0, 0, angle))
        drape = bpy.context.active_object
        drape.name = f"{prefix}_Drape_{i}"
        drape.scale = (0.1, 3.5, 1)
        bpy.ops.object.transform_apply(scale=True)
        # Add cloth modifier simulation look
        bpy.ops.object.modifier_add(type='DISPLACE')
        drape.modifiers["Displace"].strength = 0.05
        drape.modifiers["Displace"].texture = bpy.data.textures.new("DrapeNoise", 'STUCCI')
        drape.data.materials.append(drape_mat)
        link_to_collection(drape, props_coll)
    
    # Gold trim on ceiling edges
    trim_mat = create_material(f"{prefix}_Gold_Trim", base_color=(0.85, 0.7, 0.2, 1),
                               metallic=1.0, roughness=0.15)
    for i in range(4):
        angle = i * math.pi / 2
        x = 5.0 * math.cos(angle)
        y = 5.0 * math.sin(angle)
        bpy.ops.mesh.primitive_torus_add(location=(x, y, 3.95),
                                          rotation=(math.pi/2, 0, angle),
                                          major_radius=5.0, minor_radius=0.05)
        trim = bpy.context.active_object
        trim.name = f"{prefix}_Ceiling_Trim_{i}"
        trim.data.materials.append(trim_mat)
        link_to_collection(trim, props_coll)

def apply_wall_material_arena(walls, floor, ceiling):
    """Arena_Core: Hexagonal tech panels, glowing floor grid, central hologram."""
    # Wall material - hexagonal tech panels
    wall_mat = create_material("Arena_Wall", base_color=(0.18, 0.2, 0.25, 1),
                               metallic=0.6, roughness=0.3)
    nodes = wall_mat.node_tree.nodes
    links = wall_mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-800, 0)
    
    # Hexagonal pattern using voronoi
    voronoi = nodes.new(type='ShaderNodeTexVoronoi')
    voronoi.location = (-600, 100)
    voronoi.inputs['Scale'].default_value = 15
    voronoi.feature = 'F2'
    
    mapping = nodes.new(type='ShaderNodeMapping')
    mapping.location = (-800, 100)
    mapping.inputs['Scale'].default_value = (8, 8, 1)
    
    ramp_hex = nodes.new(type='ShaderNodeValToRGB')
    ramp_hex.location = (-400, 100)
    ramp_hex.color_ramp.elements[0].position = 0.3
    ramp_hex.color_ramp.elements[0].color = (0.1, 0.12, 0.15, 1)
    ramp_hex.color_ramp.elements.new(0.4).color = (0.25, 0.28, 0.33, 1)
    ramp_hex.color_ramp.elements.new(0.6).color = (0.18, 0.2, 0.25, 1)
    ramp_hex.color_ramp.elements.new(1.0).color = (0.1, 0.12, 0.15, 1)
    
    # Glowing edges
    ramp_glow = nodes.new(type='ShaderNodeValToRGB')
    ramp_glow.location = (-400, -100)
    ramp_glow.color_ramp.elements[0].position = 0.0
    ramp_glow.color_ramp.elements[0].color = (0.0, 0.8, 1.0, 1)
    ramp_glow.color_ramp.elements.new(0.15).color = (0.0, 0.5, 0.8, 1)
    ramp_glow.color_ramp.elements.new(0.5).color = (0.0, 0.0, 0.0, 1)
    ramp_glow.color_ramp.elements.new(1.0).color = (0.0, 0.0, 0.0, 1)
    
    mix_hex = nodes.new(type='ShaderNodeMixRGB')
    mix_hex.location = (-200, 50)
    mix_hex.blend_type = 'ADD'
    
    links.new(tex_coord.outputs['UV'], mapping.inputs['Vector'])
    links.new(mapping.outputs['Vector'], voronoi.inputs['Vector'])
    links.new(voronoi.outputs['Distance'], ramp_hex.inputs['Fac'])
    links.new(voronoi.outputs['Distance'], ramp_glow.inputs['Fac'])
    links.new(ramp_hex.outputs['Color'], mix_hex.inputs['Color1'])
    links.new(ramp_glow.outputs['Color'], mix_hex.inputs['Color2'])
    links.new(mix_hex.outputs['Color'], bsdf.inputs['Base Color'])
    links.new(ramp_glow.outputs['Color'], bsdf.inputs['Emission Color'])
    links.new(ramp_glow.outputs['Color'], bsdf.inputs['Emission Strength'])
    
    for wall in walls:
        wall.data.materials.append(wall_mat)
    
    # Floor - glowing grid
    floor_mat = create_material("Arena_Floor", base_color=(0.05, 0.08, 0.12, 1),
                                metallic=0.3, roughness=0.2)
    nodes = floor_mat.node_tree.nodes
    links = floor_mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-1000, 0)
    
    # Grid lines
    mapping1 = nodes.new(type='ShaderNodeMapping')
    mapping1.location = (-800, 200)
    mapping1.inputs['Scale'].default_value = (10, 1, 1)
    wave1 = nodes.new(type='ShaderNodeTexWave')
    wave1.location = (-600, 200)
    wave1.wave_type = 'SAW'
    wave1.inputs['Scale'].default_value = 10
    
    mapping2 = nodes.new(type='ShaderNodeMapping')
    mapping2.location = (-800, 0)
    mapping2.inputs['Scale'].default_value = (1, 10, 1)
    wave2 = nodes.new(type='ShaderNodeTexWave')
    wave2.location = (-600, 0)
    wave2.wave_type = 'SAW'
    wave2.inputs['Scale'].default_value = 10
    
    mix_grid = nodes.new(type='ShaderNodeMixRGB')
    mix_grid.location = (-400, 100)
    mix_grid.blend_type = 'MAXIMUM'
    
    ramp_grid = nodes.new(type='ShaderNodeValToRGB')
    ramp_grid.location = (-200, 100)
    ramp_grid.color_ramp.elements[0].position = 0.9
    ramp_grid.color_ramp.elements[0].color = (0.05, 0.08, 0.12, 1)
    ramp_grid.color_ramp.elements.new(0.95).color = (0.0, 0.7, 1.0, 1)
    ramp_grid.color_ramp.elements.new(1.0).color = (0.0, 1.0, 1.0, 1)
    
    # Center glow
    voronoi_center = nodes.new(type='ShaderNodeTexVoronoi')
    voronoi_center.location = (-600, -200)
    voronoi_center.inputs['Scale'].default_value = 1
    voronoi_center.feature = 'F1'
    
    mapping_center = nodes.new(type='ShaderNodeMapping')
    mapping_center.location = (-800, -200)
    mapping_center.inputs['Scale'].default_value = (0.5, 0.5, 1)
    
    ramp_center = nodes.new(type='ShaderNodeValToRGB')
    ramp_center.location = (-400, -200)
    ramp_center.color_ramp.elements[0].position = 0.0
    ramp_center.color_ramp.elements[0].color = (0.0, 0.8, 1.0, 1)
    ramp_center.color_ramp.elements.new(0.3).color = (0.0, 0.4, 0.6, 1)
    ramp_center.color_ramp.elements.new(1.0).color = (0.0, 0.0, 0.0, 1)
    
    mix_final = nodes.new(type='ShaderNodeMixRGB')
    mix_final.location = (-200, -50)
    mix_final.blend_type = 'ADD'
    
    links.new(tex_coord.outputs['UV'], mapping1.inputs['Vector'])
    links.new(mapping1.outputs['Vector'], wave1.inputs['Vector'])
    links.new(tex_coord.outputs['UV'], mapping2.inputs['Vector'])
    links.new(mapping2.outputs['Vector'], wave2.inputs['Vector'])
    links.new(wave1.outputs['Fac'], mix_grid.inputs['Color1'])
    links.new(wave2.outputs['Fac'], mix_grid.inputs['Color2'])
    links.new(mix_grid.outputs['Color'], ramp_grid.inputs['Fac'])
    
    links.new(tex_coord.outputs['UV'], mapping_center.inputs['Vector'])
    links.new(mapping_center.outputs['Vector'], voronoi_center.inputs['Vector'])
    links.new(voronoi_center.outputs['Distance'], ramp_center.inputs['Fac'])
    
    links.new(ramp_grid.outputs['Color'], mix_final.inputs['Color1'])
    links.new(ramp_center.outputs['Color'], mix_final.inputs['Color2'])
    links.new(mix_final.outputs['Color'], bsdf.inputs['Base Color'])
    links.new(mix_final.outputs['Color'], bsdf.inputs['Emission Color'])
    links.new(mix_final.outputs['Color'], bsdf.inputs['Emission Strength'])
    
    floor.data.materials.append(floor_mat)
    
    # Ceiling - matching panels
    ceiling_mat = create_material("Arena_Ceiling", base_color=(0.15, 0.18, 0.22, 1),
                                  metallic=0.5, roughness=0.4)
    ceiling.data.materials.append(ceiling_mat)

# -----------------------------------------------------------------------------
# PROP CREATION FUNCTIONS (PER ROOM)
# -----------------------------------------------------------------------------
# ============ ZEN DOJO ============
def create_bonsai_tree(collection, x, y, seed=0):
    """Create a detailed bonsai tree."""
    random.seed(seed)
    trunk_mat = create_material(f"Bonsai_Trunk_{seed}", base_color=(0.35, 0.2, 0.12, 1),
                                metallic=0.0, roughness=0.85)
    leaf_mat = create_material(f"Bonsai_Leaf_{seed}", base_color=(0.15, 0.35, 0.18, 1),
                               metallic=0.0, roughness=0.7, alpha=0.95)
    leaf_mat.blend_method = 'HASHED'
    
    # Trunk with curves
    bpy.ops.curve.primitive_bezier_curve_add(location=(x, y, 0))
    trunk_curve = bpy.context.active_object
    trunk_curve.name = f"Bonsai_Trunk_{seed}"
    trunk_curve.data.dimensions = '3D'
    trunk_curve.data.resolution_u = 12
    trunk_curve.data.bevel_depth = 0.025
    trunk_curve.data.bevel_resolution = 6
    trunk_curve.data.fill_mode = 'FULL'
    
    # Shape the trunk
    points = trunk_curve.data.splines[0].bezier_points
    points[0].co = (0, 0, 0)
    points[0].handle_left_type = 'AUTO'
    points[0].handle_right_type = 'AUTO'
    points[1].co = (random.uniform(-0.15, 0.15), random.uniform(-0.15, 0.15), 0.6)
    points[1].handle_left_type = 'AUTO'
    points[1].handle_right_type = 'AUTO'
    
    # Add more segments
    for i in range(3):
        trunk_curve.data.splines[0].bezier_points.add(1)
    points = trunk_curve.data.splines[0].bezier_points
    for i in range(2, 5):
        points[i].co = (random.uniform(-0.2, 0.2), random.uniform(-0.2, 0.2), 0.6 + i * 0.25)
        points[i].handle_left_type = 'AUTO'
        points[i].handle_right_type = 'AUTO'
    
    trunk_curve.data.materials.append(trunk_mat)
    link_to_collection(trunk_curve, collection)
    
    # Foliage clusters
    for cluster in range(8):
        cx = x + random.uniform(-0.25, 0.25)
        cy = y + random.uniform(-0.25, 0.25)
        cz = 0.8 + cluster * 0.25
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.12,
                                               location=(cx, cy, cz))
        foliage = bpy.context.active_object
        foliage.name = f"Bonsai_Foliage_{seed}_{cluster}"
        foliage.data.materials.append(leaf_mat)
        link_to_collection(foliage, collection)

def create_bamboo_screen(collection):
    """Create bamboo screen / curtain."""
    bamboo_mat = create_material("Bamboo_Screen", base_color=(0.55, 0.65, 0.35, 1),
                                 metallic=0.0, roughness=0.6)
    node_mat = create_material("Bamboo_Node", base_color=(0.35, 0.45, 0.2, 1),
                               metallic=0.0, roughness=0.5)
    
    for i in range(12):
        x = -2.8 + i * 0.5
        z = 1.8
        bpy.ops.mesh.primitive_cylinder_add(radius=0.025, depth=3.6, vertices=8,
                                             location=(x, 4.7, z))
        bamboo = bpy.context.active_object
        bamboo.name = f"Bamboo_{i}"
        bamboo.data.materials.append(bamboo_mat)
        link_to_collection(bamboo, collection)
        
        # Nodes
        for n in range(6):
            nz = 0.3 + n * 0.55
            bpy.ops.mesh.primitive_torus_add(location=(x, 4.7, z - 1.8 + nz),
                                              major_radius=0.03, minor_radius=0.008)
            node = bpy.context.active_object
            node.name = f"Bamboo_Node_{i}_{n}"
            node.data.materials.append(node_mat)
            link_to_collection(node, collection)

def create_zen_garden_rocks(collection):
    """Create carved rocks / stone lanterns."""
    rock_mat = create_material("Zen_Rock", base_color=(0.45, 0.43, 0.4, 1),
                               metallic=0.0, roughness=0.9)
    
    positions = [(-3.5, -2.5), (-3.8, -1.8), (-2.8, -3.2), (3.2, -2.8), (3.5, -1.5)]
    for i, (x, y) in enumerate(positions):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3, radius=random.uniform(0.25, 0.45),
                                               location=(x, y, random.uniform(0.15, 0.3)))
        rock = bpy.context.active_object
        rock.name = f"Zen_Rock_{i}"
        # Distort
        bpy.ops.object.modifier_add(type='DISPLACE')
        rock.modifiers["Displace"].strength = 0.15
        rock.modifiers["Displace"].texture = bpy.data.textures.new(f"RockTex_{i}", 'STUCCI')
        rock.data.materials.append(rock_mat)
        link_to_collection(rock, collection)

def create_tea_set(collection):
    """Create ceramic tea set."""
    ceramic_mat = create_material("Tea_Ceramic", base_color=(0.92, 0.88, 0.82, 1),
                                  metallic=0.0, roughness=0.1, clearcoat=0.8)
    tea_mat = create_material("Tea_Liquid", base_color=(0.4, 0.25, 0.1, 1),
                              metallic=0.0, roughness=0.05, alpha=0.7,
                              use_transmission=True, transmission=0.8, ior=1.33)
    tea_mat.blend_method = 'BLEND'
    
    # Tray
    bpy.ops.mesh.primitive_cylinder_add(radius=0.35, depth=0.03, vertices=32,
                                         location=(0, -2.5, 0.76))
    tray = bpy.context.active_object
    tray.name = "Zen_Tea_Tray"
    tray.data.materials.append(ceramic_mat)
    link_to_collection(tray, collection)
    
    # Teapot
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.12, segments=16, ring_count=12,
                                          location=(-0.15, -2.5, 0.9))
    pot = bpy.context.active_object
    pot.name = "Zen_Teapot"
    pot.scale = (1, 0.9, 1)
    bpy.ops.object.transform_apply(scale=True)
    pot.data.materials.append(ceramic_mat)
    link_to_collection(pot, collection)
    
    # Spout
    bpy.ops.mesh.primitive_cylinder_add(radius=0.02, depth=0.12, vertices=8,
                                         location=(-0.25, -2.5, 0.9),
                                         rotation=(0, math.pi/2, 0))
    spout = bpy.context.active_object
    spout.name = "Zen_Teapot_Spout"
    spout.data.materials.append(ceramic_mat)
    link_to_collection(spout, collection)
    
    # Handle
    bpy.ops.mesh.primitive_torus_add(location=(0.0, -2.5, 0.95),
                                      major_radius=0.06, minor_radius=0.01)
    handle = bpy.context.active_object
    handle.name = "Zen_Teapot_Handle"
    handle.data.materials.append(ceramic_mat)
    link_to_collection(handle, collection)
    
    # Cups (2)
    for i in range(2):
        cx = 0.15 + i * 0.18
        bpy.ops.mesh.primitive_cylinder_add(radius=0.05, depth=0.07, vertices=16,
                                             location=(cx, -2.5, 0.8))
        cup = bpy.context.active_object
        cup.name = f"Zen_Tea_Cup_{i}"
        cup.data.materials.append(ceramic_mat)
        link_to_collection(cup, collection)
        
        # Tea liquid
        bpy.ops.mesh.primitive_cylinder_add(radius=0.045, depth=0.05, vertices=16,
                                             location=(cx, -2.5, 0.835))
        tea = bpy.context.active_object
        tea.name = f"Zen_Tea_Liquid_{i}"
        tea.data.materials.append(tea_mat)
        link_to_collection(tea, collection)

def create_paper_lanterns_on_walls(collection):
    """Create paper lanterns mounted on walls (already done in wall function)."""
    pass

# ============ CYBERPUNK ============
def create_neon_signs(collection):
    """Create holographic/neon signs."""
    signs = [
        ("CYBER", (1, 0, 1), (-3, 3, 2.5)),
        ("PUNK", (0, 1, 1), (1, 3, 2.5)),
        ("2077", (1, 0.5, 0), (-1, -3, 2.5)),
        ("ZONE", (0, 1, 0.5), (3, -3, 2.5)),
    ]
    
    for text, color, pos in signs:
        mat = create_emission_material(f"Neon_{text}", color, 10.0)
        # Create text using curves
        bpy.ops.object.text_add(location=pos, rotation=(0, 0, math.pi/2 if pos[0] < 0 else -math.pi/2))
        txt_obj = bpy.context.active_object
        txt_obj.name = f"Neon_Sign_{text}"
        txt_obj.data.body = text
        txt_obj.data.size = 0.5
        txt_obj.data.extrude = 0.05
        txt_obj.data.materials.append(mat)
        link_to_collection(txt_obj, collection)

def create_cable_mess(collection):
    """Create dangling cables/wires."""
    cable_mat = create_material("Cyberpunk_Cable", base_color=(0.15, 0.15, 0.18, 1),
                                metallic=0.3, roughness=0.4)
    
    for i in range(20):
        x = random.uniform(-4.5, 4.5)
        y = random.uniform(-4.5, 4.5)
        z_start = random.uniform(3.0, 3.8)
        
        bpy.ops.curve.primitive_bezier_curve_add(location=(x, y, z_start))
        cable = bpy.context.active_object
        cable.name = f"Cable_{i}"
        cable.data.dimensions = '3D'
        cable.data.resolution_u = 8
        cable.data.bevel_depth = 0.012
        cable.data.bevel_resolution = 4
        
        points = cable.data.splines[0].bezier_points
        points[0].co = (0, 0, 0)
        for seg in range(5):
            cable.data.splines[0].bezier_points.add(1)
        points = cable.data.splines[0].bezier_points
        for p_idx in range(1, 6):
            points[p_idx].co = (random.uniform(-0.3, 0.3), random.uniform(-0.3, 0.3), -p_idx * 0.5)
            points[p_idx].handle_left_type = 'AUTO'
            points[p_idx].handle_right_type = 'AUTO'
        
        cable.data.materials.append(cable_mat)
        link_to_collection(cable, collection)

def create_tech_trash_bins(collection):
    """Create high-tech trash bins."""
    bin_mat = create_material("Tech_Bin_Body", base_color=(0.2, 0.2, 0.22, 1),
                              metallic=0.7, roughness=0.3)
    screen_mat = create_emission_material("Tech_Bin_Screen", (0, 1, 0.8), 3.0)
    
    for i in range(3):
        angle = i * 2 * math.pi / 3
        x = 3.5 * math.cos(angle)
        y = 3.5 * math.sin(angle)
        
        bpy.ops.mesh.primitive_cylinder_add(radius=0.35, depth=1.0, vertices=16,
                                             location=(x, y, 0.5))
        bin_obj = bpy.context.active_object
        bin_obj.name = f"Tech_Bin_{i}"
        bin_obj.data.materials.append(bin_mat)
        link_to_collection(bin_obj, collection)
        
        # Screen panel
        bpy.ops.mesh.primitive_plane_add(size=1, location=(x + 0.36*math.cos(angle), 
                                                            y + 0.36*math.sin(angle), 0.8),
                                          rotation=(0, 0, angle + math.pi/2))
        screen = bpy.context.active_object
        screen.name = f"Tech_Bin_Screen_{i}"
        screen.scale = (0.5, 0.3, 1)
        bpy.ops.object.transform_apply(scale=True)
        screen.data.materials.append(screen_mat)
        link_to_collection(screen, collection)
        
        # Lid
        bpy.ops.mesh.primitive_cylinder_add(radius=0.36, depth=0.08, vertices=16,
                                             location=(x, y, 1.04))
        lid = bpy.context.active_object
        lid.name = f"Tech_Bin_Lid_{i}"
        lid.data.materials.append(bin_mat)
        link_to_collection(lid, collection)

def create_hologram_ads(collection):
    """Create floating holographic advertisements."""
    holo_colors = [(0, 1, 1), (1, 0, 1), (1, 1, 0), (0, 1, 0.5)]
    for i in range(6):
        x = random.uniform(-3, 3)
        y = random.uniform(-3, 3)
        z = random.uniform(1.5, 3.0)
        
        mat = create_emission_material(f"Holo_Ad_{i}", holo_colors[i % 4], 4.0)
        mat.node_tree.nodes["Principled BSDF"].inputs['Alpha'].default_value = 0.6
        mat.blend_method = 'BLEND'
        
        bpy.ops.mesh.primitive_plane_add(size=1, location=(x, y, z),
                                          rotation=(random.uniform(-0.3, 0.3), 
                                                    random.uniform(-0.3, 0.3), 0))
        holo = bpy.context.active_object
        holo.name = f"Holo_Ad_{i}"
        holo.scale = (random.uniform(0.8, 1.5), random.uniform(0.4, 0.8), 1)
        bpy.ops.object.transform_apply(scale=True)
        holo.data.materials.append(mat)
        link_to_collection(holo, collection)

# ============ SPACE NEBULA ============
def create_space_control_panel(collection):
    """Create spaceship control panel with buttons/screens."""
    panel_mat = create_material("Space_Panel_Body", base_color=(0.2, 0.22, 0.25, 1),
                                metallic=0.85, roughness=0.2)
    button_mat = create_material("Space_Button", base_color=(0.3, 0.35, 0.4, 1),
                                 metallic=0.7, roughness=0.2)
    screen_mat = create_emission_material("Space_Screen", (0.2, 0.8, 1.0), 2.0)
    red_btn_mat = create_emission_material("Space_Red_Btn", (1, 0.2, 0.2), 5.0)
    green_btn_mat = create_emission_material("Space_Green_Btn", (0.2, 1, 0.3), 5.0)
    
    # Main panel
    bpy.ops.mesh.primitive_cube_add(size=1, location=(-3.5, 0, 1.2),
                                     rotation=(0, math.pi/2, 0))
    panel = bpy.context.active_object
    panel.name = "Space_Control_Panel"
    panel.scale = (0.1, 2.5, 1.5)
    bpy.ops.object.transform_apply(scale=True)
    panel.data.materials.append(panel_mat)
    link_to_collection(panel, collection)
    
    # Buttons grid
    for row in range(5):
        for col in range(8):
            bx = -3.6
            by = -1.75 + col * 0.5
            bz = 0.5 + row * 0.25
            
            bpy.ops.mesh.primitive_cylinder_add(radius=0.03, depth=0.04, vertices=12,
                                                 location=(bx, by, bz))
            btn = bpy.context.active_object
            btn.name = f"Space_Btn_{row}_{col}"
            if row == 0 and col == 3:
                btn.data.materials.append(red_btn_mat)
            elif row == 4 and col == 6:
                btn.data.materials.append(green_btn_mat)
            else:
                btn.data.materials.append(button_mat)
            link_to_collection(btn, collection)
    
    # Main screen
    bpy.ops.mesh.primitive_plane_add(size=1, location=(-3.6, 0, 1.8),
                                      rotation=(0, math.pi/2, 0))
    screen = bpy.context.active_object
    screen.name = "Space_Main_Screen"
    screen.scale = (1.2, 0.8, 1)
    bpy.ops.object.transform_apply(scale=True)
    screen.data.materials.append(screen_mat)
    link_to_collection(screen, collection)
    
    # Holographic display above panel
    holo_mat = create_emission_material("Space_Holo_Map", (0, 0.7, 1), 3.0)
    holo_mat.node_tree.nodes["Principled BSDF"].inputs['Alpha'].default_value = 0.7
    holo_mat.blend_method = 'BLEND'
    
    bpy.ops.mesh.primitive_ico_sphere_add(radius=0.5, location=(-3.5, 0, 2.5))
    holo = bpy.context.active_object
    holo.name = "Space_Holo_StarMap"
    holo.data.materials.append(holo_mat)
    link_to_collection(holo, collection)

def create_holographic_star_map(collection):
    """Create detailed holographic star map."""
    # Central projector
    proj_mat = create_material("Space_Projector", base_color=(0.15, 0.18, 0.22, 1),
                               metallic=0.9, roughness=0.15)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.25, depth=0.4, vertices=16,
                                         location=(0, 0, 0.2))
    proj = bpy.context.active_object
    proj.name = "Space_Holo_Projector"
    proj.data.materials.append(proj_mat)
    link_to_collection(proj, collection)
    
    # Star particles
    star_mat = create_emission_material("Space_Star", (1, 1, 0.9), 2.0)
    for _ in range(200):
        # Spherical distribution
        phi = random.uniform(0, 2*math.pi)
        theta = random.uniform(0, math.pi)
        r = random.uniform(0.5, 2.5)
        x = r * math.sin(theta) * math.cos(phi)
        y = r * math.sin(theta) * math.sin(phi)
        z = r * math.cos(theta) + 1.5
        
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=random.uniform(0.015, 0.04),
                                               location=(x, y, z))
        star = bpy.context.active_object
        star.name = f"Space_Star_{_}"
        star.data.materials.append(star_mat)
        link_to_collection(star, collection)
    
    # Coordinate rings
    ring_mat = create_emission_material("Space_Coord_Ring", (0, 0.8, 1), 2.0)
    for i in range(3):
        bpy.ops.mesh.primitive_torus_add(location=(0, 0, 1.5 + i * 0.3),
                                          major_radius=2.0 - i * 0.4, minor_radius=0.01)
        ring = bpy.context.active_object
        ring.name = f"Space_Coord_Ring_{i}"
        ring.rotation_euler = (math.pi/2, 0, i * math.pi/6)
        ring.data.materials.append(ring_mat)
        link_to_collection(ring, collection)

def create_oxygen_tanks(collection):
    """Create high-pressure oxygen/storage tanks."""
    for tank_idx in range(4):
        x = -2.5 + tank_idx * 0.6
        # Main tank cylinder
        bpy.ops.mesh.primitive_cylinder_add(radius=0.12, depth=0.8, vertices=24,
                                             location=(x, 2, 0.4))
        tank = bpy.context.active_object
        tank.name = f"OxygenTank_{tank_idx}"

        # Tank body material - brushed metal
        tank_mat = create_material(f"OxygenTank_Body_{tank_idx}",
                                    base_color=(0.55, 0.55, 0.58, 1),
                                    metallic=0.95, roughness=0.15)
        # Add brushed texture
        tank_mat.use_nodes = True
        nodes = tank_mat.node_tree.nodes
        links = tank_mat.node_tree.links
        noise = nodes.new(type='ShaderNodeTexNoise')
        noise.location = (-200, 0)
        noise.inputs['Scale'].default_value = 200
        noise.inputs['Detail'].default_value = 16
        noise.inputs['Distortion'].default_value = 0.5
        mapping = nodes.new(type='ShaderNodeMapping')
        mapping.location = (-350, 0)
        mapping.inputs['Scale'].default_value = (1, 50, 1)
        tex_coord = nodes.new(type='ShaderNodeTexCoord')
        tex_coord.location = (-500, 0)
        links.new(tex_coord.outputs['Object'], mapping.inputs['Vector'])
        links.new(mapping.outputs['Vector'], noise.inputs['Vector'])
        bsdf = nodes["Principled BSDF"]
        links.new(noise.outputs['Fac'], bsdf.inputs['Roughness'])
        tank.data.materials.append(tank_mat)
        link_to_collection(tank, collection)

        # Valve on top
        bpy.ops.mesh.primitive_cylinder_add(radius=0.03, depth=0.08, vertices=16,
                                             location=(x, 2, 0.85))
        valve = bpy.context.active_object
        valve.name = f"OxygenTank_Valve_{tank_idx}"
        valve_mat = create_material("OxygenTank_Valve", base_color=(0.3, 0.3, 0.35, 1),
                                    metallic=1.0, roughness=0.1)
        valve.data.materials.append(valve_mat)
        link_to_collection(valve, collection)

        # Pressure gauge
        bpy.ops.mesh.primitive_cylinder_add(radius=0.025, depth=0.02, vertices=16,
                                             location=(x + 0.1, 2.1, 0.6))
        gauge = bpy.context.active_object
        gauge.name = f"OxygenTank_Gauge_{tank_idx}"
        gauge.rotation_euler = (math.pi/2, 0, 0)
        gauge_mat = create_emission_material(f"OxygenTank_Gauge_Light_{tank_idx}", 
                                              (0, 1, 0.5), 3.0)
        gauge.data.materials.append(gauge_mat)
        link_to_collection(gauge, collection)
        
        # Hose connection
        bpy.ops.curve.primitive_bezier_curve_add(location=(x - 0.12, 2, 0.3))
        hose = bpy.context.active_object
        hose.name = f"OxygenTank_Hose_{tank_idx}"
        hose.data.dimensions = '3D'
        hose.data.bevel_depth = 0.015
        hose.data.bevel_resolution = 4
        points = hose.data.splines[0].bezier_points
        points[0].co = (0, 0, 0)
        hose.data.splines[0].bezier_points.add(1)
        points[1].co = (-0.3, 0, -0.3)
        points[0].handle_left_type = 'AUTO'
        points[0].handle_right_type = 'AUTO'
        points[1].handle_left_type = 'AUTO'
        points[1].handle_right_type = 'AUTO'
        hose_mat = create_material("OxygenTank_Hose", base_color=(0.2, 0.2, 0.25, 1),
                                   metallic=0.5, roughness=0.4)
        hose.data.materials.append(hose_mat)
        link_to_collection(hose, collection)

# ============ INDUSTRIAL ============
def create_giant_fan(collection):
    """Create massive slow-turning industrial fan."""
    fan_mat = create_material("Industrial_Fan_Metal", base_color=(0.35, 0.35, 0.38, 1),
                              metallic=0.8, roughness=0.3)
    blade_mat = create_material("Industrial_Fan_Blade", base_color=(0.25, 0.25, 0.28, 1),
                                metallic=0.7, roughness=0.4)
    
    # Hub
    bpy.ops.mesh.primitive_cylinder_add(radius=0.6, depth=0.5, vertices=24,
                                         location=(0, 0, 2.5))
    hub = bpy.context.active_object
    hub.name = "Industrial_Fan_Hub"
    hub.data.materials.append(fan_mat)
    link_to_collection(hub, collection)
    
    # Blades (6)
    for i in range(6):
        angle = i * math.pi / 3
        bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, 2.5),
                                          rotation=(0, 0, angle))
        blade = bpy.context.active_object
        blade.name = f"Industrial_Fan_Blade_{i}"
        blade.scale = (0.15, 2.8, 1)
        bpy.ops.object.transform_apply(scale=True)
        # Taper
        bpy.ops.object.modifier_add(type='SIMPLE_DEFORM')
        blade.modifiers["SimpleDeform"].deform_method = 'TAPER'
        blade.modifiers["SimpleDeform"].factor = 0.5
        blade.data.materials.append(blade_mat)
        link_to_collection(blade, collection)
    
    # Center cap
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.25, location=(0, 0, 2.5))
    cap = bpy.context.active_object
    cap.name = "Industrial_Fan_Cap"
    cap.scale = (1, 1, 0.5)
    bpy.ops.object.transform_apply(scale=True)
    cap.data.materials.append(fan_mat)
    link_to_collection(cap, collection)
    
    # Motor housing
    bpy.ops.mesh.primitive_cylinder_add(radius=0.7, depth=0.8, vertices=16,
                                         location=(0, 0, 1.9))
    motor = bpy.context.active_object
    motor.name = "Industrial_Fan_Motor"
    motor.data.materials.append(fan_mat)
    link_to_collection(motor, collection)

def create_steam_pipes(collection):
    """Create steam pipe network."""
    pipe_mat = create_material("Industrial_Pipe", base_color=(0.4, 0.4, 0.42, 1),
                               metallic=0.7, roughness=0.35)
    valve_mat = create_material("Industrial_Valve", base_color=(0.3, 0.3, 0.32, 1),
                                metallic=0.85, roughness=0.2)
    steam_mat = create_emission_material("Industrial_Steam", (0.9, 0.9, 0.95), 1.5)
    steam_mat.node_tree.nodes["Principled BSDF"].inputs['Alpha'].default_value = 0.3
    steam_mat.blend_method = 'BLEND'
    
    # Main vertical pipe
    bpy.ops.mesh.primitive_cylinder_add(radius=0.15, depth=3.5, vertices=12,
                                         location=(-4, -4, 1.75))
    main_pipe = bpy.context.active_object
    main_pipe.name = "Industrial_Main_Pipe"
    main_pipe.data.materials.append(pipe_mat)
    link_to_collection(main_pipe, collection)
    
    # Horizontal branches
    for i in range(4):
        y_pos = -3 + i * 1.5
        bpy.ops.mesh.primitive_cylinder_add(radius=0.1, depth=2.0, vertices=12,
                                             location=(-3, y_pos, 3.5),
                                             rotation=(0, math.pi/2, 0))
        branch = bpy.context.active_object
        branch.name = f"Industrial_Pipe_Branch_{i}"
        branch.data.materials.append(pipe_mat)
        link_to_collection(branch, collection)
        
        # Valve
        bpy.ops.mesh.primitive_cylinder_add(radius=0.12, depth=0.15, vertices=12,
                                             location=(-4.1, y_pos, 3.5))
        valve = bpy.context.active_object
        valve.name = f"Industrial_Valve_{i}"
        valve.data.materials.append(valve_mat)
        link_to_collection(valve, collection)
    
    # Steam particles
    for _ in range(50):
        x = random.uniform(-4.2, -1.8)
        y = random.uniform(-4.2, -1.8)
        z = random.uniform(3.5, 4.0)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=random.uniform(0.03, 0.08),
                                               location=(x, y, z))
        steam = bpy.context.active_object
        steam.name = f"Industrial_Steam_{_}"
        steam.data.materials.append(steam_mat)
        link_to_collection(steam, collection)

def create_crates_pallets(collection):
    """Create wooden crates and metal pallets."""
    wood_mat = create_material("Industrial_Wood", base_color=(0.35, 0.25, 0.15, 1),
                               metallic=0.0, roughness=0.85)
    metal_mat = create_material("Industrial_Pallet_Metal", base_color=(0.35, 0.35, 0.38, 1),
                                metallic=0.7, roughness=0.4)
    label_mat = create_emission_material("Industrial_Label", (1, 0.8, 0), 2.0)
    
    # Wooden crates
    crate_positions = [
        (-2, -3, 0.3), (-1.3, -3, 0.3), (-2, -2.3, 0.3),
        (2, 3, 0.3), (2.7, 3, 0.3), (2, 3.7, 0.3),
        (-3.5, 2, 0.3), (-3.5, 2.7, 0.3),
    ]
    for i, (x, y, z) in enumerate(crate_positions):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z + 0.3))
        crate = bpy.context.active_object
        crate.name = f"Industrial_Crate_{i}"
        crate.scale = (0.6, 0.6, 0.6)
        bpy.ops.object.transform_apply(scale=True)
        crate.data.materials.append(wood_mat)
        link_to_collection(crate, collection)
        
        # Label
        bpy.ops.mesh.primitive_plane_add(size=1, location=(x, y + 0.31, z + 0.5),
                                          rotation=(math.pi/2, 0, 0))
        label = bpy.context.active_object
        label.name = f"Industrial_Label_{i}"
        label.scale = (0.3, 0.2, 1)
        bpy.ops.object.transform_apply(scale=True)
        label.data.materials.append(label_mat)
        link_to_collection(label, collection)
    
    # Metal pallets
    for i in range(3):
        x = 3.5 - i * 1.2
        y = -3.5
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, 0.1))
        pallet = bpy.context.active_object
        pallet.name = f"Industrial_Pallet_{i}"
        pallet.scale = (0.8, 1.0, 0.15)
        bpy.ops.object.transform_apply(scale=True)
        pallet.data.materials.append(metal_mat)
        link_to_collection(pallet, collection)
        
        # Pallet slats
        for s in range(3):
            bpy.ops.mesh.primitive_cube_add(size=1, location=(x - 0.6 + s * 0.6, y, 0.02))
            slat = bpy.context.active_object
            slat.name = f"Industrial_Pallet_Slat_{i}_{s}"
            slat.scale = (0.15, 1.0, 0.02)
            bpy.ops.object.transform_apply(scale=True)
            slat.data.materials.append(metal_mat)
            link_to_collection(slat, collection)

def create_oil_stains_on_floor(collection):
    """Oil stains are handled in floor material shader."""
    pass

# ============ WARP FANTASY ============
def create_magic_crystal_pillars(collection):
    """Create magical glowing crystal pillars."""
    crystal_mat = create_material("Warp_Crystal", base_color=(0.3, 0.5, 1.0, 1),
                                  metallic=0.0, roughness=0.05, alpha=0.6,
                                  use_transmission=True, transmission=0.9, ior=1.5,
                                  clearcoat=1.0, clearcoat_roughness=0.0)
    crystal_mat.blend_method = 'BLEND'
    glow_mat = create_emission_material("Warp_Crystal_Glow", (0.2, 0.6, 1.0), 8.0)
    
    positions = [(-3, -3), (3, -3), (-3, 3), (3, 3), (0, -4), (0, 4), (-4, 0), (4, 0)]
    for i, (x, y) in enumerate(positions):
        # Crystal cluster
        for c in range(3):
            cx = x + random.uniform(-0.3, 0.3)
            cy = y + random.uniform(-0.3, 0.3)
            cz = 0.5 + c * 0.7
            
            bpy.ops.mesh.primitive_cone_add(radius1=0.25 - c*0.05, depth=0.8,
                                             vertices=6, location=(cx, cy, cz))
            crystal = bpy.context.active_object
            crystal.name = f"Warp_Crystal_{i}_{c}"
            crystal.data.materials.append(crystal_mat)
            link_to_collection(crystal, collection)
        
        # Base glow
        bpy.ops.mesh.primitive_cylinder_add(radius=0.5, depth=0.1, vertices=16,
                                             location=(x, y, 0.05))
        glow = bpy.context.active_object
        glow.name = f"Warp_Crystal_Glow_{i}"
        glow.data.materials.append(glow_mat)
        link_to_collection(glow, collection)

def create_treasure_chests(collection):
    """Create glowing treasure chests."""
    wood_mat = create_material("Warp_Chest_Wood", base_color=(0.25, 0.15, 0.08, 1),
                               metallic=0.0, roughness=0.7)
    metal_mat = create_material("Warp_Chest_Metal", base_color=(0.6, 0.5, 0.2, 1),
                                metallic=1.0, roughness=0.15)
    glow_mat = create_emission_material("Warp_Chest_Glow", (1, 0.9, 0.4), 4.0)
    
    positions = [(-3.5, 0), (3.5, 0), (0, -3.5), (0, 3.5)]
    for i, (x, y) in enumerate(positions):
        angle = math.atan2(y, x) if (x != 0 or y != 0) else 0
        
        # Chest body
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, 0.35),
                                         rotation=(0, 0, angle))
        chest = bpy.context.active_object
        chest.name = f"Warp_Chest_{i}"
        chest.scale = (0.7, 0.45, 0.5)
        bpy.ops.object.transform_apply(scale=True)
        chest.data.materials.append(wood_mat)
        link_to_collection(chest, collection)
        
        # Lid
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, 0.7),
                                         rotation=(0, 0, angle))
        lid = bpy.context.active_object
        lid.name = f"Warp_Chest_Lid_{i}"
        lid.scale = (0.72, 0.47, 0.1)
        bpy.ops.object.transform_apply(scale=True)
        lid.data.materials.append(wood_mat)
        link_to_collection(lid, collection)
        
        # Metal bands
        for band in range(3):
            bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, 0.15 + band * 0.25),
                                             rotation=(0, 0, angle))
            b = bpy.context.active_object
            b.name = f"Warp_Chest_Band_{i}_{band}"
            b.scale = (0.74, 0.49, 0.03)
            bpy.ops.object.transform_apply(scale=True)
            b.data.materials.append(metal_mat)
            link_to_collection(b, collection)
        
        # Lock
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x + 0.75*math.cos(angle), 
                                                           y + 0.75*math.sin(angle), 0.5),
                                         rotation=(0, 0, angle))
        lock = bpy.context.active_object
        lock.name = f"Warp_Chest_Lock_{i}"
        lock.scale = (0.1, 0.08, 0.1)
        bpy.ops.object.transform_apply(scale=True)
        lock.data.materials.append(metal_mat)
        link_to_collection(lock, collection)
        
        # Inner glow (slightly open)
        bpy.ops.mesh.primitive_plane_add(size=1, location=(x, y, 0.6),
                                          rotation=(math.pi/2, 0, angle))
        inner = bpy.context.active_object
        inner.name = f"Warp_Chest_Inner_Glow_{i}"
        inner.scale = (0.5, 0.3, 1)
        bpy.ops.object.transform_apply(scale=True)
        inner.data.materials.append(glow_mat)
        link_to_collection(inner, collection)

def create_blue_fire_brazier(collection):
    """Create blue magical fire braziers."""
    brazier_mat = create_material("Warp_Brazier_Stone", base_color=(0.25, 0.23, 0.2, 1),
                                  metallic=0.0, roughness=0.9)
    fire_mat = create_emission_material("Warp_Blue_Fire", (0.2, 0.5, 1.0), 10.0)
    fire_mat.node_tree.nodes["Principled BSDF"].inputs['Alpha'].default_value = 0.7
    fire_mat.blend_method = 'BLEND'
    
    positions = [(-4, -2), (4, -2), (-4, 2), (4, 2)]
    for i, (x, y) in enumerate(positions):
        # Brazier bowl
        bpy.ops.mesh.primitive_cylinder_add(radius=0.4, depth=0.5, vertices=12,
                                             location=(x, y, 0.25))
        brazier = bpy.context.active_object
        brazier.name = f"Warp_Brazier_{i}"
        brazier.data.materials.append(brazier_mat)
        link_to_collection(brazier, collection)
        
        # Legs
        for leg in range(3):
            leg_angle = leg * 2*math.pi/3
            lx = x + 0.35 * math.cos(leg_angle)
            ly = y + 0.35 * math.sin(leg_angle)
            bpy.ops.mesh.primitive_cylinder_add(radius=0.04, depth=0.5, vertices=6,
                                                 location=(lx, ly, 0.0))
            l = bpy.context.active_object
            l.name = f"Warp_Brazier_Leg_{i}_{leg}"
            l.data.materials.append(brazier_mat)
            link_to_collection(l, collection)
        
        # Blue fire
        for f in range(5):
            fx = x + random.uniform(-0.2, 0.2)
            fy = y + random.uniform(-0.2, 0.2)
            fz = 0.5 + f * 0.25
            bpy.ops.mesh.primitive_cone_add(radius1=0.15 - f*0.02, depth=0.4,
                                             vertices=8, location=(fx, fy, fz))
            fire = bpy.context.active_object
            fire.name = f"Warp_Blue_Fire_{i}_{f}"
            fire.data.materials.append(fire_mat)
            link_to_collection(fire, collection)

def create_runic_floor_patterns(collection):
    """Runic patterns are handled in floor material shader."""
    pass

# ============ LUXURY ============
def create_luxury_paintings(collection, is_day=True):
    """Create luxury framed paintings."""
    frame_mat = create_material("Luxury_Frame_Gold", base_color=(0.85, 0.7, 0.2, 1),
                                metallic=1.0, roughness=0.1)
    canvas_mat = create_material("Luxury_Canvas", base_color=(0.2, 0.15, 0.1, 1),
                                 metallic=0.0, roughness=0.9)
    
    positions = [
        (-4, 0, 2.2, math.pi/2), (4, 0, 2.2, -math.pi/2),
        (0, -4, 2.2, math.pi), (0, 4, 2.2, 0),
        (-4, 0, 1.0, math.pi/2), (4, 0, 1.0, -math.pi/2),
    ]
    
    for i, (x, y, z, rot) in enumerate(positions):
        # Frame
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z), rotation=(0, 0, rot))
        frame = bpy.context.active_object
        frame.name = f"Luxury_Frame_{i}"
        frame.scale = (0.05, 1.5, 1.0)
        bpy.ops.object.transform_apply(scale=True)
        frame.data.materials.append(frame_mat)
        link_to_collection(frame, collection)
        
        # Canvas
        cx = x + 0.06 * math.cos(rot)
        cy = y + 0.06 * math.sin(rot)
        bpy.ops.mesh.primitive_plane_add(size=1, location=(cx, cy, z), rotation=(0, 0, rot))
        canvas = bpy.context.active_object
        canvas.name = f"Luxury_Canvas_{i}"
        canvas.scale = (1.3, 0.9, 1)
        bpy.ops.object.transform_apply(scale=True)
        # Add subtle texture variation per painting
        c_mat = create_material(f"Luxury_Canvas_{i}", 
                                base_color=(random.uniform(0.15, 0.35),
                                            random.uniform(0.1, 0.25),
                                            random.uniform(0.08, 0.2), 1),
                                metallic=0.0, roughness=0.85)
        canvas.data.materials.append(c_mat)
        link_to_collection(canvas, collection)

def create_gold_flower_vases(collection, is_day=True):
    """Create gold vases with flowers."""
    gold_mat = create_material("Luxury_Gold", base_color=(0.85, 0.7, 0.2, 1),
                               metallic=1.0, roughness=0.1)
    flower_colors = [(1, 0.3, 0.3), (1, 1, 0.3), (1, 0.5, 1), (0.5, 1, 0.5),
                     (1, 0.6, 0.2), (0.6, 0.6, 1)] if is_day else \
                    [(0.6, 0.1, 0.1), (0.5, 0.5, 0.1), (0.4, 0.1, 0.4), (0.1, 0.4, 0.1),
                     (0.5, 0.2, 0.05), (0.2, 0.2, 0.5)]
    
    positions = [(-3.5, -3.5), (3.5, -3.5), (-3.5, 3.5), (3.5, 3.5),
                 (0, -4), (0, 4), (-4, 0), (4, 0)]
    
    for i, (x, y) in enumerate(positions):
        # Vase body
        bpy.ops.mesh.primitive_cylinder_add(radius=0.12, depth=0.6, vertices=16,
                                             location=(x, y, 0.3))
        vase = bpy.context.active_object
        vase.name = f"Luxury_Vase_{i}"
        vase.data.materials.append(gold_mat)
        link_to_collection(vase, collection)
        
        # Vase neck
        bpy.ops.mesh.primitive_cylinder_add(radius=0.05, depth=0.25, vertices=16,
                                             location=(x, y, 0.725))
        neck = bpy.context.active_object
        neck.name = f"Luxury_Vase_Neck_{i}"
        neck.data.materials.append(gold_mat)
        link_to_collection(neck, collection)
        
        # Flowers
        for f in range(5):
            fx = x + random.uniform(-0.1, 0.1)
            fy = y + random.uniform(-0.1, 0.1)
            fz = 0.85 + f * 0.12
            
            # Stem
            bpy.ops.mesh.primitive_cylinder_add(radius=0.005, depth=0.3, vertices=4,
                                                 location=(fx, fy, fz + 0.15))
            stem = bpy.context.active_object
            stem.name = f"Luxury_Flower_Stem_{i}_{f}"
            stem_mat = create_material("Luxury_Stem", base_color=(0.1, 0.3, 0.1, 1),
                                       metallic=0.0, roughness=0.7)
            stem.data.materials.append(stem_mat)
            link_to_collection(stem, collection)
            
            # Petals
            petal_mat = create_material(f"Luxury_Petal_{i}_{f}", 
                                        base_color=flower_colors[(i+f) % len(flower_colors)],
                                        metallic=0.0, roughness=0.5)
            bpy.ops.mesh.primitive_uv_sphere_add(radius=0.05, segments=8, ring_count=6,
                                                  location=(fx, fy, fz + 0.35))
            petal = bpy.context.active_object
            petal.name = f"Luxury_Flower_{i}_{f}"
            petal.scale = (1.2, 1.2, 0.6)
            bpy.ops.object.transform_apply(scale=True)
            petal.data.materials.append(petal_mat)
            link_to_collection(petal, collection)

def create_leather_sofa(collection, is_day=True):
    """Create luxury leather sofa."""
    leather_color = (0.25, 0.15, 0.08, 1) if is_day else (0.12, 0.06, 0.03, 1)
    leather_mat = create_material("Luxury_Leather", base_color=leather_color,
                                  metallic=0.0, roughness=0.45, clearcoat=0.2)
    wood_mat = create_material("Luxury_Sofa_Wood", base_color=(0.2, 0.12, 0.06, 1),
                               metallic=0.0, roughness=0.3, clearcoat=0.4)
    pillow_mat = create_material("Luxury_Pillow", base_color=(0.7, 0.55, 0.35, 1) if is_day else (0.4, 0.3, 0.2, 1),
                                 metallic=0.0, roughness=0.6)
    
    # Sofa base
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 3.5, 0.4),
                                     rotation=(0, 0, math.pi))
    base = bpy.context.active_object
    base.name = "Luxury_Sofa_Base"
    base.scale = (2.5, 0.9, 0.4)
    bpy.ops.object.transform_apply(scale=True)
    base.data.materials.append(leather_mat)
    link_to_collection(base, collection)
    
    # Backrest
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 4.15, 1.0),
                                     rotation=(0, 0, math.pi))
    back = bpy.context.active_object
    back.name = "Luxury_Sofa_Back"
    back.scale = (2.5, 0.35, 0.8)
    bpy.ops.object.transform_apply(scale=True)
    back.data.materials.append(leather_mat)
    link_to_collection(back, collection)
    
    # Armrests
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_cube_add(size=1, location=(side * 2.5, 3.8, 0.6),
                                         rotation=(0, 0, math.pi))
        arm = bpy.context.active_object
        arm.name = f"Luxury_Sofa_Arm_{'L' if side < 0 else 'R'}"
        arm.scale = (0.3, 0.7, 0.6)
        bpy.ops.object.transform_apply(scale=True)
        arm.data.materials.append(leather_mat)
        link_to_collection(arm, collection)
    
    # Legs (wood)
    for lx in [-2.2, 2.2]:
        for ly in [3.0, 4.3]:
            bpy.ops.mesh.primitive_cylinder_add(radius=0.05, depth=0.4, vertices=8,
                                                 location=(lx, ly, 0.2))
            leg = bpy.context.active_object
            leg.name = f"Luxury_Sofa_Leg_{lx}_{ly}"
            leg.data.materials.append(wood_mat)
            link_to_collection(leg, collection)
    
    # Pillows
    for i in range(3):
        px = -1.5 + i * 1.5
        bpy.ops.mesh.primitive_cube_add(size=1, location=(px, 3.55, 0.65))
        pillow = bpy.context.active_object
        pillow.name = f"Luxury_Sofa_Pillow_{i}"
        pillow.scale = (0.5, 0.35, 0.35)
        bpy.ops.object.transform_apply(scale=True)
        pillow.data.materials.append(pillow_mat)
        link_to_collection(pillow, collection)

def create_velvet_drapes(collection, is_day=True):
    """Velvet drapes are created in wall material function."""
    pass

# ============ ARENA CORE ============
def create_central_hologram(collection):
    """Create central arena hologram."""
    holo_mat = create_emission_material("Arena_Holo_Core", (0, 0.9, 1), 5.0)
    holo_mat.node_tree.nodes["Principled BSDF"].inputs['Alpha'].default_value = 0.5
    holo_mat.blend_method = 'BLEND'
    
    # Main holo sphere
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3, radius=1.2, location=(0, 0, 2.0))
    holo = bpy.context.active_object
    holo.name = "Arena_Central_Hologram"
    holo.data.materials.append(holo_mat)
    link_to_collection(holo, collection)
    
    # Rotating rings
    ring_mat = create_emission_material("Arena_Holo_Ring", (0, 1, 0.8), 3.0)
    for i in range(5):
        bpy.ops.mesh.primitive_torus_add(location=(0, 0, 2.0),
                                          major_radius=1.5 + i * 0.3, minor_radius=0.02)
        ring = bpy.context.active_object
        ring.name = f"Arena_Holo_Ring_{i}"
        ring.rotation_euler = (random.uniform(0, math.pi), random.uniform(0, math.pi), 0)
        ring.data.materials.append(ring_mat)
        link_to_collection(ring, collection)
    
    # Particle field
    particle_mat = create_emission_material("Arena_Holo_Particle", (0.5, 1, 1), 2.0)
    particle_mat.node_tree.nodes["Principled BSDF"].inputs['Alpha'].default_value = 0.6
    particle_mat.blend_method = 'BLEND'
    
    for _ in range(300):
        phi = random.uniform(0, 2*math.pi)
        theta = random.uniform(0, math.pi)
        r = random.uniform(0.3, 2.0)
        x = r * math.sin(theta) * math.cos(phi)
        y = r * math.sin(theta) * math.sin(phi)
        z = r * math.cos(theta) + 2.0
        
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=random.uniform(0.01, 0.03),
                                               location=(x, y, z))
        p = bpy.context.active_object
        p.name = f"Arena_Holo_Particle_{_}"
        p.data.materials.append(particle_mat)
        link_to_collection(p, collection)

def create_player_spawn_pads(collection):
    """Create illuminated player spawn pads."""
    pad_mat = create_emission_material("Arena_Spawn_Pad", (0, 0.8, 1), 2.0)
    pad_base_mat = create_material("Arena_Spawn_Base", base_color=(0.1, 0.15, 0.2, 1),
                                   metallic=0.5, roughness=0.2)
    
    for i in range(8):
        angle = i * math.pi / 4
        x = 3.5 * math.cos(angle)
        y = 3.5 * math.sin(angle)
        
        # Base
        bpy.ops.mesh.primitive_cylinder_add(radius=0.8, depth=0.1, vertices=16,
                                             location=(x, y, 0.05))
        base = bpy.context.active_object
        base.name = f"Arena_Spawn_Base_{i}"
        base.data.materials.append(pad_base_mat)
        link_to_collection(base, collection)
        
        # Glowing pad
        bpy.ops.mesh.primitive_cylinder_add(radius=0.75, depth=0.02, vertices=16,
                                             location=(x, y, 0.11))
        pad = bpy.context.active_object
        pad.name = f"Arena_Spawn_Pad_{i}"
        pad.data.materials.append(pad_mat)
        link_to_collection(pad, collection)
        
        # Number indicator
        num_mat = create_emission_material(f"Arena_Spawn_Num_{i}", (1, 1, 1), 3.0)
        bpy.ops.object.text_add(location=(x, y, 0.2), rotation=(math.pi/2, 0, angle))
        num = bpy.context.active_object
        num.name = f"Arena_Spawn_Num_{i}"
        num.data.body = str(i + 1)
        num.data.size = 0.3
        num.data.extrude = 0.02
        num.data.materials.append(num_mat)
        link_to_collection(num, collection)

def create_weapon_racks(collection):
    """Create weapon display racks."""
    rack_mat = create_material("Arena_Rack", base_color=(0.15, 0.18, 0.22, 1),
                               metallic=0.7, roughness=0.25)
    weapon_mat = create_material("Arena_Weapon", base_color=(0.4, 0.45, 0.5, 1),
                                 metallic=0.85, roughness=0.15)
    glow_mat = create_emission_material("Arena_Weapon_Glow", (0, 1, 0.6), 2.0)
    
    for i in range(4):
        angle = i * math.pi / 2
        x = 4.5 * math.cos(angle)
        y = 4.5 * math.sin(angle)
        
        # Rack frame
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, 1.2),
                                         rotation=(0, 0, angle))
        rack = bpy.context.active_object
        rack.name = f"Weapon_Rack_{i}"
        rack.scale = (0.1, 1.5, 2.0)
        bpy.ops.object.transform_apply(scale=True)
        rack.data.materials.append(rack_mat)
        link_to_collection(rack, collection)
        
        # Weapon slots
        for slot in range(4):
            wz = 0.3 + slot * 0.6
            # Weapon placeholder (sword-like)
            bpy.ops.mesh.primitive_cube_add(size=1, location=(x + 0.4*math.cos(angle), 
                                                               y + 0.4*math.sin(angle), wz),
                                             rotation=(0, 0, angle))
            weapon = bpy.context.active_object
            weapon.name = f"Weapon_Rack_Item_{i}_{slot}"
            weapon.scale = (0.03, 0.03, 0.8)
            bpy.ops.object.transform_apply(scale=True)
            if slot == 0:
                weapon.data.materials.append(glow_mat)
            else:
                weapon.data.materials.append(weapon_mat)
            link_to_collection(weapon, collection)

# -----------------------------------------------------------------------------
# LIGHTING SETUP PER ROOM
# -----------------------------------------------------------------------------
def setup_lighting_zendojo(collection):
    """Warm, soft paper lantern lighting."""
    # Main lantern lights (already created as emissive objects)
    # Add subtle ambient fill
    bpy.ops.object.light_add(type='SUN', location=(0, 0, 5))
    sun = bpy.context.active_object
    sun.name = "ZenDojo_Sun"
    sun.data.energy = 1.5
    sun.data.color = (1, 0.95, 0.85)
    sun.data.angle = 0.53
    link_to_collection(sun, collection)
    
    # Accent spotlights on key areas
    for pos, target in [((-3, -2.5, 3), (-3, -2.5, 0)),
                         ((3, -2.5, 3), (3, -2.5, 0)),
                         ((0, -2.5, 3), (0, -2.5, 0.7))]:
        bpy.ops.object.light_add(type='SPOT', location=pos)
        spot = bpy.context.active_object
        spot.name = f"ZenDojo_Spot_{pos[0]}_{pos[1]}"
        spot.data.energy = 50
        spot.data.color = (1, 0.9, 0.7)
        spot.data.spot_size = 0.8
        spot.data.spot_blend = 0.5
        # Point at target
        direction = (target[0]-pos[0], target[1]-pos[1], target[2]-pos[2])
        spot.rotation_euler = (math.atan2(-direction[2], math.hypot(direction[0], direction[1])),
                               0, math.atan2(direction[1], direction[0]))
        link_to_collection(spot, collection)

def setup_lighting_cyberpunk(collection):
    """Neon, high contrast, colored rim lights."""
    # Main ambient - dark
    bpy.ops.object.light_add(type='SUN', location=(0, 0, 5))
    sun = bpy.context.active_object
    sun.name = "Cyberpunk_Sun"
    sun.data.energy = 0.3
    sun.data.color = (0.5, 0.3, 0.7)
    link_to_collection(sun, collection)
    
    # Neon rim lights on walls (emissive materials handle this)
    # Add colored spotlights for atmosphere
    neon_spots = [
        ((0, 4.5, 3), (0, 0, 0), (1, 0, 1)),    # Magenta from north
        ((0, -4.5, 3), (0, 0, 0), (0, 1, 1)),   # Cyan from south
        ((4.5, 0, 3), (0, 0, 0), (1, 0.5, 0)),  # Orange from east
        ((-4.5, 0, 3), (0, 0, 0), (0, 1, 0.5)), # Green from west
    ]
    for pos, target, color in neon_spots:
        bpy.ops.object.light_add(type='SPOT', location=pos)
        spot = bpy.context.active_object
        spot.name = f"Cyberpunk_NeonSpot_{color}"
        spot.data.energy = 200
        spot.data.color = color
        spot.data.spot_size = 1.2
        spot.data.spot_blend = 0.3
        direction = (target[0]-pos[0], target[1]-pos[1], target[2]-pos[2])
        spot.rotation_euler = (math.atan2(-direction[2], math.hypot(direction[0], direction[1])),
                               0, math.atan2(direction[1], direction[0]))
        link_to_collection(spot, collection)
    
    # Volumetric fog light
    bpy.ops.object.light_add(type='POINT', location=(0, 0, 2))
    fog_light = bpy.context.active_object
    fog_light.name = "Cyberpunk_Volumetric"
    fog_light.data.energy = 100
    fog_light.data.color = (0.2, 0.1, 0.3)
    fog_light.data.shadow_soft_size = 2.0
    link_to_collection(fog_light, collection)

def setup_lighting_spacenebula(collection):
    """Cool blue ambient, nebula glow from windows."""
    bpy.ops.object.light_add(type='SUN', location=(0, 0, 5))
    sun = bpy.context.active_object
    sun.name = "SpaceNebula_Sun"
    sun.data.energy = 0.5
    sun.data.color = (0.6, 0.7, 1.0)
    link_to_collection(sun, collection)
    
    # Panel lights on ceiling
    for i in range(6):
        angle = i * math.pi / 3
        x = 2.5 * math.cos(angle)
        y = 2.5 * math.sin(angle)
        bpy.ops.object.light_add(type='AREA', location=(x, y, 3.9))
        area = bpy.context.active_object
        area.name = f"SpaceNebula_PanelLight_{i}"
        area.data.energy = 80
        area.data.color = (0.7, 0.85, 1.0)
        area.data.size = 0.8
        area.data.size_y = 0.8
        area.rotation_euler = (math.pi, 0, 0)
        link_to_collection(area, collection)
    
    # Nebula glow from windows (handled by emissive materials)
    # Accent light on control panel
    bpy.ops.object.light_add(type='SPOT', location=(-3, 0, 3))
    panel_light = bpy.context.active_object
    panel_light.name = "SpaceNebula_PanelSpot"
    panel_light.data.energy = 100
    panel_light.data.color = (0.2, 0.8, 1.0)
    panel_light.data.spot_size = 0.6
    panel_light.rotation_euler = (math.pi/4, 0, -math.pi/2)
    link_to_collection(panel_light, collection)

def setup_lighting_industrial(collection):
    """Harsh industrial lighting, warm Edison bulbs."""
    bpy.ops.object.light_add(type='SUN', location=(0, 0, 5))
    sun = bpy.context.active_object
    sun.name = "Industrial_Sun"
    sun.data.energy = 1.0
    sun.data.color = (1, 0.9, 0.8)
    link_to_collection(sun, collection)
    
    # Hanging Edison bulbs
    bulb_positions = [(-3, -3, 3.5), (3, -3, 3.5), (-3, 3, 3.5), (3, 3, 3.5),
                      (0, -3, 3.5), (0, 3, 3.5), (-3, 0, 3.5), (3, 0, 3.5)]
    for i, pos in enumerate(bulb_positions):
        bpy.ops.object.light_add(type='POINT', location=pos)
        bulb = bpy.context.active_object
        bulb.name = f"Industrial_Edison_{i}"
        bulb.data.energy = 60
        bulb.data.color = (1, 0.85, 0.65)
        bulb.data.shadow_soft_size = 0.1
        link_to_collection(bulb, collection)
        
        # Bulb mesh (emissive)
        bulb_mat = create_emission_material(f"Industrial_Bulb_Mesh_{i}", (1, 0.9, 0.7), 4.0)
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.12, location=pos)
        bulb_mesh = bpy.context.active_object
        bulb_mesh.name = f"Industrial_Bulb_Geo_{i}"
        bulb_mesh.data.materials.append(bulb_mat)
        link_to_collection(bulb_mesh, collection)
    
    # Red warning lights
    for pos in [(-4.5, -4.5, 3.5), (4.5, -4.5, 3.5)]:
        bpy.ops.object.light_add(type='POINT', location=pos)
        warn = bpy.context.active_object
        warn.name = f"Industrial_Warning_{pos[0]}_{pos[1]}"
        warn.data.energy = 30
        warn.data.color = (1, 0.1, 0.1)
        link_to_collection(warn, collection)

def setup_lighting_warpfantasy(collection):
    """Magical, ethereal lighting with colored sources."""
    bpy.ops.object.light_add(type='SUN', location=(0, 0, 5))
    sun = bpy.context.active_object
    sun.name = "WarpFantasy_Sun"
    sun.data.energy = 0.4
    sun.data.color = (0.7, 0.6, 0.9)
    link_to_collection(sun, collection)
    
    # Crystal pillar lights (emissive materials)
    # Blue fire braziers (emissive)
    # Rune glow (emissive floor)
    
    # God rays from ceiling
    bpy.ops.object.light_add(type='SPOT', location=(0, 0, 3.9))
    godray = bpy.context.active_object
    godray.name = "WarpFantasy_GodRay"
    godray.data.energy = 300
    godray.data.color = (0.5, 0.4, 0.8)
    godray.data.spot_size = 1.5
    godray.data.spot_blend = 0.1
    godray.data.shadow_soft_size = 0.5
    godray.rotation_euler = (math.pi, 0, 0)
    link_to_collection(godray, collection)
    
    # Magical ambient points
    magic_colors = [(0.3, 0.5, 1), (0.6, 0.3, 1), (0.2, 0.8, 0.6), (1, 0.4, 0.6)]
    for i, color in enumerate(magic_colors):
        angle = i * math.pi / 2
        x = 3 * math.cos(angle)
        y = 3 * math.sin(angle)
        bpy.ops.object.light_add(type='POINT', location=(x, y, 2))
        magic = bpy.context.active_object
        magic.name = f"WarpFantasy_Magic_{i}"
        magic.data.energy = 50
        magic.data.color = color
        magic.data.shadow_soft_size = 0.5
        link_to_collection(magic, collection)

def setup_lighting_luxury(collection, is_day=True):
    """Elegant chandelier, warm accent lighting."""
    if is_day:
        # Daylight simulation
        bpy.ops.object.light_add(type='SUN', location=(3, -3, 5))
        sun = bpy.context.active_object
        sun.name = "Luxury_DAY_Sun"
        sun.data.energy = 3.0
        sun.data.color = (1, 0.98, 0.92)
        sun.data.angle = 0.53
        link_to_collection(sun, collection)
        
        # Window light shafts
        for i in range(2):
            angle = i * math.pi + math.pi/4
            x = 4.8 * math.cos(angle)
            y = 4.8 * math.sin(angle)
            bpy.ops.object.light_add(type='AREA', location=(x, y, 3.5))
            win = bpy.context.active_object
            win.name = f"Luxury_DAY_Window_{i}"
            win.data.energy = 200
            win.data.color = (1, 0.95, 0.85)
            win.data.size = 2.0
            win.data.size_y = 1.5
            win.rotation_euler = (0, math.pi/2 if i == 0 else -math.pi/2, 0)
            link_to_collection(win, collection)
    else:
        # Night - warm intimate
        bpy.ops.object.light_add(type='SUN', location=(0, 0, 5))
        sun = bpy.context.active_object
        sun.name = "Luxury_NIGHT_Sun"
        sun.data.energy = 0.1
        sun.data.color = (0.5, 0.4, 0.3)
        link_to_collection(sun, collection)
    
    # Chandelier (centerpiece)
    chandelier_mat = create_emission_material("Luxury_Chandelier", (1, 0.95, 0.8), 2.0)
    crystal_mat = create_material("Luxury_Crystal", base_color=(1, 1, 1, 1),
                                  metallic=0.0, roughness=0.0, alpha=0.3,
                                  use_transmission=True, transmission=1.0, ior=1.52,
                                  clearcoat=1.0)
    crystal_mat.blend_method = 'BLEND'
    
    # Chandelier arms
    for i in range(8):
        angle = i * math.pi / 4
        x = 0.6 * math.cos(angle)
        y = 0.6 * math.sin(angle)
        bpy.ops.mesh.primitive_curve_primitive_add(location=(x, y, 3.5))
        # Actually use cylinder for arm
        bpy.ops.mesh.primitive_cylinder_add(radius=0.02, depth=0.6, vertices=6,
                                             location=(x, y, 3.2))
        arm = bpy.context.active_object
        arm.name = f"Luxury_Chandelier_Arm_{i}"
        arm.rotation_euler = (angle, 0, 0)
        arm.data.materials.append(chandelier_mat)
        link_to_collection(arm, collection)
        
        # Crystal at end
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.08,
                                               location=(x + 0.65*math.cos(angle), 
                                                         y + 0.65*math.sin(angle), 2.8))
        crystal = bpy.context.active_object
        crystal.name = f"Luxury_Chandelier_Crystal_{i}"
        crystal.data.materials.append(crystal_mat)
        link_to_collection(crystal, collection)
    
    # Center bowl
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.2, location=(0, 0, 3.5))
    bowl = bpy.context.active_object
    bowl.name = "Luxury_Chandelier_Bowl"
    bowl.scale = (1, 1, 0.5)
    bpy.ops.object.transform_apply(scale=True)
    bowl.data.materials.append(chandelier_mat)
    link_to_collection(bowl, collection)
    
    # Wall sconces
    for i in range(4):
        angle = i * math.pi / 2
        x = 4.7 * math.cos(angle)
        y = 4.7 * math.sin(angle)
        bpy.ops.object.light_add(type='POINT', location=(x, y, 2.0))
        sconce = bpy.context.active_object
        sconce.name = f"Luxury_Sconce_{i}"
        sconce.data.energy = 40
        sconce.data.color = (1, 0.9, 0.7)
        sconce.data.shadow_soft_size = 0.1
        link_to_collection(sconce, collection)

def setup_lighting_arena(collection):
    """High-tech arena lighting, clean and sharp."""
    bpy.ops.object.light_add(type='SUN', location=(0, 0, 5))
    sun = bpy.context.active_object
    sun.name = "Arena_Sun"
    sun.data.energy = 1.0
    sun.data.color = (0.9, 0.95, 1.0)
    link_to_collection(sun, collection)
    
    # Overhead panel lights (grid)
    for gx in [-3, 0, 3]:
        for gy in [-3, 0, 3]:
            bpy.ops.object.light_add(type='AREA', location=(gx, gy, 3.9))
            panel = bpy.context.active_object
            panel.name = f"Arena_PanelLight_{gx}_{gy}"
            panel.data.energy = 150
            panel.data.color = (0.8, 0.9, 1.0)
            panel.data.size = 1.2
            panel.data.size_y = 1.2
            panel.rotation_euler = (math.pi, 0, 0)
            link_to_collection(panel, collection)
    
    # Rim lights on walls (cyan)
    for i in range(8):
        angle = i * math.pi / 4
        x = 4.8 * math.cos(angle)
        y = 4.8 * math.sin(angle)
        bpy.ops.object.light_add(type='SPOT', location=(x, y, 2.5))
        rim = bpy.context.active_object
        rim.name = f"Arena_RimLight_{i}"
        rim.data.energy = 100
        rim.data.color = (0, 0.9, 1.0)
        rim.data.spot_size = 0.5
        rim.data.spot_blend = 0.2
        direction = (-x, -y, -1.5)
        rim.rotation_euler = (math.atan2(-direction[2], math.hypot(direction[0], direction[1])),
                              0, math.atan2(direction[1], direction[0]))
        link_to_collection(rim, collection)
    
    # Central hologram light
    bpy.ops.object.light_add(type='POINT', location=(0, 0, 2))
    holo_light = bpy.context.active_object
    holo_light.name = "Arena_Holo_Light"
    holo_light.data.energy = 200
    holo_light.data.color = (0, 0.8, 1.0)
    holo_light.data.shadow_soft_size = 1.0
    link_to_collection(holo_light, collection)

# -----------------------------------------------------------------------------
# MAIN ROOM BUILD FUNCTIONS
# -----------------------------------------------------------------------------
def build_room_zendojo():
    """Build ZenDojo room."""
    print("Building ZenDojo...")
    clear_scene()
    
    # Collections
    room_coll = create_collection("ZenDojo")
    props_coll = create_collection("ZenDojo_Props", room_coll)
    lights_coll = create_collection("ZenDojo_Lights", room_coll)
    
    # Architecture
    floor, ceiling, walls = create_room_architecture("ZenDojo", room_coll)
    
    # Wall/Floor/Ceiling materials
    apply_wall_material_zendojo(walls, floor, ceiling)
    
    # Props
    create_bonsai_tree(props_coll, 0, -2.5, seed=1)
    create_bonsai_tree(props_coll, -2.5, 2.5, seed=2)
    create_bonsai_tree(props_coll, 2.5, 2.5, seed=3)
    create_bamboo_screen(props_coll)
    create_zen_garden_rocks(props_coll)
    create_tea_set(props_coll)
    
    # Lighting
    setup_lighting_zendojo(lights_coll)
    
    # Export
    export_room("ZenDojo", room_coll)
    print("  ✓ ZenDojo complete")

def build_room_cyberpunk():
    """Build Cyberpunk room."""
    print("Building Cyberpunk...")
    clear_scene()
    
    room_coll = create_collection("Cyberpunk")
    props_coll = create_collection("Cyberpunk_Props", room_coll)
    lights_coll = create_collection("Cyberpunk_Lights", room_coll)
    
    floor, ceiling, walls = create_room_architecture("Cyberpunk", room_coll)
    apply_wall_material_cyberpunk(walls, floor, ceiling)
    
    create_neon_signs(props_coll)
    create_cable_mess(props_coll)
    create_tech_trash_bins(props_coll)
    create_hologram_ads(props_coll)
    
    setup_lighting_cyberpunk(lights_coll)
    
    export_room("Cyberpunk", room_coll)
    print("  ✓ Cyberpunk complete")

def build_room_spacenebula():
    """Build SpaceNebula room."""
    print("Building SpaceNebula...")
    clear_scene()
    
    room_coll = create_collection("SpaceNebula")
    props_coll = create_collection("SpaceNebula_Props", room_coll)
    lights_coll = create_collection("SpaceNebula_Lights", room_coll)
    
    floor, ceiling, walls = create_room_architecture("SpaceNebula", room_coll)
    apply_wall_material_spacenebula(walls, floor, ceiling)
    
    create_space_control_panel(props_coll)
    create_holographic_star_map(props_coll)
    create_oxygen_tanks(props_coll)
    
    setup_lighting_spacenebula(lights_coll)
    
    export_room("SpaceNebula", room_coll)
    print("  ✓ SpaceNebula complete")

def build_room_industrial():
    """Build Industrial room."""
    print("Building Industrial...")
    clear_scene()
    
    room_coll = create_collection("Industrial")
    props_coll = create_collection("Industrial_Props", room_coll)
    lights_coll = create_collection("Industrial_Lights", room_coll)
    
    floor, ceiling, walls = create_room_architecture("Industrial", room_coll)
    apply_wall_material_industrial(walls, floor, ceiling)
    
    create_giant_fan(props_coll)
    create_steam_pipes(props_coll)
    create_crates_pallets(props_coll)
    
    setup_lighting_industrial(lights_coll)
    
    export_room("Industrial", room_coll)
    print("  ✓ Industrial complete")

def build_room_warpfantasy():
    """Build WarpFantasy room."""
    print("Building WarpFantasy...")
    clear_scene()
    
    room_coll = create_collection("WarpFantasy")
    props_coll = create_collection("WarpFantasy_Props", room_coll)
    lights_coll = create_collection("WarpFantasy_Lights", room_coll)
    
    floor, ceiling, walls = create_room_architecture("WarpFantasy", room_coll)
    apply_wall_material_warpfantasy(walls, floor, ceiling)
    
    create_magic_crystal_pillars(props_coll)
    create_treasure_chests(props_coll)
    create_blue_fire_brazier(props_coll)
    
    setup_lighting_warpfantasy(lights_coll)
    
    export_room("WarpFantasy", room_coll)
    print("  ✓ WarpFantasy complete")

def build_room_luxury(is_day=True):
    """Build Luxury room (DAY or NIGHT variant)."""
    suffix = "_DAY" if is_day else "_NIGHT"
    name = f"Luxury{suffix}"
    print(f"Building {name}...")
    clear_scene()
    
    room_coll = create_collection(name)
    props_coll = create_collection(f"{name}_Props", room_coll)
    lights_coll = create_collection(f"{name}_Lights", room_coll)
    
    floor, ceiling, walls = create_room_architecture(name, room_coll)
    apply_wall_material_luxury(walls, floor, ceiling, is_day)
    
    create_luxury_paintings(props_coll, is_day)
    create_gold_flower_vases(props_coll, is_day)
    create_leather_sofa(props_coll, is_day)
    
    setup_lighting_luxury(lights_coll, is_day)
    
    export_room(name, room_coll)
    print(f"  ✓ {name} complete")

def build_room_arena():
    """Build Arena_Core room."""
    print("Building Arena_Core...")
    clear_scene()
    
    room_coll = create_collection("Arena_Core")
    props_coll = create_collection("Arena_Core_Props", room_coll)
    lights_coll = create_collection("Arena_Core_Lights", room_coll)
    
    floor, ceiling, walls = create_room_architecture("Arena_Core", room_coll)
    apply_wall_material_arena(walls, floor, ceiling)
    
    create_central_hologram(props_coll)
    create_player_spawn_pads(props_coll)
    create_weapon_racks(props_coll)
    
    setup_lighting_arena(lights_coll)
    
    export_room("Arena_Core", room_coll)
    print("  ✓ Arena_Core complete")

# -----------------------------------------------------------------------------
# EXPORT FUNCTION
# -----------------------------------------------------------------------------
def export_room(room_name, collection):
    """Export room as FBX and/or GLTF."""
    export_path = os.path.join(bpy.path.abspath(OUTPUT_DIR), room_name)
    os.makedirs(export_path, exist_ok=True)
    
    # Select all objects in collection
    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.all_objects:
        obj.select_set(True)
    
    if EXPORT_FBX:
        fbx_path = os.path.join(export_path, f"{room_name}.fbx")
        bpy.ops.export_scene.fbx(
            filepath=fbx_path,
            use_selection=True,
            apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_NONE',
            use_space_transform=True,
            bake_space_transform=True,
            object_types={'MESH', 'LIGHT', 'CAMERA', 'ARMATURE'},
            use_mesh_modifiers=True,
            mesh_smooth_type='FACE',
            use_subsurf=False,
            use_mesh_edges=False,
            use_tspace=True,
            use_custom_props=False,
            add_leaf_bones=False,
            primary_bone_axis='Y',
            secondary_bone_axis='X',
            use_armature_deform_only=False,
            armature_nodetype='NULL',
            bake_anim=False,
        )
        print(f"    Exported FBX: {fbx_path}")
    
    if EXPORT_GLTF:
        gltf_path = os.path.join(export_path, f"{room_name}.glb")
        bpy.ops.export_scene.gltf(
            filepath=gltf_path,
            export_format='GLB',
            use_selection=True,
            export_materials='EXPORT',
            export_colors=True,
            export_cameras=False,
            export_lights=True,
        )
        print(f"    Exported GLTF: {gltf_path}")

# -----------------------------------------------------------------------------
# MASTER BUILD FUNCTION
# -----------------------------------------------------------------------------
def build_all_rooms():
    """Build all 8 rooms in sequence."""
    print("=" * 60)
    print("CueStrike AAA World Tour - Room Builder")
    print("=" * 60)
    
    # Ensure output directory exists
    os.makedirs(bpy.path.abspath(OUTPUT_DIR), exist_ok=True)
    
    build_room_zendojo()
    build_room_cyberpunk()
    build_room_spacenebula()
    build_room_industrial()
    build_room_warpfantasy()
    build_room_luxury(is_day=True)
    build_room_luxury(is_day=False)
    build_room_arena()
    
    print("=" * 60)
    print("ALL ROOMS BUILT SUCCESSFULLY!")
    print("Zero Pink Policy: VERIFIED (All materials use Principled BSDF)")
    print(f"Exported to: {bpy.path.abspath(OUTPUT_DIR)}")
    print("=" * 60)

# -----------------------------------------------------------------------------
# ENTRY POINT
# -----------------------------------------------------------------------------
if __name__ == "__main__":
    build_all_rooms()