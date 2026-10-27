import bpy,os,json,contextlib,io
from mathutils import Vector
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL';OUT=os.path.join(ROOT,'Assets/Blender/DesertOperator');EX=os.path.join(ROOT,'Assets/Characters/DesertOperator/Models')
s=bpy.context.scene;rig=bpy.data.objects['DesertOperator_Rig'];col=bpy.data.collections['01_CHARACTER - rigged export'];meshes=[o for o in col.objects if o.type=='MESH']
for b in rig.pose.bones:b.matrix_basis.identity()
materials={m for o in meshes for m in o.data.materials};images={}
for m in materials:
 for n in m.node_tree.nodes:
  if n.type!='TEX_IMAGE' or not n.image:continue
  suffix=next((x for x in ['BaseColor','Normal','Roughness'] if x in n.image.name),None)
  if suffix:
   filename=m.name+'_'+suffix+'.png';path=os.path.join(OUT,'Textures',filename);assert os.path.isfile(path)
   n.image.filepath=path;images[n.image]=filename
for name in ['LOD1 - export only','LOD2 - export only']:
 previous=bpy.data.collections.get(name)
 if previous:
  for o in list(previous.objects):
   data=o.data;bpy.data.objects.remove(o,do_unlink=True)
   if data.users==0:bpy.data.meshes.remove(data)
  bpy.data.collections.remove(previous)
def select(objects):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=rig
lods=[]
def export(objects,name):
 select([rig]+objects)
 with contextlib.redirect_stdout(io.StringIO()):
  bpy.ops.export_scene.fbx(filepath=os.path.join(EX,name+'.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE',use_mesh_modifiers=True,armature_nodetype='NULL')
 count=0
 for o in objects:o.data.calc_loop_triangles();count+=len(o.data.loop_triangles)
 lods.append({'file':name+'.fbx','triangles':count,'renderers':len(objects)})
export(meshes,'DesertOperator')
for level,ratio in [(1,.42),(2,.16)]:
 collection=bpy.data.collections.new('LOD'+str(level)+' - export only');s.collection.children.link(collection);copies=[]
 for source in meshes:
  o=source.copy();o.data=source.data.copy();o.name='LOD'+str(level)+'_'+source.name;collection.objects.link(o);copies.append(o)
  if len(o.data.polygons)>24:
   select([o]);bpy.context.view_layer.objects.active=o;mod=o.modifiers.new('Distance simplification','DECIMATE');mod.ratio=ratio;o.modifiers.move(len(o.modifiers)-1,0);bpy.ops.object.modifier_apply(modifier=mod.name)
 export(copies,'DesertOperator_LOD'+str(level));collection.hide_render=True;collection.hide_viewport=True;s.view_layers[0].layer_collection.children[collection.name].exclude=True
qa=bpy.data.scenes.new('Anatomy FBX verification');bpy.context.window.scene=qa
with contextlib.redirect_stdout(io.StringIO()):bpy.ops.import_scene.fbx(filepath=os.path.join(EX,'DesertOperator.fbx'),use_anim=False)
qmeshes=[o for o in qa.objects if o.type=='MESH'];arms=[o for o in qa.objects if o.type=='ARMATURE']
qimages={n.image for o in qmeshes for m in o.data.materials if m and m.use_nodes for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image}
result={'mesh_count':len(qmeshes),'bones':len(arms[0].data.bones),'all_have_uv':all(bool(o.data.uv_layers) for o in qmeshes),'all_skinned':all(any(m.type=='ARMATURE' and m.object for m in o.modifiers) for o in qmeshes),'texture_files_exist':all(os.path.isfile(bpy.path.abspath(i.filepath)) for i in qimages),'lods':lods}
assert result['mesh_count']==28 and result['bones']==49 and result['all_have_uv'] and result['all_skinned'] and result['texture_files_exist'],result
bpy.context.window.scene=s
for o in list(qa.objects):bpy.data.objects.remove(o,do_unlink=True)
bpy.data.scenes.remove(qa)
for im,filename in images.items():im.filepath='//Textures/'+filename
select([rig]);s.render.engine='CYCLES';s.cycles.samples=32;s.cycles.use_denoising=True
cam=s.camera;cam.location=(2,-5,1.12);cam.rotation_euler=(Vector((0,0,.91))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.02
s.render.resolution_x=1000;s.render.resolution_y=1200;s.render.film_transparent=False;s.objects['Preview floor'].hide_render=False
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_distance=2.8;area.spaces.active.region_3d.view_location=Vector((0,0,.9));area.spaces.active.shading.color_type='MATERIAL'
with open(os.path.join(OUT,'FBXRoundtripReport.json'),'w') as f:json.dump(result,f,indent=2)
with open(os.path.join(OUT,'BackThoraxRevisionReport.json'),encoding='utf-8') as f:report=json.load(f)
report['export']=result
with open(os.path.join(OUT,'BackThoraxRevisionReport.json'),'w',encoding='utf-8') as f:json.dump(report,f,indent=2,ensure_ascii=False)
with open(os.path.join(OUT,'ValidationReport.json')) as f:v=json.load(f)
v['lods']=lods;v['back_thorax_revision']='2026-10-27; see BackThoraxRevisionReport.json'
with open(os.path.join(OUT,'ValidationReport.json'),'w') as f:json.dump(v,f,indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'DesertOperator.blend'))
print('Saved Blender and refreshed 3 FBX files',json.dumps(result))

