"""Restore the last approved B2 layout, then clear only the marked barriers."""
import bpy
import os
from mathutils import Vector

ROOT = r"C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL"
SOURCE = os.path.join(ROOT, 'artifacts', 'backups', 'Underground_B2_before_open_corridor_20261107.blend')
DEST = os.path.join(ROOT, 'Assets', 'Blender', 'UndergroundResearchFacility', 'Underground_B2_Blockout.blend')
PREV = os.path.join(ROOT, 'artifacts', 'previews')

bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.open_mainfile(filepath=SOURCE)
bpy.context.preferences.filepaths.save_version = 0

# The first glass wall is traced red; the far glass wall and the outer
# corridor's hanging end beam/posts are X-marked in the reference.
remove_prefixes = (
    'B2 experiment room separate east viewing wall',
    'B2 specimen room separate west viewing wall',
    'B2 continuous outside corridor | boundary 3',
)
removed = []
for obj in list(bpy.data.objects):
    if obj.name.startswith(remove_prefixes):
        removed.append(obj.name)
        bpy.data.objects.remove(obj, do_unlink=True)

coll = bpy.data.collections['20_B2_Rooms_and_Corridors']
floor_mat = next(m for m in bpy.data.materials if 'worn industrial floor' in m.name)
wall_mat = next(m for m in bpy.data.materials if 'aged cold concrete' in m.name)

def box(name, center, size, mat):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj=bpy.context.object
    obj.name=name
    obj.dimensions=size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    for parent in list(obj.users_collection):
        parent.objects.unlink(obj)
    coll.objects.link(obj)
    return obj

# Extend just the existing upper and lower 3.5 m side corridors.  Keep the two
# 14 x 14 m room modules and the central 7 m link at their original positions.
for side in (-1,1):
    name='south' if side < 0 else 'north'
    y=side*8.75
    box(f'B2 {name} side corridor continuation | floor',
        (19.8,y,-.145),(18,3.5,.25),floor_mat)
    box(f'B2 {name} side corridor continuation | outer wall',
        (19.8,side*10.5,1.85),(18,.28,3.7),wall_mat)
    box(f'B2 {name} side corridor continuation | end cap',
        (28.8,y,1.85),(.28,3.5,3.7),wall_mat)

scene=bpy.context.scene
scene.render.filepath=os.path.join(PREV,'Underground_B2_Blockout.png')
bpy.ops.wm.save_as_mainfile(filepath=DEST)
bpy.ops.render.render(write_still=True)

cam=scene.camera
coords=[]
for obj in scene.objects:
    if obj.type == 'MESH':
        coords.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
xmin=min(p.x for p in coords);xmax=max(p.x for p in coords)
ymin=min(p.y for p in coords);ymax=max(p.y for p in coords)
cam.location=((xmin+xmax)/2,(ymin+ymax)/2,100)
cam.rotation_euler=(0,0,0)
cam.data.ortho_scale=max((xmax-xmin)*1.12,(ymax-ymin)*1.12*1400/900)
scene.render.filepath=os.path.join(PREV,'Underground_B2_Blockout_Plan.png')
bpy.ops.render.render(write_still=True)

print('RESTORED_SOURCE', SOURCE)
print('REMOVED_COUNT', len(removed))
for name in removed:
    print('REMOVED', name)
print('SAVED', DEST)
