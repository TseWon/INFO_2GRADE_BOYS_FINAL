"""Open the right B2 room to both side corridors and expand its floor/roof."""
import bpy
import os
from mathutils import Vector

ROOT=r"C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL"
SOURCE=os.path.join(ROOT,'artifacts','backups','Underground_B2_before_wide_right_room_20261108.blend')
DEST=os.path.join(ROOT,'Assets','Blender','UndergroundResearchFacility','Underground_B2_Blockout.blend')
PREV=os.path.join(ROOT,'artifacts','previews')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.open_mainfile(filepath=SOURCE)
bpy.context.preferences.filepaths.save_version=0

room='B2 equally sized specimen escape room'
remove_names=(f'{room} | N | wall 0',f'{room} | S | wall 0')
for name in remove_names:
    obj=bpy.data.objects.get(name)
    if obj is None:
        raise RuntimeError(f'Missing requested wall: {name}')
    bpy.data.objects.remove(obj,do_unlink=True)

for suffix in ('floor','ceiling'):
    obj=bpy.data.objects[f'{room} | {suffix}']
    obj.dimensions=(14,21,.25 if suffix=='floor' else .20)

# The expanded room now covers the previous side-corridor floor tiles.  Leave
# only the four-metre transition tiles between the old shell and the room.
for direction in ('north','south'):
    obj=bpy.data.objects[f'B2 {direction} side corridor continuation | floor']
    obj.location.x=12.8
    obj.dimensions=(4,3.5,.25)

scene=bpy.context.scene
scene.render.filepath=os.path.join(PREV,'Underground_B2_Blockout.png')
bpy.ops.wm.save_as_mainfile(filepath=DEST)
bpy.ops.render.render(write_still=True)

cam=scene.camera
coords=[]
for obj in scene.objects:
    if obj.type=='MESH':
        coords.extend(obj.matrix_world @ Vector(p) for p in obj.bound_box)
xmin=min(p.x for p in coords);xmax=max(p.x for p in coords)
ymin=min(p.y for p in coords);ymax=max(p.y for p in coords)
cam.location=((xmin+xmax)/2,(ymin+ymax)/2,100)
cam.rotation_euler=(0,0,0)
cam.data.ortho_scale=max((xmax-xmin)*1.12,(ymax-ymin)*1.12*1400/900)
scene.render.filepath=os.path.join(PREV,'Underground_B2_Blockout_Plan.png')
bpy.ops.render.render(write_still=True)
print('REMOVED_WALLS',*remove_names,sep='\n')
print('ROOM_SIZE',tuple(round(v,3) for v in bpy.data.objects[f'{room} | floor'].dimensions))
print('SAVED',DEST)
