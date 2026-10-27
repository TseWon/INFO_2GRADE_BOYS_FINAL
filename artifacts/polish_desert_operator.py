import bpy,math,numpy as np
from mathutils import Vector
scene=bpy.context.scene;rig=scene.objects['DesertOperator_Rig']
body=scene.objects['Uniform_Ripstop']
for uv in body.data.uv_layers.active.data:uv.uv*=.60
# Sculpt restrained diagonal compression folds into the actual garment surface.
for v in body.data.vertices:
 p=body.matrix_world@v.co;x,y,z=p;s=1 if x>=0 else -1;ax=abs(x)
 if z>.98 and ax>.22:
  cx=.215+max(0,1.43-z)*.30;cy=-.014*max(0,(1.17-z)/.215)
  phi=math.atan2(y-cy,ax-cx);delta=0
  for j,(zc,amp) in enumerate([(1.38,.002),(1.30,.003),(1.235,.004),(1.175,.004),(1.11,.004),(1.04,.003)]):
   d=z-(zc+.024*math.sin(phi+(j%3)*1.4));delta+=amp*(.65*math.exp(-((d-.012)/.012)**2)-math.exp(-(d/.0075)**2))
 elif .28<z<.91 and ax>.035:
  cx=.115;cy=.015;phi=math.atan2(y-cy,ax-cx);delta=0
  for j,(zc,amp) in enumerate([(.85,.003),(.775,.004),(.70,.003),(.625,.0045),(.565,.004),(.48,.004),(.39,.0035),(.31,.004)]):
   d=z-(zc+.025*math.sin(phi+j*.85));delta+=amp*(.7*math.exp(-((d-.014)/.014)**2)-math.exp(-(d/.009)**2))
 else:continue
 radial=Vector((s*(ax-cx),y-cy,0))
 if radial.length>.02:p+=radial.normalized()*delta;v.co=body.matrix_world.inverted()@p
# Warm sandy fabric albedo while keeping the high frequency normal and roughness maps.
im=bpy.data.images.get('DO_Desert_Ripstop_BaseColor')
if im:
 a=np.empty(len(im.pixels),dtype=np.float32);im.pixels.foreach_get(a);a=a.reshape(-1,4);a[:,:3]*=np.array([.96,.91,.80]);im.pixels.foreach_set(a.ravel());im.save();im.pack()
# Lower the entire fitted head assembly; shorten the neck without changing shoulder anatomy.
for o in scene.objects:
 if o.type!='MESH':continue
 hg=o.vertex_groups.get('Head');ng=o.vertex_groups.get('Neck')
 for v in o.data.vertices:
  weights={g.group:g.weight for g in v.groups};p=o.matrix_world@v.co
  if hg and weights.get(hg.index,0)>.5:p.z-=.032
  elif ng and weights.get(ng.index,0)>.5:p.z-=.032*max(0,min(1,(p.z-1.48)/.10))
  else:continue
  v.co=o.matrix_world.inverted()@p
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
rig.data.edit_bones['Head'].head.z-=.032;rig.data.edit_bones['Head'].tail.z-=.032;rig.data.edit_bones['Neck'].tail.z-=.032
bpy.ops.object.mode_set(mode='OBJECT')
# A folded stand collar covers the back of the neck as in the reference, with a small front split.
verts=[];faces=[];N=56
for j in range(5):
 t=j/4
 for i in range(N):
  a=2*math.pi*i/N;x=(.087-.007*t)*math.cos(a);y=(.084-.009*t)*math.sin(a)+.005
  z=1.485+t*(.063-.017*max(0,-math.sin(a))**10);verts.append((x,y,z))
for j in range(4):
 for i in range(N):
  a=j*N+i;b=j*N+(i+1)%N;faces.append((a,b,b+N,a+N))
me=bpy.data.meshes.new('Folded collar mesh');me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new('Uniform_stand_collar',me);scene.collection.objects.link(o);me.materials.append(bpy.data.materials['DO_Desert_Ripstop'])
bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
so=o.modifiers.new('Collar cloth thickness','SOLIDIFY');so.thickness=.003;bpy.ops.object.modifier_apply(modifier=so.name)
for p in me.polygons:p.use_smooth=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project();bpy.ops.object.mode_set(mode='OBJECT')
for uv in o.data.uv_layers.active.data:uv.uv*=.3
g=o.vertex_groups.new(name='Chest');g.add(list(range(len(o.data.vertices))),1,'REPLACE');o.parent=rig;mod=o.modifiers.new('Skeleton','ARMATURE');mod.object=rig
# Replace the concave lens fan with valid boundary triangulation.
o=scene.objects.get('Goggle_smoked_lens')
if o:
 import bmesh
 bm=bmesh.new();bm.from_mesh(o.data);bm.verts.ensure_lookup_table();center=bm.verts[0]
 for f in list(bm.faces):bm.faces.remove(f)
 boundary=[v for v in bm.verts if v!=center];bm.verts.remove(center)
 bm.faces.new(boundary);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
print('Reference proportions refined: fitted head, collar, cloth compression folds, warm camo, valid lens triangles')
