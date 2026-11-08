"""Close the red-outlined experiment room and clear its connecting side walls."""
import bpy
import os
from mathutils import Vector

ROOT = r"C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL"
SOURCE = os.path.join(ROOT,'artifacts','backups','Underground_B2_before_red_box_20261108.blend')
DEST = os.path.join(ROOT,'Assets','Blender','UndergroundResearchFacility','Underground_B2_Blockout.blend')
PREV = os.path.join(ROOT,'artifacts','previews')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.open_mainfile(filepath=SOURCE)
bpy.context.preferences.filepaths.save_version=0

# The connector's side walls made a long continuous hall through the red box.
# Keep its floor, but clear the walls and overhead slab so the surrounding
# north/south corridors remain open at this junction.
targets=[]
for obj in list(bpy.data.objects):
    n=obj.name
    if n.startswith('B2 full width closed observation connector | north') or \
       n.startswith('B2 full width closed observation connector | south') or \
       n=='B2 full width closed observation connector | ceiling':
        targets.append(n)
        bpy.data.objects.remove(obj,do_unlink=True)

# Complete the experiment room's missing east side (x=7.8 m).  The west side
# retains its existing viewing wall from the hexagonal containment room.
coll=bpy.data.collections['20_B2_Rooms_and_Corridors']
wall_mat=next(m for m in bpy.data.materials if 'aged cold concrete' in m.name)
bpy.ops.mesh.primitive_cube_add(size=1,location=(7.8,0,1.85))
wall=bpy.context.object
wall.name='B2 experiment room | E closed box wall'
wall.dimensions=(.28,14,3.7)
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
wall.data.materials.append(wall_mat)
for parent in list(wall.users_collection):
    parent.objects.unlink(wall)
coll.objects.link(wall)

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
print('REMOVED',len(targets),*targets,sep='\n')
print('ADDED',wall.name)
print('SAVED',DEST)
