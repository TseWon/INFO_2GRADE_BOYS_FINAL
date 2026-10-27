import bpy,math,json,os,shutil
from mathutils import Vector
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
OUT=os.path.join(ROOT,'Assets/Blender/DesertOperator');EXPORT=os.path.join(ROOT,'Assets/Characters/DesertOperator/Models')
os.makedirs(EXPORT,exist_ok=True)
scene=bpy.context.scene;rig=scene.objects['DesertOperator_Rig']
collection=bpy.data.collections.new('01_CHARACTER - rigged export');scene.collection.children.link(collection)
characters=list(scene.objects)
for o in characters:
 for c in list(o.users_collection):c.objects.unlink(o)
 collection.objects.link(o)
# Repetitive small objects are batched by functional zone and material. Main equipment
# stays identifiable while the game export avoids a renderer for every buckle and stitch.
zones={}
for o in characters:
 if o.type!='MESH':continue
 if o.name in ['Uniform_Ripstop','Gloves_and_Boot_Uppers','Outsoles_and_Glove_Reinforcement']:continue
 groupnames={g.name for g in o.vertex_groups}
 zone='HeadEquipment' if groupnames<={'Head','Neck'} else 'TorsoEquipment' if groupnames<={'Chest','Spine'} else 'BeltEquipment' if groupnames=={'Hips'} else 'LimbEquipment'
 key=(zone,o.data.materials[0].name);zones.setdefault(key,[]).append(o)
for (zone,material),group in zones.items():
 if len(group)<2:continue
 bpy.ops.object.select_all(action='DESELECT')
 for o in group:o.select_set(True)
 bpy.context.view_layer.objects.active=group[0];bpy.ops.object.join();bpy.context.object.name=zone+'_'+material.replace('DO_','')
# Validate mesh data and normalize the final exported deform weights.
report={'meshes':[],'bones':len(rig.data.bones),'height_m':0,'animations_included':False}
mins=Vector((1e9,1e9,1e9));maxs=-mins
for o in collection.objects:
 if o.type!='MESH':continue
 o.data.calc_loop_triangles();bad=0;unweighted=0
 for v in o.data.vertices:
  total=sum(g.weight for g in v.groups)
  if total<.0001:unweighted+=1
  elif abs(total-1)>.0001:
   for g in list(v.groups):o.vertex_groups[g.group].add([v.index],g.weight/total,'REPLACE')
  p=o.matrix_world@v.co
  for j in range(3):mins[j]=min(mins[j],p[j]);maxs[j]=max(maxs[j],p[j])
 report['meshes'].append({'name':o.name,'vertices':len(o.data.vertices),'triangles':len(o.data.loop_triangles),'uv':bool(o.data.uv_layers),'unweighted_vertices':unweighted})
 assert unweighted==0,o.name+' has unweighted vertices'
 assert o.data.uv_layers,o.name+' lacks UV'
report['height_m']=maxs.z-mins.z;report['triangles']=sum(m['triangles'] for m in report['meshes']);report['dimensions']=list(maxs-mins)
report['material_count']=len({m.name for o in collection.objects if o.type=='MESH' for m in o.data.materials})
report['source']='Four supplied images. Geometry is fully 3D; no image planes are included in the character export.'
manifest=[]
for m in sorted({m for o in collection.objects if o.type=='MESH' for m in o.data.materials},key=lambda m:m.name):
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 maps={}
 for n in m.node_tree.nodes:
  if n.type=='TEX_IMAGE' and n.image:maps[n.image.name.split('_')[-1]]='Textures/'+os.path.basename(n.image.filepath)
 if maps:maps['MetallicSmoothness']='Textures/'+m.name+'_MetallicSmoothness.png'
 manifest.append({'name':m.name,'baseColor':list(p.inputs['Base Color'].default_value),'roughness':p.inputs['Roughness'].default_value,'metallic':p.inputs['Metallic'].default_value,'maps':maps})
with open(os.path.join(OUT,'AssetReport.json'),'w') as f:json.dump(report,f,indent=2)
with open(os.path.join(OUT,'MaterialManifest.json'),'w') as f:json.dump(manifest,f,indent=2)
for b in rig.pose.bones:b.matrix_basis.identity()
bpy.ops.object.select_all(action='DESELECT')
for o in collection.objects:o.select_set(True)
bpy.context.view_layer.objects.active=rig
kwargs=dict(filepath=os.path.join(EXPORT,'DesertOperator.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE',use_mesh_modifiers=True,armature_nodetype='NULL')
bpy.ops.export_scene.fbx(**kwargs)
# A studio collection is for inspection only and is excluded from the FBX export.
studio=bpy.data.collections.new('02_PREVIEW - excluded from FBX');scene.collection.children.link(studio)
def link(o):studio.objects.link(o);return o
world=bpy.data.worlds.new('Neutral studio');world.use_nodes=True;scene.world=world
background=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');background.inputs['Color'].default_value=(.16,.18,.21,1);background.inputs['Strength'].default_value=.45
def area(name,loc,power,size):
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape=next(i.identifier for i in data.bl_rna.properties['shape'].enum_items if i.identifier=='DISK');data.size=size
 o=link(bpy.data.objects.new(name,data));o.location=loc;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
area('Large soft key',(3,-4,4),650,3.5);area('Soft fill',(-3,-2,2.7),350,3);area('Rim light',(1.7,3,3.6),800,2.5)
me=bpy.data.meshes.new('Studio floor');me.from_pydata([(-100,-100,0),(100,-100,0),(100,100,0),(-100,100,0)],[],[(0,1,2,3)]);me.update();ground=link(bpy.data.objects.new('Preview floor',me))
m=bpy.data.materials.new('Studio floor');m.use_nodes=True;p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(.075,.083,.092,1);p.inputs['Roughness'].default_value=.9;me.materials.append(m)
camera=bpy.data.cameras.new('Inspection camera');camera.type=next(i.identifier for i in camera.bl_rna.properties['type'].enum_items if i.identifier=='ORTHO');camera.ortho_scale=2.02
cam=link(bpy.data.objects.new('Inspection camera',camera));cam.location=(2,-5,1.12);cam.rotation_euler=(Vector((0,0,.91))-cam.location).to_track_quat('-Z','Y').to_euler();scene.camera=cam
scene.render.resolution_x=1000;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.render.image_settings.file_format=next(i.identifier for i in scene.render.image_settings.bl_rna.properties['file_format'].enum_items if i.identifier=='PNG')
scene.render.film_transparent=False
# References stay packed in an explicitly excluded, hidden guide collection.
refs=bpy.data.collections.new('03_REFERENCES - hidden guides');scene.collection.children.link(refs);refs.hide_render=True;refs.hide_viewport=True
paths=['0782b807-85d8-4b78-a1d8-44e3eb1b5b23','fb615db5-fce0-4083-8acc-9889cf6223bc','a0a610dc-5fc7-407e-a6b2-1bd423281d26','83d83e71-e14c-4eba-adee-b0ca947e982c']
for label,pth in zip(['Front','Back','Left','Right'],paths):
 p=os.path.join(os.environ['TEMP'],'codex-clipboard-'+pth+'.png')
 if os.path.exists(p):
  im=bpy.data.images.load(p);im.pack();o=bpy.data.objects.new('Reference_'+label,None);o.empty_display_type=next(i.identifier for i in o.bl_rna.properties['empty_display_type'].enum_items if i.identifier=='IMAGE');o.data=im;o.empty_display_size=1.82;refs.objects.link(o)
# Pack image maps and make the project self-contained.
for im in bpy.data.images:
 if im.name.startswith('DO_'):im.pack();im.filepath='//Textures/'+os.path.basename(im.filepath)
bpy.data.libraries.write(os.path.join(OUT,'DesertOperator.blend'),{scene},fake_user=True)
print(json.dumps(report))
scene.render.filepath=os.path.join(ROOT,'artifacts/desert_operator_beauty.png')
bpy.ops.render.render(write_still=True)
print('Beauty render and FBX saved')
