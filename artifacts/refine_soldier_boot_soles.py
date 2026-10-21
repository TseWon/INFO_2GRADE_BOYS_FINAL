import bpy,bmesh,math
from mathutils import Vector
scene=bpy.context.scene;rig=next(o for o in scene.objects if o.type=='ARMATURE')
rubber=bpy.data.materials['Soldier_Rubber'];ob=scene.objects['Mesh_Soldier_Rubber']
bm=bmesh.new();bm.from_mesh(ob.data);deform=bm.verts.layers.deform.active
indices={ob.vertex_groups[n].index for n in ['Foot_L','Foot_R']}
# bmesh uses its own explicit context names, independent of RNA operator enums.
dead=[v for v in bm.verts if any(v[deform].get(i,0)>.5 for i in indices)]
for v in dead:bm.verts.remove(v)
bm.to_mesh(ob.data);bm.free();ob.data.update()
new=[]
outline=[(0,-.202),(.037,-.194),(.059,-.166),(.064,-.105),(.059,-.036),(.052,.030),(.052,.060),(.035,.079),(0,.084),(-.035,.079),(-.052,.060),(-.052,.030),(-.059,-.036),(-.064,-.105),(-.059,-.166),(-.037,-.194)]
curve=[]
for i in range(len(outline)):
 p0=Vector(outline[(i-1)%len(outline)]);p1=Vector(outline[i]);p2=Vector(outline[(i+1)%len(outline)]);p3=Vector(outline[(i+2)%len(outline)])
 for j in range(5):
  t=j/5;curve.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
for sign in [-1,1]:
 side='L' if sign<0 else 'R';verts=[];faces=[];N=len(curve)
 for z,scale in [(.006,.95),(.012,1),(.042,1),(.051,.96)]:
  for p in curve:verts.append((sign*.12+p.x*scale,-.058+(p.y+.058)*scale,z))
 for j in range(3):
  for i in range(N):a=j*N+i;b=j*N+(i+1)%N;faces.append((a,b,b+N,a+N))
 faces.extend([tuple(reversed(range(N))),tuple(3*N+i for i in range(N))])
 me=bpy.data.meshes.new('Contoured combat boot outsole');me.from_pydata(verts,[],faces);me.update()
 sole=bpy.data.objects.new('Contoured outsole '+side,me);scene.collection.objects.link(sole);me.materials.append(rubber)
 for p in me.polygons:p.use_smooth=len(p.vertices)==4
 vg=sole.vertex_groups.new(name='Foot_'+side);vg.add(list(range(len(me.vertices))),1,'REPLACE');sole.parent=rig
 bpy.ops.object.select_all(action='DESELECT');sole.select_set(True);bpy.context.view_layer.objects.active=sole
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project();bpy.ops.object.mode_set(mode='OBJECT');new.append(sole)
bpy.ops.object.select_all(action='DESELECT');ob.select_set(True)
for n in new:n.select_set(True)
bpy.context.view_layer.objects.active=ob;bpy.ops.object.join()
scene['boot_revision']='Anatomically shaped toe and heel outline replacing rectangular sole blocks'
bpy.data.libraries.write(r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL\Assets\Blender\TacticalSoldier\tactical_soldier.blend',{scene},fake_user=True)
print('Rounded boot sole silhouette saved')
