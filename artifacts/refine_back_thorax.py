import bpy,bmesh,math,os,json,numpy as np
from mathutils import Vector
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
exec(compile(open(os.path.join(ROOT,'artifacts/anatomy_review.py'),encoding='utf-8').read(),'review','exec'))
def g(x,c,w):return math.exp(-((x-c)/w)**2)
def step(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def torso_displacement(p):
 x,y,z=p;a=abs(x);sign=1 if x>=0 else -1
 core=1-step(.18,.265,a)
 rear=step(.005,.105,y);front=1-step(-.11,-.015,y)
 # A broad thoracic cage and sheet-like latissimus, not added isolated lobes.
 thorax=.020*g(z,1.365,.145)
 scapula=.008*g(a,.105,.065)*g(z,1.425,.064)
 lat=.011*g(a,.155,.060)*g(z,1.295,.103)
 spine=-.005*g(x,0,.033)*g(z,1.405,.12)
 dy=(thorax+scapula+lat+spine)*rear*core
 dy-=.011*g(z,1.372,.115)*g(a,.085,.14)*front*core
 # Continuous upper-back taper into the waist, with restrained lateral fullness.
 dx=sign*.010*g(z,1.29,.105)*step(.10,.175,a)*(1-step(.215,.27,a))
 dy+=.003*g(z,1.17,.075)*rear*core
 # Neck/trapezius transition; retain the previous reduced outer shoulder width.
 dz=.009*g(a,.122,.062)*g(z,1.49,.043)*(.65+.35*rear)
 # Broader posterior axillary fold eases the body-to-upper-arm transition.
 dy+=.006*g(a,.208,.045)*g(z,1.376,.075)*rear
 return Vector((dx,dy,dz))
ob=bpy.data.objects['Uniform_Ripstop']
assert not ob.get('back_thorax_revision_20261027'),'Already applied'
parts=components(ob);torso=next(ids for ids in parts if min(ob.data.vertices[i].co.z for i in ids)>.90 and max(ob.data.vertices[i].co.z for i in ids)>1.5)
bm=bmesh.new();bm.from_mesh(ob.data);bm.verts.ensure_lookup_table();tag=bm.verts.layers.float.new('thorax_edit_region')
for v in bm.verts:v[tag]=1 if v.index in set(torso) else 0
start=len(bm.verts)
for iteration in range(3):
 edges=[e for e in bm.edges if all(v[tag]>.5 for v in e.verts) and 1.105<(e.verts[0].co.z+e.verts[1].co.z)*.5<1.52 and abs((e.verts[0].co.x+e.verts[1].co.x)*.5)<.26 and e.calc_length()>.022]
 if not edges:break
 bmesh.ops.subdivide_edges(bm,edges=edges,cuts=1,use_grid_fill=True)
# Deform only the existing jacket surface. Cloth seam/pocket islands follow later.
for v in bm.verts:
 if v[tag]>.5:v.co+=torso_displacement(v.co)
bm.verts.layers.float.remove(tag)
bm.to_mesh(ob.data);bm.free();ob.data.update()
ob.data.normals_split_custom_set([(0,0,0)]*len(ob.data.loops))
# Interpolated deform weights are retained and normalized after subdivision.
for v in ob.data.vertices:
 total=sum(w.weight for w in v.groups)
 assert total>0,'Unweighted vertex after subdivision'
 if abs(total-1)>1e-6:
  for w in list(v.groups):ob.vertex_groups[w.group].add([v.index],w.weight/total,'REPLACE')
ob['back_thorax_revision_20261027']=True
ob['back_thorax_added_vertices']=len(ob.data.vertices)-start
print('Jacket refinement added vertices',len(ob.data.vertices)-start,'total',len(ob.data.vertices))
review('backthorax_body',True)
