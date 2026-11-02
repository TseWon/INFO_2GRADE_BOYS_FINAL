import bpy, math, os, json
from mathutils import Vector, Matrix
ROOT = r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
OUT = os.path.join(ROOT,'Assets/Characters/FittedSoldier')
os.makedirs(OUT+'/Textures',exist_ok=True)
source=bpy.data.collections['UNSAVED_Tactical_Soldier']
body=bpy.data.objects['GEO-body_male_realistic']; cx=body.matrix_world.translation.x
# Run in a separate background process from the verified recovery file.
# Avoid Blender 5.2's partial Scene-library writer (crashes in scene_copy_data).
keep=set(source.objects)|{body}
for obj in keep:
    world=obj.matrix_world.copy();obj.parent=None;obj.matrix_world=world
for obj in list(bpy.data.objects):
    if obj not in keep:bpy.data.objects.remove(obj,do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'artifacts/FittedSoldier_Source.blend'),compress=True)
coll=bpy.data.collections.new('Unity_Export_FittedSoldier');bpy.context.scene.collection.children.link(coll)
copies=[]; material_copies={}; manifest=[]
mult=body.modifiers.get('Multires'); original_level=mult.levels; mult.levels=1
bpy.context.view_layer.update()
deps=bpy.context.evaluated_depsgraph_get()
for src in list(source.objects)+[body]:
    if src.type not in {'MESH','CURVE'}:continue
    ev=src.evaluated_get(deps)
    me=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=deps)
    me.transform(src.matrix_world)
    ob=bpy.data.objects.new('Export_'+src.name.replace('UNSAVED_',''),me);coll.objects.link(ob)
    for i,mat in enumerate(me.materials):
        if mat.name not in material_copies:
            cp=mat.copy();cp.name=mat.name.replace('UNSAVED_','FS_');material_copies[mat.name]=cp
        me.materials[i]=material_copies[mat.name]
    copies.append(ob)
    print('Copied',src.name,flush=True)
mult.levels=original_level
# Join by material: eight draws rather than hundreds of small hardware objects.
groups=[]
for mat in material_copies.values():
    subset=[o for o in copies if mat in list(o.data.materials)]
    copies=[o for o in copies if o not in subset]
    bpy.ops.object.select_all(action='DESELECT')
    for o in subset:o.select_set(True)
    bpy.context.view_layer.objects.active=subset[0];bpy.ops.object.join()
    ob=bpy.context.object;ob.name=mat.name+'_Mesh';groups.append(ob)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.006)
    bpy.ops.object.mode_set(mode='OBJECT')
    print('UV READY',ob.name,len(ob.data.vertices),flush=True)
    bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    manifest.append({'name':mat.name,'color':list(bs.inputs['Base Color'].default_value),'roughness':bs.inputs['Roughness'].default_value,'metallic':bs.inputs['Metallic'].default_value})
state={'groups':groups,'coll':coll,'cx':cx,'out':OUT,'manifest':manifest,'index':0,'original_engine':bpy.context.scene.render.engine}
bpy.app.driver_namespace['fitted_export']=state
print('EXPORT PREPARED',[(o.name,len(o.data.vertices)) for o in groups])
exec(compile(open(os.path.join(ROOT,'artifacts/finish_fitted_export.py'),encoding='utf-8').read(),'finish_fitted_export.py','exec'))
