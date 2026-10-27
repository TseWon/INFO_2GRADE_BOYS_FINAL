import bpy,bmesh,os,json,math,numpy as np
from mathutils import Vector,Quaternion
from mathutils.kdtree import KDTree
from mathutils.bvhtree import BVHTree
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL';OUT=os.path.join(ROOT,'Assets/Blender/DesertOperator')
exec(compile(open(os.path.join(ROOT,'artifacts/anatomy_review.py'),encoding='utf-8').read(),'review','exec'))
before=np.load(os.path.join(ART,'back_thorax_before_meshes.npz'))
with open(os.path.join(ART,'back_thorax_before_structure.json')) as f:meta=json.load(f)
col=bpy.data.collections['01_CHARACTER - rigged export'];rig=bpy.data.objects['DesertOperator_Rig'];body=bpy.data.objects['Uniform_Ripstop'];carrier=bpy.data.objects['TorsoEquipment_Coyote_Cordura']
report={'revision':'back-thorax-20261027','backup':'artifacts/backups/DesertOperator_before_back_thorax_20261027.blend','objects':[],'remaining_limits':['Large shoulder rotations, deep squat, aiming and full animation clips not verified.','Existing finger skinning still needs grip-specific weight correction.']}
for o in col.objects:
 if o.type!='MESH':continue
 old=before[o.name];coords=np.array([v.co[:] for v in o.data.vertices]);bm=bmesh.new();bm.from_mesh(o.data);boundary=sum(e.is_boundary for e in bm.edges);nonmanifold=sum(not e.is_manifold for e in bm.edges);bm.free()
 weights=np.zeros((len(coords),len(o.vertex_groups)))
 for v in o.data.vertices:
  for g in v.groups:weights[v.index,g.group]=g.weight
 original_weights=before[o.name+'__weights'];maxerr=float(np.max(np.abs(weights[:len(old)]-original_weights)))
 mats=[m.name for m in o.data.materials];assert mats==meta['objects'][o.name]['materials'],(o.name,mats)
 assert np.isfinite(coords).all() and np.max(np.abs(weights.sum(axis=1)-1))<1e-5
 uv=[tuple(d.uv) for l in o.data.uv_layers for d in l.data];assert uv and np.isfinite(np.array(uv)).all()
 displacement=float(np.max(np.linalg.norm(coords[:len(old)]-old,axis=1)))
 report['objects'].append({'name':o.name,'vertices_before':len(old),'vertices_after':len(coords),'existing_vertex_weight_max_delta':maxerr,'max_existing_vertex_displacement_m':displacement,'boundary_edges':boundary,'nonmanifold_edges':nonmanifold,'uv_layers':[l.name for l in o.data.uv_layers],'materials':mats})
report['bones_preserved']=all((b.head_local-Vector(meta['bones'][b.name]['head'])).length<1e-7 and (b.tail_local-Vector(meta['bones'][b.name]['tail'])).length<1e-7 for b in rig.data.bones)
assert report['bones_preserved']
pants=next(ids for ids in components(body) if len(ids)>10000 and max(body.data.vertices[i].co.z for i in ids)<1.1)
tree=KDTree(len(before[body.name]))
for i,p in enumerate(before[body.name]):tree.insert(p,i)
tree.balance();report['pants_vertices']=len(pants);report['pants_max_position_change_m']=max(tree.find(body.data.vertices[i].co)[2] for i in pants);assert report['pants_max_position_change_m']<1e-6
boots=bpy.data.objects['Gloves_and_Boot_Uppers'];report['boots_unchanged']=all((v.co-Vector(before[boots.name][v.index])).length<1e-7 for v in boots.data.vertices if v.co.z<.5);assert report['boots_unchanged']
bt=BVHTree.FromPolygons([v.co for v in body.data.vertices],[list(p.vertices) for p in body.data.polygons]);report['plate_clearance_samples']=[]
for part in components(carrier):
 if len(part)<500:continue
 side=1 if sum(carrier.data.vertices[i].co.y for i in part)>0 else -1;keep=set(part)
 pt=BVHTree.FromPolygons([v.co for v in carrier.data.vertices],[list(p.vertices) for p in carrier.data.polygons if all(i in keep for i in p.vertices)])
 gaps=[];thickness=[]
 for x in np.linspace(-.15,.15,13):
  for z in np.linspace(1.13,1.43,19):
   skin=bt.ray_cast(Vector((x,side*.5,z)),Vector((0,-side,0)))[0]
   inner=pt.ray_cast(Vector((x,-side*.5,z)),Vector((0,side,0)))[0];outer=pt.ray_cast(Vector((x,side*.5,z)),Vector((0,-side,0)))[0]
   if skin is not None and inner is not None and outer is not None:gaps.append(side*(inner.y-skin.y));thickness.append(abs(outer.y-inner.y))
 entry={'side':'front' if side<0 else 'back','samples':len(gaps),'min_clearance_mm':min(gaps)*1000,'max_clearance_mm':max(gaps)*1000,'median_thickness_mm':float(np.median(thickness))*1000};report['plate_clearance_samples'].append(entry)
 assert min(gaps)>0,entry
report['thorax_ray_sections']=[]
for z in [1.20,1.30,1.40]:
 for x in [0,.10,.15]:
  front=bt.ray_cast(Vector((x,-.5,z)),Vector((0,1,0)))[0];back=bt.ray_cast(Vector((x,.5,z)),Vector((0,-1,0)))[0]
  report['thorax_ray_sections'].append({'x':x,'z':z,'depth_m':back.y-front.y})
for name,deg in [('UpperArm_L',-24),('Forearm_L',-50),('UpperArm_R',18),('Forearm_R',-15),('Thigh_L',28),('Shin_L',-48),('Index1_L',35),('Index2_L',45)]:
 bone=rig.pose.bones[name];bone.rotation_mode=next(i.identifier for i in bone.bl_rna.properties['rotation_mode'].enum_items if i.identifier=='QUATERNION');bone.rotation_quaternion=Quaternion((1,0,0),math.radians(deg))
bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();report['pose_checks']=[]
for o in col.objects:
 if o.type!='MESH':continue
 ev=o.evaluated_get(deps);me=ev.to_mesh();invalid=sum(not all(math.isfinite(c) for c in v.co) for v in me.vertices);assert invalid==0
 stretch=max(((me.vertices[e.vertices[0]].co-me.vertices[e.vertices[1]].co).length/max(1e-8,(o.data.vertices[e.vertices[0]].co-o.data.vertices[e.vertices[1]].co).length) for e in o.data.edges),default=1)
 report['pose_checks'].append({'object':o.name,'invalid_vertices':invalid,'max_edge_stretch':stretch});ev.to_mesh_clear()
s=bpy.context.scene;cam=s.camera;state=(cam.matrix_world.copy(),cam.data.ortho_scale);ground_state=s.objects['Preview floor'].hide_render;transparent=s.render.film_transparent;s.objects['Preview floor'].hide_render=True;s.render.film_transparent=True;s.render.engine='BLENDER_WORKBENCH';cam.location=(2,-5,1.1);cam.rotation_euler=(Vector((0,0,.90))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.02;s.render.resolution_x=700;s.render.resolution_y=900;s.render.filepath=os.path.join(ART,'backthorax_pose_checked_final.png');bpy.ops.render.render(write_still=True)
for b in rig.pose.bones:b.matrix_basis.identity()
cam.matrix_world=state[0];cam.data.ortho_scale=state[1];s.objects['Preview floor'].hide_render=ground_state;s.render.film_transparent=transparent;bpy.context.view_layer.update()
with open(os.path.join(OUT,'BackThoraxRevisionReport.json'),'w') as f:json.dump(report,f,indent=2)
print(json.dumps({'changed_objects':[e['name'] for e in report['objects'] if e['max_existing_vertex_displacement_m']>1e-6],'pants_unchanged':report['pants_max_position_change_m'],'bones_preserved':report['bones_preserved'],'plate_checks':report['plate_clearance_samples'],'body_boundary_edges':next(e['boundary_edges'] for e in report['objects'] if e['name']=='Uniform_Ripstop')},ensure_ascii=False))
