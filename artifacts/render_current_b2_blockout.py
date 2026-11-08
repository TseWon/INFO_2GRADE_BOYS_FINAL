"""Refresh the preview images from the saved B2 Blender asset."""
import bpy
from pathlib import Path
from mathutils import Vector

root = Path(r"C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL")
bpy.ops.wm.open_mainfile(filepath=str(root / 'Assets/Blender/UndergroundResearchFacility/Underground_B2_Blockout.blend'))
scene = bpy.context.scene
preview = root / 'artifacts/previews'
preview.mkdir(exist_ok=True)
scene.render.filepath = str(preview / 'Underground_B2_Blockout.png')
bpy.ops.render.render(write_still=True)

coords = [o.matrix_world @ Vector(c) for o in scene.objects if o.type=='MESH' for c in o.bound_box]
xmin,xmax = min(p.x for p in coords),max(p.x for p in coords)
ymin,ymax = min(p.y for p in coords),max(p.y for p in coords)
cam = scene.camera
cam.location = ((xmin+xmax)/2,(ymin+ymax)/2,100)
cam.rotation_euler = (0,0,0)
cam.data.ortho_scale = max((xmax-xmin)*1.12,(ymax-ymin)*1.12*1400/900)
scene.render.filepath = str(preview / 'Underground_B2_Blockout_Plan.png')
bpy.ops.render.render(write_still=True)
print('PREVIEWS',scene.render.filepath)
