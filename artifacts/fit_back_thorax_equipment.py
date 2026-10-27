import bpy,bmesh,math,os,numpy as np,json
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
exec(compile(open(os.path.join(ROOT,'artifacts/anatomy_review.py'),encoding='utf-8').read(),'review','exec'))
def g(x,c,w):return math.exp(-((x-c)/w)**2)
def step(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
body=bpy.data.objects['Uniform_Ripstop'];carrier=bpy.data.objects['TorsoEquipment_Coyote_Cordura'];col=bpy.data.collections['01_CHARACTER - rigged export']
assert not carrier.get('back_thorax_fit_20261027')
bt=BVHTree.FromPolygons([v.co for v in body.data.vertices],[list(p.vertices) for p in body.data.polygons])
panels={};panel_ids={}
for part in components(carrier):
 if len(part)==208:
  side=1 if sum(carrier.data.vertices[i].co.y for i in part)>0 else -1;keep=set(part);panel_ids[side]=keep
  panels[side]=BVHTree.FromPolygons([v.co for v in carrier.data.vertices],[list(p.vertices) for p in carrier.data.polygons if all(i in keep for i in p.vertices)])
def shift(side,x,z):
 # Sample just inside the panel boundary. Interior points use exact ray contacts.
 z=max(1.115,min(1.438,z));x=max(-.17,min(.17,x))
 for scale in [1,.95,.85,.7]:
  xx=x*scale
  skin=bt.ray_cast(Vector((xx,side*.5,z)),Vector((0,-side,0)))[0]
  inner=panels[side].ray_cast(Vector((xx,-side*.5,z)),Vector((0,side,0)))[0]
  if skin is not None and inner is not None:return skin.y+side*.008-inner.y
 return side*.023
changes={}
# Move sewn webbing along the panel's curvature; keep compact pouches rigid.
for o in col.objects:
 if o.type!='MESH' or o in [body,carrier] or o.name.startswith(('Head','Helmet','Goggle','Belt','Quick','Buckle')):continue
 count=0
 for part in components(o):
  pts=[o.data.vertices[i].co.copy() for i in part];center=sum(pts,Vector())/len(pts)
  names={o.vertex_groups[w.group].name for i in part for w in o.data.vertices[i].groups if w.weight>.2}
  if 'Chest' not in names or not (1.08<center.z<1.50):continue
  if abs(center.y)>.09:
   side=1 if center.y>0 else -1
   zspan=max(p.z for p in pts)-min(p.z for p in pts)
   yspan=max(p.y for p in pts)-min(p.y for p in pts)
   rigid=(yspan>.026 and zspan<.29 and len(part)>60)
   delta=shift(side,center.x,center.z)
   for i,p in zip(part,pts):o.data.vertices[i].co.y+=delta if rigid else shift(side,p.x,p.z)
   count+=len(part)
  elif max(p.x for p in pts)-min(p.x for p in pts)>.025 and abs(center.x)>.14:
   # Cummerbund wraps the expanded side of the rib cage.
   for i,p in zip(part,pts):
    o.data.vertices[i].co.x+=math.copysign(.008*g(p.z,1.27,.13),p.x)
    t=step(-.12,.13,p.y);o.data.vertices[i].co.y+=(1-t)*shift(-1,p.x,p.z)+t*shift(1,p.x,p.z)
   count+=len(part)
 if count:o.data.update();changes[o.name]=count
# Carrier pouches and bridges, before subdividing the two actual panels.
for part in components(carrier):
 if len(part)==208:continue
 pts=[carrier.data.vertices[i].co.copy() for i in part];center=sum(pts,Vector())/len(pts)
 if abs(center.y)>.09:
  dy=shift(1 if center.y>0 else -1,center.x,center.z)
  for i in part:carrier.data.vertices[i].co.y+=dy
 else:
  for i,p in zip(part,pts):
   t=step(-.15,.16,p.y)
   carrier.data.vertices[i].co.y+=(1-t)*shift(-1,p.x,min(p.z,1.41))+t*shift(1,p.x,min(p.z,1.41))
   carrier.data.vertices[i].co.z+=.015*g(p.z,1.49,.055)
bm=bmesh.new();bm.from_mesh(carrier.data);bm.verts.ensure_lookup_table();tag=bm.verts.layers.float.new('plate_side_edit')
for v in bm.verts:v[tag]=1 if v.index in panel_ids[1] else (-1 if v.index in panel_ids[-1] else 0)
bmesh.ops.triangulate(bm,faces=[f for f in bm.faces if all(abs(v[tag])>.5 for v in f.verts)])
for iteration in range(3):
 edges=[e for e in bm.edges if abs(e.verts[0][tag])>.5 and e.verts[0][tag]*e.verts[1][tag]>.5 and e.calc_length()>.025]
 if not edges:break
 bmesh.ops.subdivide_edges(bm,edges=edges,cuts=1,use_grid_fill=True)
for v in bm.verts:
 if abs(v[tag])>.5:v.co.y+=shift(1 if v[tag]>0 else -1,v.co.x,v.co.z)
bm.verts.layers.float.remove(tag);bm.to_mesh(carrier.data);bm.free();carrier.data.update()
carrier.data.normals_split_custom_set([(0,0,0)]*len(carrier.data.loops));carrier['back_thorax_fit_20261027']=True
changes[carrier.name]=len(carrier.data.vertices)
with open(os.path.join(ART,'back_thorax_equipment_changes.json'),'w') as f:json.dump(changes,f,indent=2)
print('Equipment fitted',changes)
review('backthorax_equipped_clay',False)
