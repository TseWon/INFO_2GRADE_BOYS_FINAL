import bpy,json
from mathutils import Vector
from pathlib import Path
root=Path(r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL')
bpy.ops.wm.open_mainfile(filepath=str(root/'Assets/Blender/SurfaceResearchStation/SurfaceResearchStation.blend'))
out=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 p=[o.matrix_world@Vector(v) for v in o.bound_box]
 out.append(dict(name=o.name,module=o.get('module'),min=[round(min(v[i] for v in p),3) for i in range(3)],max=[round(max(v[i] for v in p),3) for i in range(3)],materials=[m.name for m in o.data.materials]))
(root/'artifacts/current_surface_inventory.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
print('MESHES',len(out))
