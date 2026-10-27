import bpy,os,json
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL';OUT=os.path.join(ROOT,'Assets','Blender','DesertOperator');EX=os.path.join(ROOT,'Assets','Characters','DesertOperator','Models')
scene=bpy.context.scene;rig=scene.objects['DesertOperator_Rig'];col=bpy.data.collections['01_CHARACTER - rigged export']
materials={m for o in col.objects if o.type=='MESH' for m in o.data.materials};loaded={}
for m in materials:
 for node in m.node_tree.nodes:
  if node.type!='TEX_IMAGE' or not node.image:continue
  oldname=node.image.name.split('.')[0]
  suffix=next((x for x in ['BaseColor','Normal','Roughness'] if x in oldname),None)
  if suffix is None:continue
  name=m.name+'_'+suffix;path=os.path.join(OUT,'Textures',name+'.png');assert os.path.isfile(path),path
  if name not in loaded:
   im=bpy.data.images.load(path,check_existing=False);im.name=name+'_Packed';im.colorspace_settings.name='sRGB' if suffix=='BaseColor' else 'Non-Color';im.pack();loaded[name]=im
  node.image=loaded[name]
for index,name in [(0,'DesertOperator'),(1,'DesertOperator_LOD1'),(2,'DesertOperator_LOD2')]:
 target=col if index==0 else bpy.data.collections['LOD'+str(index)+' - export only']
 layer=scene.view_layers[0].layer_collection.children[target.name]
 old_exclude=layer.exclude;old_hide=target.hide_viewport;layer.exclude=False;target.hide_viewport=False
 bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
 for o in target.objects:o.select_set(True)
 bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=os.path.join(EX,name+'.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE',armature_nodetype='NULL')
 target.hide_viewport=old_hide;layer.exclude=old_exclude
qa=bpy.data.scenes.new('FBX file validation');bpy.context.window.scene=qa
bpy.ops.import_scene.fbx(filepath=os.path.join(EX,'DesertOperator.fbx'),use_anim=False)
objs=[o for o in qa.objects if o.type=='MESH'];arms=[o for o in qa.objects if o.type=='ARMATURE']
images={n.image for o in objs for m in o.data.materials if m and m.use_nodes for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image}
missing=[i.filepath for i in images if not os.path.isfile(bpy.path.abspath(i.filepath))]
result={'mesh_count':len(objs),'bones':len(arms[0].data.bones),'all_have_uv':all(bool(o.data.uv_layers) for o in objs),'all_skinned':all(any(m.type=='ARMATURE' and m.object for m in o.modifiers) for o in objs),'texture_files_exist':not missing,'missing_textures':missing}
assert result['bones']==49 and result['all_have_uv'] and result['all_skinned'] and not missing,result
with open(os.path.join(OUT,'FBXRoundtripReport.json'),'w') as f:json.dump(result,f,indent=2)
bpy.context.window.scene=scene
for o in list(qa.objects):bpy.data.objects.remove(o,do_unlink=True)
bpy.data.scenes.remove(qa)
for name,im in loaded.items():im.filepath='//Textures/'+name+'.png'
manifest=[]
for m in sorted(materials,key=lambda m:m.name):
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');maps={}
 for suffix in ['BaseColor','Normal','Roughness','MetallicSmoothness']:
  path='Textures/'+m.name+'_'+suffix+'.png'
  if os.path.isfile(os.path.join(OUT,path)):maps[suffix]=path
 manifest.append({'name':m.name,'baseColor':list(p.inputs['Base Color'].default_value),'roughness':p.inputs['Roughness'].default_value,'metallic':p.inputs['Metallic'].default_value,'maps':maps})
with open(os.path.join(OUT,'MaterialManifest.json'),'w') as f:json.dump(manifest,f,indent=2)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.data.libraries.write(os.path.join(OUT,'DesertOperator.blend'),{scene},fake_user=True)
print('VERIFIED',json.dumps(result),'Packed source maps',len(loaded))
