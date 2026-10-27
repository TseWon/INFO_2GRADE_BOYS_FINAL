import bpy,os,json,math,numpy as np,bmesh
from mathutils import Vector,Quaternion
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
OUT=os.path.join(ROOT,'Assets/Blender/DesertOperator');BACK=os.path.join(ROOT,'artifacts/backups/DesertOperator_before_body_proportions_20261027.blend')
col=bpy.data.collections['01_CHARACTER - rigged export'];rig=bpy.data.objects['DesertOperator_Rig'];meshes=[o for o in col.objects if o.type=='MESH']
names=[o.name for o in meshes]+[rig.name]
with bpy.data.libraries.load(BACK,link=False) as (a,b):b.objects=list(names)
original=dict(zip(names,b.objects));report={'backup':BACK,'checks':[],'bone_rest_preserved':True}
for o in meshes:
 old=original[o.name]
 weights=lambda obj:[tuple((obj.vertex_groups[g.group].name,round(g.weight,7)) for g in v.groups) for v in obj.data.vertices]
 uv=lambda obj:[[round(c,7) for d in layer.data for c in d.uv] for layer in obj.data.uv_layers]
 bm=bmesh.new();bm.from_mesh(o.data);bad=sum(not e.is_manifold for e in bm.edges);bm.free()
 bmo=bmesh.new();bmo.from_mesh(old.data);bad_before=sum(not e.is_manifold for e in bmo.edges);bmo.free()
 displacement=max((v.co-w.co).length for v,w in zip(o.data.vertices,old.data.vertices))
 entry={'object':o.name,'vertex_count':len(o.data.vertices),'vertex_count_unchanged':len(o.data.vertices)==len(old.data.vertices),'weights_identical':weights(o)==weights(old),'uv_identical':uv(o)==uv(old),'max_vertex_displacement_m':displacement,'nonmanifold_edges_before':bad_before,'nonmanifold_edges_after':bad}
 assert entry['vertex_count_unchanged'] and entry['uv_identical'],entry
 if o.name=='Uniform_Ripstop':
  entry['pelvis_weights_intentionally_reblended']=int(o.get('pelvis_skin_blend_corrected',0))
  entry['shoulder_weights_intentionally_reblended']=int(o.get('shoulder_skin_blend_corrected',0))
  entry['weight_sum_max_error']=max(abs(sum(g.weight for g in v.groups)-1) for v in o.data.vertices)
  assert entry['weight_sum_max_error']<1e-5
 else:assert entry['weights_identical'],entry
 assert bad==bad_before,entry
 report['checks'].append(entry)
for bone in rig.data.bones:
 old=original[rig.name].data.bones[bone.name]
 if (bone.head_local-old.head_local).length>1e-7 or (bone.tail_local-old.tail_local).length>1e-7:report['bone_rest_preserved']=False
assert report['bone_rest_preserved']
body=bpy.data.objects['Uniform_Ripstop'];report['body_sections']=[]
for label,z in [('waist',1.06),('pelvis',.94),('seat',.88),('upper_thigh',.79),('knee',.53),('calf',.40)]:
 result={'section':label,'height_m':z}
 for key,o in [('before',original[body.name]),('after',body)]:
  pts=[v.co for v in o.data.vertices if abs(v.co.z-z)<.012 and abs(v.co.x)<.25]
  result[key]={'width_m':max(p.x for p in pts)-min(p.x for p in pts),'front_y':min(p.y for p in pts),'back_y':max(p.y for p in pts),'depth_m':max(p.y for p in pts)-min(p.y for p in pts)}
 report['body_sections'].append(result)
for o in original.values():
 data=o.data;bpy.data.objects.remove(o,do_unlink=True)
 if data.users==0:
  if isinstance(data,bpy.types.Mesh):bpy.data.meshes.remove(data)
  elif isinstance(data,bpy.types.Armature):bpy.data.armatures.remove(data)
for name,deg in [('UpperArm_L',-24),('Forearm_L',-50),('UpperArm_R',18),('Forearm_R',-15),('Thigh_L',28),('Shin_L',-48),('Index1_L',35),('Index2_L',45)]:
 bone=rig.pose.bones[name];bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion((1,0,0),math.radians(deg))
bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();report['pose_check']=[]
for o in meshes:
 ev=o.evaluated_get(deps);me=ev.to_mesh();invalid=sum(not all(math.isfinite(c) for c in v.co) for v in me.vertices)
 maxedge=max(((me.vertices[e.vertices[0]].co-me.vertices[e.vertices[1]].co).length/max(1e-8,(o.data.vertices[e.vertices[0]].co-o.data.vertices[e.vertices[1]].co).length) for e in o.data.edges),default=0)
 report['pose_check'].append({'object':o.name,'invalid_vertices':invalid,'max_edge_stretch_ratio':maxedge});assert invalid==0
 ev.to_mesh_clear()
s=bpy.context.scene;cam=s.camera;s.render.engine='BLENDER_WORKBENCH';s.render.resolution_x=700;s.render.resolution_y=900;cam.location=(2,-5,1.1);cam.rotation_euler=(Vector((0,0,.90))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.02;s.render.film_transparent=True;s.render.filepath=os.path.join(ROOT,'artifacts/anatomy_pose_validation.png');bpy.ops.render.render(write_still=True)
for b in rig.pose.bones:b.matrix_basis.identity()
bpy.context.view_layer.update()
with open(os.path.join(OUT,'AnatomyRevisionReport.json'),'w',encoding='utf-8') as f:json.dump(report,f,indent=2,ensure_ascii=False)
print(json.dumps({'meshes_verified':len(meshes),'uv_unchanged':True,'weights':'preserved except deliberate pelvis blend correction','rest_bones_unchanged':True,'max_body_edge_stretch':next(c['max_edge_stretch_ratio'] for c in report['pose_check'] if c['object']=='Uniform_Ripstop')},ensure_ascii=False))
