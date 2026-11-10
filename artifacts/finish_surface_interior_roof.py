import bpy, bmesh, math, os, json
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL')
s=bpy.context.scene
assert s.name.startswith('Surface Interior')
backup=ROOT/'artifacts/backups/SurfaceResearchInterior_before_ceiling_20261110.blend'
if not backup.exists():bpy.ops.wm.save_as_mainfile(filepath=str(backup),copy=True)
assert not bpy.data.collections.get('SI_08_Ceilings'),'Ceilings already exist'
c=bpy.data.collections.new('SI_08_Ceilings');s.collection.children.link(c)
paint=bpy.data.materials['SI_Existing_Ivory_Wall'];steel=bpy.data.materials['SI_Existing_Graphite_Frame'];glass=bpy.data.materials['SI_Clear_Observation_Glass']
def mesh(name,vs,fs,mat):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 uv=me.uv_layers.new(name='SurfaceMeters')
 for f in me.polygons:
  ax=max(range(3),key=lambda k:abs(f.normal[k]));axes=[k for k in range(3) if k!=ax]
  for li in f.loop_indices:
   p=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(p[axes[0]]/2,p[axes[1]]/2)
 ob=bpy.data.objects.new(name,me);c.objects.link(ob);me.materials.append(mat);return ob
def prism(name,pts,z0,z1,mat):
 n=len(pts);return mesh(name,[(x,y,z) for z in (z0,z1) for x,y in pts],[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],mat)
def beam(name,a,b,width=.09):
 a,b=Vector(a),Vector(b);d=b-a
 o=prism(name,[(-width/2,-width/2),(width/2,-width/2),(width/2,width/2),(-width/2,width/2)],-d.length/2,d.length/2,steel)
 o.location=(a+b)/2;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();return o
floor=bpy.data.objects['GROUND / continuous walkable floor']
roof=floor.copy();roof.data=floor.data.copy();roof.name='CEILING / continuous lab and corridor roof';c.objects.link(roof)
roof.location.z+=4.48;roof.data.materials.clear();roof.data.materials.append(paint)
court=[(-12.0,-13.9),(12.0,-13.9),(12.0,4),(8.5,8.9),(-8.5,8.9),(-12.0,4)]
cutter=prism('TEMP atrium ceiling opening',court,3,6,paint)
bpy.context.view_layer.objects.active=roof
mod=roof.modifiers.new('Atrium opening','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
# Tall upper glazing encloses the cage without changing any room or corridor.
for i,a in enumerate(court):
 b=court[(i+1)%len(court)];av,bv=Vector(a),Vector(b);d=bv-av;L=d.length;n=Vector((-d.y,d.x)).normalized()*.025
 count=math.ceil(L/2.4)
 for j in range(count):
  p=av+d*j/count;q=av+d*(j+1)/count
  prism('CEILING / clerestory glass %02d-%02d'%(i,j),[tuple(p-n),tuple(q-n),tuple(q+n),tuple(p+n)],4.18,9.6,glass)
  beam('CEILING / clerestory mullion',(p.x,p.y,4.18),(p.x,p.y,9.65),.085)
 for z in (4.23,6.9,9.6):beam('CEILING / clerestory rail',(*a,z),(*b,z),.10)
# Six raised skylight facets meet a central ridge point; lowest roof is 0.79m above cage crown.
peak=Vector((0,-2.5,12.0))
for i,a in enumerate(court):
 b=court[(i+1)%len(court)];aa=Vector((*a,9.6));bb=Vector((*b,9.6))
 ob=mesh('CEILING / atrium skylight facet %02d'%i,[tuple(aa),tuple(bb),tuple(peak)],[(0,1,2)],glass)
 so=ob.modifiers.new('Laminated skylight thickness','SOLIDIFY');so.thickness=.045
 beam('CEILING / skylight rib',aa,peak,.13)
 for t in (.33,.66):beam('CEILING / skylight glazing bar',aa.lerp(peak,t),bb.lerp(peak,t),.075)
s.name='Surface Interior | Covered'
s['design_notes']='Covered walkable ground floor. 4.2m lab ceilings; tall glazed atrium 9.6m eaves/12m apex preserves 8.81m cage. Original room layout retained.'
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Assets/Blender/SurfaceResearchInterior/SurfaceResearchInterior.blend'))
# Export evaluated curves, labels and meshes grouped by functional collection/material.
out=ROOT/'Assets/Environment/SurfaceResearchInterior/Models';out.mkdir(parents=True,exist_ok=True)
dg=bpy.context.evaluated_depsgraph_get();temp=bpy.data.scenes.new('SI temporary export');groups={};manifest=[]
materials={m for o in s.objects if o.type in ('MESH','CURVE','FONT') for m in o.data.materials if m}
for m in materials:
 p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if m.use_nodes else None
 maps={}
 if p:
  for n in m.node_tree.nodes:
   if n.type=='TEX_IMAGE' and n.image:maps[n.image.name]=bpy.path.abspath(n.image.filepath)
 manifest.append({'name':m.name,'color':list(p.inputs['Base Color'].default_value) if p else list(m.diffuse_color),'metallic':p.inputs['Metallic'].default_value if p else 0,'roughness':p.inputs['Roughness'].default_value if p else .6,'maps':maps})
for o in list(s.objects):
 if o.type not in ('MESH','CURVE','FONT'):continue
 e=o.evaluated_get(dg);me=bpy.data.meshes.new_from_object(e,preserve_all_data_layers=True,depsgraph=dg)
 if not len(me.polygons):bpy.data.meshes.remove(me);continue
 group=o.users_collection[0].name
 key=group+'__'+(me.materials[0].name if me.materials else 'Unassigned')
 ob=bpy.data.objects.new(o.name,me);temp.collection.objects.link(ob);ob.matrix_world=o.matrix_world.copy();groups.setdefault(key,[]).append(ob)
win=bpy.context.window_manager.windows[0];win.scene=temp
for key,obs in groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for ob in obs:ob.select_set(True)
 bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();obs[0].name=key
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(out/'SurfaceResearchInterior.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='RELATIVE',use_mesh_modifiers=True)
report={'objects':len(temp.objects),'ceilings':len(c.objects),'roof_height':12,'groups':[o.name for o in temp.objects]}
(out.parent/'MaterialManifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(ROOT/'artifacts/surface_interior/export_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
win.scene=s
for ob in list(temp.objects):bpy.data.objects.remove(ob,do_unlink=True)
bpy.data.scenes.remove(temp)
print(json.dumps(report))
