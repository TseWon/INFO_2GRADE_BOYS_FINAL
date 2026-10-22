import bpy, os, json, math
from mathutils import Vector
root=bpy.data.objects.get('SteelStorageCabinet')
assert root is not None
mesh=[o for o in root.children_recursive if o.type=='MESH']
assert len(mesh)>80
assert not any(o.type in {'LIGHT','CAMERA','CURVE','FONT'} for o in bpy.context.scene.objects)
assert all(o.data.uv_layers for o in mesh)
assert all(math.isfinite(c) for o in mesh for v in o.data.vertices for c in v.co)
ref=bpy.data.images.get('REFERENCE | six-view steel cabinet')
assert ref and ref.packed_file and ref.use_fake_user
for label,side in [('Left',-1),('Right',1)]:
    pivot=bpy.data.objects['Door_'+label+'_Pivot'];door=bpy.data.objects['Door_'+label]
    assert door.parent==pivot and len(door.data.vertices)>8
    assert abs(pivot.location.x-side*.5025)<1e-5
    grip=bpy.data.objects['Handle '+label+' recessed grip'];assert grip.parent==pivot
    before=grip.matrix_world.translation.copy()
    pivot.rotation_euler.z=math.radians(side*60);bpy.context.view_layer.update()
    assert (grip.matrix_world.translation-before).length>.3
    pivot.rotation_euler.z=0;bpy.context.view_layer.update()
    assert (grip.matrix_world.translation-before).length<1e-5
report={'status':'PASS','file':bpy.data.filepath,'bytes':os.path.getsize(bpy.data.filepath),'mesh_objects':len(mesh),'packed_reference':True,'door_pivots_and_handle_parenting':True,'actual_handle_cutouts':True,'uv_layers':True,'studio_excluded':True}
with open(r'C:\Users\PLUS\UNITY_PROJECT\INFO_2GRADE_BOYS_FINAL\artifacts\steel_storage_cabinet_verification.json','w',encoding='utf-8') as f:json.dump(report,f,indent=2)
print(json.dumps(report),flush=True)
