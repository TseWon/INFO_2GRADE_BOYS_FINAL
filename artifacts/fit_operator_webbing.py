import bpy,os
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
exec(compile(open(os.path.join(ROOT,'artifacts/anatomy_review.py'),encoding='utf-8').read(),'review','exec'))
carrier=bpy.data.objects['TorsoEquipment_Coyote_Cordura']
if not carrier.get('contact_fit_20261027'):
 ids=set()
 for part in components(carrier):
  if len(part)==208:ids.update(part)
 faces=[list(p.vertices) for p in carrier.data.polygons if all(i in ids for i in p.vertices)]
 tree=BVHTree.FromPolygons([v.co for v in carrier.data.vertices],faces)
 for name in ['LimbEquipment_Webbing','Details_DO_Seam_Thread']:
  o=bpy.data.objects[name];fitted=0
  for part in components(o):
   pts=[v.co.copy() for i in part for v in [o.data.vertices[i]]]
   zmin=min(p.z for p in pts);zmax=max(p.z for p in pts)
   if not (zmin>1.1 and zmax<1.44):continue
   front=max(p.y for p in pts)<-.12 and min(p.y for p in pts)>-.20
   back=min(p.y for p in pts)>.14 and max(p.y for p in pts)<.195
   if not (front or back):continue
   sign=-1 if front else 1
   for i,p in zip(part,pts):
    hit,normal,fi,dist=tree.ray_cast(Vector((p.x,sign*.5,p.z)),Vector((0,-sign,0)))
    if hit is None:continue
    # Preserve tape thickness while fitting it to the actual carrier surface.
    near=[q.y for q in pts if abs(q.x-p.x)<.0002]
    delta=p.y-(min(near)+max(near))*.5
    o.data.vertices[i].co.y=hit.y+sign*(.003 if name.startswith('Limb') else .005)+delta
   fitted+=1
  o.data.update();print('Fitted woven components',name,fitted)
 for name in ['TorsoEquipment_Coyote_Cordura','TorsoEquipment_Webbing','TorsoEquipment_Seam_Thread']:
  o=bpy.data.objects[name]
  for part in components(o):
   pts=[o.data.vertices[i].co for i in part]
   if max(p.y for p in pts)<-.15 and min(p.z for p in pts)>1.10 and max(p.z for p in pts)<1.30:
    for i in part:o.data.vertices[i].co.y+=.011
  o.data.update()
 carrier['contact_fit_20261027']=True
bpy.context.scene.render.engine='CYCLES';bpy.context.scene.cycles.samples=24
review('final_textured',False)
