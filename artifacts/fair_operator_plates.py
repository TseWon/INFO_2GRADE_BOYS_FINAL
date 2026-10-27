import bpy,os,numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
exec(compile(open(os.path.join(ROOT,'artifacts/anatomy_review.py'),encoding='utf-8').read(),'review','exec'))
body=bpy.data.objects['Uniform_Ripstop'];o=bpy.data.objects['TorsoEquipment_Coyote_Cordura']
bt=BVHTree.FromPolygons([v.co for v in body.data.vertices],[list(p.vertices) for p in body.data.polygons])
def basis(x,z):
 u=x/.18;v=(z-1.28)/.16
 return np.array([1,u*u,u**4,v,v*v,v**3,u*u*v,u*u*v*v])
for part in components(o):
 if len(part)<500:continue
 center=sum((o.data.vertices[i].co for i in part),Vector())/len(part);side=1 if center.y>0 else -1
 keep=set(part);tree=BVHTree.FromPolygons([v.co for v in o.data.vertices],[list(p.vertices) for p in o.data.polygons if all(i in keep for i in p.vertices)])
 rows=[];values=[]
 for x in np.linspace(-.165,.165,17):
  for z in np.linspace(1.12,1.437,24):
   inner=tree.ray_cast(Vector((x,-side*.5,z)),Vector((0,side,0)))[0]
   skin=bt.ray_cast(Vector((x,side*.5,z)),Vector((0,-side,0)))[0]
   if inner is not None and skin is not None:rows.append(basis(x,z));values.append(side*skin.y+.008)
 rows=np.array(rows);values=np.array(values);coef=np.linalg.lstsq(rows,values,rcond=None)[0]
 offset=max(0,float(np.max(values-rows@coef)))+.001
 moves=[]
 for i in part:
  p=o.data.vertices[i].co.copy()
  inner=tree.ray_cast(Vector((p.x,-side*.5,p.z)),Vector((0,side,0)))[0]
  outer=tree.ray_cast(Vector((p.x,side*.5,p.z)),Vector((0,-side,0)))[0]
  if inner is None or outer is None:
   xx=p.x*.98;zz=max(1.113,min(1.437,p.z))
   inner=tree.ray_cast(Vector((xx,-side*.5,zz)),Vector((0,side,0)))[0]
   outer=tree.ray_cast(Vector((xx,side*.5,zz)),Vector((0,-side,0)))[0]
  if inner is None or outer is None:continue
  thick=abs(outer.y-inner.y)
  fraction=max(0,min(1,side*(p.y-inner.y)/max(thick,.001)))
  target=max(.055,float(basis(p.x,p.z)@coef+offset))
  y=side*(target+fraction*(.042 if side<0 else .04));moves.append((i,y))
 for i,y in moves:o.data.vertices[i].co.y=y
 print('Fair plate',side,'samples',len(rows),'clearance safety offset mm',round(offset*1000,2))
o.data.normals_split_custom_set([(0,0,0)]*len(o.data.loops));o.data.update()
