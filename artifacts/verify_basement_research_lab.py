import bpy, math, os, json
root=bpy.data.objects.get('B1_Basement_Laboratory')
assert root is not None
objs=list(root.children_recursive);mesh=[o for o in objs if o.type=='MESH']
assert len(mesh)>500
assert all(math.isfinite(c) for o in mesh for v in o.data.vertices for c in v.co)
assert all(o.data.uv_layers for o in mesh)
assert not any(o.type in {'CURVE','FONT','CAMERA'} for o in bpy.context.scene.objects)
assert all('TEMP' not in o.name for o in bpy.context.scene.objects)
for collection in ['01_Structure','03_Catwalks_Stairs','04_Railings','05_Pipes_Vents','07_Lab_Equipment','08_Furniture_Storage']:
    assert bpy.data.collections.get(collection) and len(bpy.data.collections[collection].objects)>0
assert bpy.data.objects.get('Experiment main pedestal')
assert bpy.data.objects.get('Left access stair steel tread')
assert bpy.data.objects.get('Right access stair steel tread')
assert bpy.data.objects.get('Observation blue glazing')
im=bpy.data.images.get('REFERENCE | abandoned basement laboratory')
assert im and im.packed_file and im.use_fake_user
report={'status':'PASS','file':bpy.data.filepath,'bytes':os.path.getsize(bpy.data.filepath),'mesh_objects':len(mesh),'mesh_geometry_finite':True,'uv_layers_present':True,'reference_packed':True,'both_stairs_present':True,'observation_window_present':True,'no_preview_rig_in_asset':True,'installed_light_count':sum(o.type=='LIGHT' for o in objs)}
with open(r'C:\Users\PLUS\UNITY_PROJECT\INFO_2GRADE_BOYS_FINAL\artifacts\basement_lab_verification.json','w',encoding='utf-8') as f:json.dump(report,f,indent=2)
print(json.dumps(report),flush=True)
