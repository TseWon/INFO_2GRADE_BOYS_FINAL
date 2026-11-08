"""Widen the current B2 exit stair and its approach to the B1 hall's 16 m span."""
import bpy
import math
import os
from pathlib import Path

ROOT = Path(r"C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL")
PATH = ROOT / 'Assets/Blender/UndergroundResearchFacility/Underground_B2_Blockout.blend'
bpy.ops.wm.open_mainfile(filepath=str(PATH))
bpy.context.preferences.filepaths.save_version = 0

coll = bpy.data.objects['B2 staircase | step 01'].users_collection[0]
wall_mat = bpy.data.objects['B2 emergency stair | E | wall 0'].data.materials[0]
yellow_mat = bpy.data.objects['B2 emergency stair | W | safety threshold'].data.materials[0]

def remove_prefix(prefix):
    for obj in list(bpy.data.objects):
        if obj.name.startswith(prefix):
            bpy.data.objects.remove(obj, do_unlink=True)

def box(name, xyz, size, material, angle=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=xyz)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.rotation_euler.z = angle
    obj.data.materials.append(material)
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    coll.objects.link(obj)
    return obj

def resize(name, size, location=None):
    obj = bpy.data.objects[name]
    obj.dimensions = size
    if location is not None:
        obj.location = location
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.select_set(False)

# The 21 m deep escape room keeps 2.5 m of end wall on each side.
remove_prefix('B2 equally sized specimen escape room | E |')
box('B2 equally sized specimen escape room | E | wall 0',
    (28.8,-9.25,1.85),(2.5,.28,3.7),wall_mat,math.pi/2)
box('B2 equally sized specimen escape room | E | wall 1',
    (28.8,9.25,1.85),(2.5,.28,3.7),wall_mat,math.pi/2)
box('B2 equally sized specimen escape room | E | lintel',
    (28.8,0,3.2),(16,.28,1),wall_mat,math.pi/2)
box('B2 equally sized specimen escape room | E | safety threshold',
    (28.8,0,.008),(16,.08,.016),yellow_mat,math.pi/2)

resize('B2 stair passage | floor',(3,16,.25))
resize('B2 stair passage | ceiling',(3,16,.2))
for direction,sign in [('south',-1),('north',1)]:
    resize(f'B2 stair passage | {direction} | wall 0',
           (3,.28,3.7),(30.3,sign*8,1.85))

resize('B2 emergency stair | floor',(8,16,.25))
resize('B2 emergency stair | ceiling',(8,16,.2))
for side,sign in [('S',-1),('N',1)]:
    resize(f'B2 emergency stair | {side} | wall 0',
           (8,.28,3.7),(35.8,sign*8,1.85))
resize('B2 emergency stair | E | wall 0',(16,.28,3.7),(39.8,0,1.85))
remove_prefix('B2 emergency stair | W |')
box('B2 emergency stair | W | lintel',
    (31.8,0,3.2),(16,.28,1),wall_mat,-math.pi/2)
box('B2 emergency stair | W | safety threshold',
    (31.8,0,.008),(16,.08,.016),yellow_mat,-math.pi/2)
for i in range(1,13):
    obj = bpy.data.objects[f'B2 staircase | step {i:02d}']
    size = list(obj.dimensions)
    size[1] = 16
    resize(obj.name,tuple(size))

assert abs(bpy.data.objects['B2 staircase | step 01'].dimensions.y - 16)<.001
bpy.ops.wm.save_as_mainfile(filepath=str(PATH))
print('SAVED',PATH)
print('B2_STAIR_WIDTH',bpy.data.objects['B2 staircase | step 01'].dimensions.y)
