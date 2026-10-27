import bpy,json,os,math
from mathutils import Vector,Quaternion
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
OUT=os.path.join(ROOT,'Assets/Blender/DesertOperator');EXPORT=os.path.join(ROOT,'Assets/Characters/DesertOperator/Models')
scene=bpy.context.scene;rig=scene.objects['DesertOperator_Rig'];col=bpy.data.collections['01_CHARACTER - rigged export']
for im in bpy.data.images:
 if im.name.startswith('DO_'):im.filepath=os.path.join(OUT,'Textures',os.path.basename(im.filepath))
for previous in list(scene.collection.children):
 if previous.name.startswith(('LOD1 - export only','LOD2 - export only')):
  for ob in list(previous.objects):bpy.data.objects.remove(ob,do_unlink=True)
  bpy.data.collections.remove(previous)
meshes=[o for o in col.objects if o.type=='MESH'];report={'lods':[],'rig_bones':len(rig.data.bones),'animation_clips':0}
def select(objects):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=rig
def export(objects,name):
 select([rig]+objects)
 bpy.ops.export_scene.fbx(filepath=os.path.join(EXPORT,name+'.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE',use_mesh_modifiers=True,armature_nodetype='NULL')
 count=0
 for o in objects:o.data.calc_loop_triangles();count+=len(o.data.loop_triangles)
 report['lods'].append({'file':name+'.fbx','triangles':count,'renderers':len(objects)})
for b in rig.pose.bones:b.matrix_basis.identity()
export(meshes,'DesertOperator')
for level,ratio in [(1,.42),(2,.16)]:
 lodcol=bpy.data.collections.new('LOD'+str(level)+' - export only');scene.collection.children.link(lodcol);lods=[]
 for original in meshes:
  ob=original.copy();ob.data=original.data.copy();ob.name='LOD'+str(level)+'_'+original.name;lodcol.objects.link(ob);lods.append(ob)
  if len(ob.data.polygons)>24:
   select([ob]);bpy.context.view_layer.objects.active=ob;d=ob.modifiers.new('Distance simplification','DECIMATE');d.ratio=ratio;bpy.ops.object.modifier_apply(modifier=d.name)
 export(lods,'DesertOperator_LOD'+str(level));lodcol.hide_render=True;lodcol.hide_viewport=True
 scene.view_layers[0].layer_collection.children[lodcol.name].exclude=True
# Rest and bent-pose diagnostics, with actual evaluated meshes (not only bone counts).
for name,deg in [('UpperArm_L',-24),('Forearm_L',-50),('UpperArm_R',18),('Forearm_R',-15),('Thigh_L',22),('Shin_L',-38),('Index1_L',35),('Index2_L',45),('Middle1_L',35),('Middle2_L',45)]:
 b=rig.pose.bones[name];b.rotation_mode=next(i.identifier for i in b.bl_rna.properties['rotation_mode'].enum_items if i.identifier=='QUATERNION');b.rotation_quaternion=Quaternion((1,0,0),math.radians(deg))
bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();checks=[]
for ob in meshes:
 ev=ob.evaluated_get(deps);me=ev.to_mesh();invalid=sum(not all(math.isfinite(c) for c in v.co) for v in me.vertices)
 checks.append({'name':ob.name,'evaluated_vertices':len(me.vertices),'invalid_vertices':invalid});assert invalid==0
 ev.to_mesh_clear()
report['deformation_checks']=checks
cam=scene.camera;cam.data.ortho_scale=2.02;cam.location=(2,-5,1.12);cam.rotation_euler=(Vector((0,0,.91))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=800;scene.render.resolution_y=1000;scene.render.filepath=os.path.join(ROOT,'artifacts/desert_operator_pose_check.png');bpy.ops.render.render(write_still=True)
for b in rig.pose.bones:b.matrix_basis.identity()
bpy.context.view_layer.update()
scene.render.resolution_x=1000;scene.render.resolution_y=1200;scene.render.filepath=os.path.join(ROOT,'artifacts/desert_operator_beauty.png');bpy.ops.render.render(write_still=True)
for label,pos in [('front',(0,-6,.94)),('back',(0,6,.94)),('left',(-6,0,.94)),('right',(6,0,.94))]:
 cam.location=pos;cam.rotation_euler=(Vector((0,0,.91))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.resolution_x=650;scene.render.resolution_y=900;scene.render.filepath=os.path.join(ROOT,'artifacts/desert_operator_'+label+'.png');bpy.ops.render.render(write_still=True)
cam.location=(.65,-2,1.60);cam.rotation_euler=(Vector((0,-.025,1.48))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=.91;scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.filepath=os.path.join(ROOT,'artifacts/desert_operator_detail.png');bpy.ops.render.render(write_still=True)
cam.location=(2,-5,1.12);cam.rotation_euler=(Vector((0,0,.91))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.02;scene.render.resolution_x=1000;scene.render.resolution_y=1200
# Keep original relative texture paths for a portable .blend after exporting absolute sources.
for im in bpy.data.images:
 if im.name.startswith('DO_'):im.pack();im.filepath='//Textures/'+os.path.basename(im.filepath)
select([rig]);rig.data.name='DesertOperator_Humanoid_49'
with open(os.path.join(OUT,'ValidationReport.json'),'w') as f:json.dump(report,f,indent=2)
with open(os.path.join(OUT,'AssetReport.json')) as f:asset=json.load(f)
asset['triangles']=report['lods'][0]['triangles'];asset['meshes']=[]
for ob in meshes:
 ob.data.calc_loop_triangles();asset['meshes'].append({'name':ob.name,'vertices':len(ob.data.vertices),'triangles':len(ob.data.loop_triangles),'uv':bool(ob.data.uv_layers),'unweighted_vertices':sum(not v.groups for v in ob.data.vertices)})
with open(os.path.join(OUT,'AssetReport.json'),'w') as f:json.dump(asset,f,indent=2)
with open(os.path.join(OUT,'MaterialManifest.json')) as f:manifest=json.load(f)
for entry in manifest:
 mat=bpy.data.materials[entry['name']];p=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');entry['baseColor']=list(p.inputs['Base Color'].default_value)
with open(os.path.join(OUT,'MaterialManifest.json'),'w') as f:json.dump(manifest,f,indent=2)
bpy.data.libraries.write(os.path.join(OUT,'DesertOperator.blend'),{scene},fake_user=True)
print(json.dumps({'lods':report['lods'],'bones':report['rig_bones'],'deformation_checks':'passed finite evaluated vertices','renders':7}))
