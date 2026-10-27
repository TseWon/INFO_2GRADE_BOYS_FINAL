import bpy,os,math,json,numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
exec(compile(open(os.path.join(ROOT,'artifacts/anatomy_review.py'),encoding='utf-8').read(),'review','exec'))
arrays=np.load(os.path.join(ART,'back_thorax_before_meshes.npz'));carrier=bpy.data.objects['TorsoEquipment_Coyote_Cordura']
parts=components(carrier);trees={};pack=None
for part in parts:
 center=sum((carrier.data.vertices[i].co for i in part),Vector())/len(part)
 if len(part)>500:
  keep=set(part);side=1 if center.y>0 else -1
  trees[side]=BVHTree.FromPolygons([v.co for v in carrier.data.vertices],[list(p.vertices) for p in carrier.data.polygons if all(i in keep for i in p.vertices)])
 if len(part)==96 and center.y>.18:pack=center
assert len(trees)==2
dy_pack=pack.y-.202
for name in ['LimbEquipment_Webbing','Details_DO_Seam_Thread','TorsoEquipment_Webbing']:
 o=bpy.data.objects[name];old=arrays[name]
 for part in components(o):
  pts=[Vector(old[i]) for i in part];center=sum(pts,Vector())/len(pts)
  if not 1.10<center.z<1.46:continue
  if center.y>.20 and abs(center.x)<.13:
   for i,p in zip(part,pts):o.data.vertices[i].co=p+Vector((0,dy_pack,0))
   continue
  tape=len(part)==44 and name=='LimbEquipment_Webbing'
  stitching=len(part)==16 and name=='Details_DO_Seam_Thread'
  if not (tape or stitching):continue
  side=-1 if center.y<-.12 and center.y>-.19 else (1 if .14<center.y<.19 else 0)
  if not side:continue
  for i,p in zip(part,pts):
   hit=trees[side].ray_cast(Vector((p.x,side*.5,p.z)),Vector((0,-side,0)))[0]
   if hit is None:continue
   near=[q.y for q in pts if abs(q.x-p.x)<.0002]
   thickness=p.y-(min(near)+max(near))*.5
   o.data.vertices[i].co.y=hit.y+side*(.002 if tape else .004)+thickness
 o.data.update()
# Add restrained padding and knuckle fullness to the existing gloved hands.
o=bpy.data.objects['Gloves_and_Boot_Uppers'];rig=bpy.data.objects['DesertOperator_Rig'];changed=0
if not o.get('glove_shape_revision_20261027'):
 for v in o.data.vertices:
  p=v.co.copy()
  if p.z<.78:continue
  side='_L' if p.x<0 else '_R'
  candidates=[]
  for b in rig.data.bones:
   if not b.name.endswith(side) or not b.name.startswith(('Thumb','Index','Middle','Ring','Little')):continue
   a=b.head_local;d=b.tail_local-a;t=max(0,min(1,(p-a).dot(d)/d.length_squared));q=a+d*t
   candidates.append(((p-q).length,q,b,t))
  dist,q,b,t=min(candidates,key=lambda x:x[0])
  if p.z<.893 and dist<.015:
   # Small radial padding, preserving finger length and each joint location.
   gain=.07+.045*math.exp(-((t-.08)/.23)**2);v.co=q+(p-q)*(1+gain);changed+=1
  elif .895<p.z<.95:
   strength=math.exp(-((p.z-.920)/.026)**2)
   v.co.y+=.0018*strength*max(0,min(1,(p.y+.02)/.025));changed+=1
 o.data.update();o['glove_shape_revision_20261027']=changed
print('Existing glove vertices refined',changed,'hydration accessories shift',dy_pack)
bpy.context.scene.render.engine='CYCLES';bpy.context.scene.cycles.samples=20;bpy.context.scene.cycles.use_denoising=True
if globals().get('RENDER',True):review('backthorax_textured',False)
