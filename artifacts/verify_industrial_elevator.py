import bpy, os, json, math
root=bpy.data.objects.get('IndustrialElevator')
assert root is not None
objects=list(root.children_recursive)
meshes=[o for o in objects if o.type=='MESH']
assert len(meshes)>100
assert all(math.isfinite(c) for o in meshes for v in o.data.vertices for c in v.co)
assert not any(o.type in {'CURVE','FONT','CAMERA','LIGHT'} for o in bpy.context.scene.objects)
assert all(o.data.uv_layers for o in meshes)
assert all(bpy.data.objects.get(name) is not None for name in ['Door_Left','Door_Right'])
assert bpy.data.objects['Door_Left'].data != bpy.data.objects['Door_Right'].data
ref=bpy.data.images.get('REFERENCE | elevator six-view sheet')
assert ref and ref.packed_file and ref.use_fake_user
report={'status':'PASS','file':bpy.data.filepath,'bytes':os.path.getsize(bpy.data.filepath),'mesh_objects':len(meshes),'all_meshes_have_uvs':True,'separate_door_meshes':True,'reference_packed':True,'studio_objects_removed':True}
print(json.dumps(report),flush=True)
with open(r'C:\Users\PLUS\UNITY_PROJECT\INFO_2GRADE_BOYS_FINAL\artifacts\industrial_elevator_verification.json','w',encoding='utf-8') as f:json.dump(report,f,indent=2)
