import bpy,bmesh,os,numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
exec(compile(open(os.path.join(ROOT,'artifacts/anatomy_review.py'),encoding='utf-8').read(),'review','exec'))
body=bpy.data.objects['Uniform_Ripstop'];o=bpy.data.objects['TorsoEquipment_Coyote_Cordura']
bt=BVHTree.FromPolygons([v.co for v in body.data.vertices],[list(p.vertices) for p in body.data.polygons])
with bpy.data.libraries.load(os.path.join(ART,'backups/DesertOperator_before_back_thorax_20261027.blend'),link=False) as (a,b):b.objects=['TorsoEquipment_Coyote_Cordura']
src=b.objects[0];sides={};coefs={};offsets={}
def basis(x,z):
 u=x/.18;v=(z-1.28)/.16
 return np.array([1,u*u,u**4,v,v*v,v**3,u*u*v,u*u*v*v])
for part in components(src):
 if len(part)!=208:continue
 side=1 if sum(src.data.vertices[i].co.y for i in part)>0 else -1;keep=set(part)
 for i in part:sides[i]=side
 tree=BVHTree.FromPolygons([v.co for v in src.data.vertices],[list(p.vertices) for p in src.data.polygons if all(i in keep for i in p.vertices)])
 rows=[];vals=[]
 for x in np.linspace(-.17,.17,19):
  for z in np.linspace(1.115,1.438,28):
   hit=tree.ray_cast(Vector((x,side*.5,z)),Vector((0,-side,0)))[0];skin=bt.ray_cast(Vector((x,side*.5,z)),Vector((0,-side,0)))[0]
   if hit is not None and skin is not None:rows.append(basis(x,z));vals.append(side*skin.y+.008)
 rows=np.array(rows);vals=np.array(vals);coefs[side]=np.linalg.lstsq(rows,vals,rcond=None)[0];offsets[side]=max(0,float(np.max(vals-rows@coefs[side])))+.001
bm=bmesh.new();bm.from_mesh(src.data);bm.verts.ensure_lookup_table();tag=bm.verts.layers.float.new('side');fraction=bm.verts.layers.float.new('thickness_fraction')
for v in bm.verts:
 side=sides.get(v.index,0);v[tag]=side
 if side:
  x,y,z=v.co;base=-.163 if side<0 else .133;curve=.65 if side<0 else -.5;thick=.042 if side<0 else .04
  v[fraction]=max(0,min(1,(y-base-curve*x*x)/thick))
 else:
  assert abs(v.co.x-o.data.vertices[v.index].co.x)<.00001
  v.co=o.data.vertices[v.index].co
for face in bm.faces:
 if face.calc_area()>.0003 and all(abs(v[tag])>.5 for v in face.verts) and abs(face.normal.y)>.8:
  # Keep main inner/outer cloth faces at an exact layer; bevel vertices alone interpolate thickness.
  value=1.0 if face.normal.y>0 else 0.0
  for v in face.verts:v[fraction]=value
bmesh.ops.triangulate(bm,faces=[f for f in bm.faces if all(abs(v[tag])>.5 for v in f.verts)])
for iteration in range(3):
 edges=[e for e in bm.edges if e.verts[0][tag]*e.verts[1][tag]>.5 and e.calc_length()>.025]
 if edges:bmesh.ops.subdivide_edges(bm,edges=edges,cuts=1,use_grid_fill=True)
for v in bm.verts:
 if not abs(v[tag])>.5:continue
 side=1 if v[tag]>0 else -1;inner=max(.055,float(basis(v.co.x,v.co.z)@coefs[side]+offsets[side]))
 v.co.y=-(inner+(1-v[fraction])*.042) if side<0 else inner+v[fraction]*.04
bm.verts.layers.float.remove(tag);bm.verts.layers.float.remove(fraction)
new=src.data.copy();bm.to_mesh(new);bm.free();o.data=new;o.data.materials[0]=bpy.data.materials['DO_Coyote_Cordura'];bpy.data.objects.remove(src,do_unlink=True)
for p in o.data.polygons:p.use_smooth=True
o.data.normals_split_custom_set([(0,0,0)]*len(o.data.loops));o.data.update()
print('Resolved carrier surfaces, explicit preserved thickness',len(o.data.vertices),'vertices')
RENDER=False
exec(compile(open(os.path.join(ROOT,'artifacts/polish_back_thorax.py'),encoding='utf-8').read(),'polish','exec'))
bpy.context.scene.render.engine='BLENDER_WORKBENCH'
review('backthorax_final_clay',False)
